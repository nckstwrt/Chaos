namespace Chaos.Engine;

using Chaos.Enums;
using Chaos.Models;
using System.Collections.Generic;

/// <summary>
/// Handles movement for wizards and creatures. The original game's
/// movement system works as follows:
///
///   1. Each unit has a Movement value (squares per turn).
///   2. Flying units can move over occupied squares.
///   3. Ground units must path around obstacles.
///   4. "Engaged" units (adjacent to an enemy) cannot move — they can
///      only attack the adjacent enemy or stay put.
///   5. When a unit moves next to an enemy, it can choose to attack.
///   6. A unit that attacks ends its movement.
///
/// Movement in the original is done one step at a time (the player presses
/// a direction key for each square), which creates natural pathfinding.
/// </summary>
public class MovementEngine
{
    private readonly GameState _game;
    private readonly CombatEngine _combat;

    public MovementEngine(GameState game, CombatEngine combat)
    {
        _game = game;
        _combat = combat;
    }

    /// <summary>
    /// Get the valid squares a creature or wizard can move to from their
    /// current position. This is a simplified version — the original does
    /// step-by-step movement, but for the port we calculate reachable squares.
    /// </summary>
    public List<(int X, int Y)> GetValidMoves(int fromX, int fromY, int movement, bool canFly, int ownerWizardId)
    {
        var reachable = new List<(int, int)>();

        // If engaged, the unit can't move (only attack adjacent enemies)
        if (_game.IsAdjacentToEnemy(fromX, fromY, ownerWizardId))
        {
            // Can attack adjacent enemies though
            return reachable;
        }

        // BFS/flood fill to find reachable squares
        var visited = new HashSet<(int, int)> { (fromX, fromY) };
        var frontier = new Queue<(int X, int Y, int StepsLeft)>();
        frontier.Enqueue((fromX, fromY, movement));

        while (frontier.Count > 0)
        {
            var (cx, cy, steps) = frontier.Dequeue();
            if (steps <= 0) continue;

            foreach (var (nx, ny) in _game.Board.GetAdjacentCells(cx, cy))
            {
                if (visited.Contains((nx, ny))) continue;
                visited.Add((nx, ny));

                var cell = _game.Board[nx, ny];

                // Can we enter this square?
                if (cell.IsPassable)
                {
                    reachable.Add((nx, ny));
                    frontier.Enqueue((nx, ny, steps - 1));
                }
                else if (canFly && cell.Content != CellContent.Wall)
                {
                    // Flying units can move over occupied squares but not land on them
                    // (unless the square is empty on the far side)
                    frontier.Enqueue((nx, ny, steps - 1));
                    if (cell.Content == CellContent.Empty || cell.Content == CellContent.DeadBody)
                        reachable.Add((nx, ny));
                }

                // Adjacent to enemy = potential attack target (add to reachable for combat)
                if (IsEnemyOccupied(cell, ownerWizardId))
                {
                    reachable.Add((nx, ny)); // Can move into enemy square to attack
                }
            }
        }

        return reachable;
    }

    /// <summary>
    /// Get squares adjacent to (x,y) that contain enemies — valid attack targets.
    /// </summary>
    public List<(int X, int Y)> GetAttackTargets(int x, int y, int ownerWizardId)
    {
        var targets = new List<(int, int)>();
        foreach (var (nx, ny) in _game.Board.GetAdjacentCells(x, y))
        {
            var cell = _game.Board[nx, ny];
            if (IsEnemyOccupied(cell, ownerWizardId))
                targets.Add((nx, ny));
        }
        return targets;
    }

    /// <summary>
    /// Get valid ranged attack targets for a unit with ranged capability.
    /// </summary>
    public List<(int X, int Y)> GetRangedTargets(int x, int y, int range, int ownerWizardId)
    {
        var targets = new List<(int, int)>();
        for (int tx = 0; tx < GameBoard.Width; tx++)
        for (int ty = 0; ty < GameBoard.Height; ty++)
        {
            if (tx == x && ty == y) continue;
            if (GameBoard.Distance(x, y, tx, ty) > range) continue;

            var cell = _game.Board[tx, ty];
            if (IsEnemyOccupied(cell, ownerWizardId)
                && LineOfSight.HasClearPath(_game.Board, x, y, tx, ty))
                targets.Add((tx, ty));
            }
        return targets;
    }

    /// <summary>
    /// Execute a move: relocate a creature from one square to another.
    /// If the destination contains an enemy, this becomes an attack.
    /// Returns a description of what happened.
    /// </summary>
    public string MoveCreature(BoardCreature creature, int toX, int toY)
    {
        var targetCell = _game.Board[toX, toY];

        // Moving into an enemy = melee attack
        if (IsEnemyOccupied(targetCell, creature.OwnerWizardId))
        {
            creature.HasAttacked = true;
            creature.HasMoved = true;
            return _combat.ExecuteAttack(creature.X, creature.Y, toX, toY, isRanged: false);
        }

        // Normal movement
        _game.Board[creature.X, creature.Y].Creature = null;
        _game.Board[creature.X, creature.Y].Content = CellContent.Empty;

        creature.X = toX;
        creature.Y = toY;
        creature.HasMoved = true;

        targetCell.Creature = creature;
        targetCell.Content = CellContent.Creature;

        return $"{creature.Stats.Name} moves to ({toX},{toY}).";
    }

    /// <summary>
    /// Execute wizard movement. Similar to creature movement but handles
    /// the wizard's board cell differently.
    /// </summary>
    public string MoveWizard(Wizard wizard, int toX, int toY)
    {
        var targetCell = _game.Board[toX, toY];

        // Moving into an enemy = melee attack
        if (IsEnemyOccupied(targetCell, wizard.Id))
        {
            return _combat.ExecuteAttack(wizard.X, wizard.Y, toX, toY, isRanged: false);
        }

        // Mounting: if moving onto own mount creature
        if (targetCell.Content == CellContent.Creature
            && targetCell.Creature?.OwnerWizardId == wizard.Id
            && targetCell.Creature.Stats.IsMount)
        {
            _game.Board[wizard.X, wizard.Y].Wizard = null;
            _game.Board[wizard.X, wizard.Y].Content = CellContent.Empty;

            wizard.Mount = targetCell.Creature;
            wizard.X = toX;
            wizard.Y = toY;

            // The wizard replaces the creature on the board
            _game.Creatures.Remove(targetCell.Creature);
            targetCell.Creature = null;
            targetCell.Content = CellContent.Wizard;
            targetCell.Wizard = wizard;

            return $"{wizard.Name} mounts the {wizard.Mount.Stats.Name}!";
        }

        // Normal movement
        _game.Board[wizard.X, wizard.Y].Wizard = null;
        _game.Board[wizard.X, wizard.Y].Content = CellContent.Empty;

        wizard.X = toX;
        wizard.Y = toY;
        targetCell.Wizard = wizard;
        targetCell.Content = CellContent.Wizard;

        return $"{wizard.Name} moves to ({toX},{toY}).";
    }

    private static bool IsEnemyOccupied(BoardCell cell, int myWizardId)
    {
        if (cell.Content == CellContent.Wizard && cell.Wizard?.Id != myWizardId)
            return true;
        if (cell.Content == CellContent.Creature && cell.Creature?.OwnerWizardId != myWizardId)
            return true;
        return false;
    }
}
