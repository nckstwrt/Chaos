namespace Chaos.Engine;

using Chaos.Enums;
using Chaos.Models;
using System.Collections.Generic;

/// <summary>
/// Handles Shadow Wood's active attack mechanic.
///
/// From the game instructions: "Shadow Wood can attack anything
/// in contact with them (except undead)."
///
/// Each turn, every Shadow Wood cell attacks one random adjacent
/// non-undead enemy creature or wizard. The attack uses the Shadow
/// Wood's Combat stat (verified at 4 from the creature data table —
/// Shadow Wood is object 37, shares stats with the tree record).
///
/// Call AttackAll() once per round, alongside spreading.
/// </summary>
public class ShadowWoodEngine
{
    private readonly GameState _game;
    private readonly CombatEngine _combat;

    // Shadow Wood combat stat from binary (object 37 at 0xE987)
    private const int ShadowWoodCombat = 4;

    public ShadowWoodEngine(GameState game, CombatEngine combat)
    {
        _game = game;
        _combat = combat;
    }

    /// <summary>
    /// Every Shadow Wood cell attacks one random adjacent
    /// non-undead enemy. Returns messages for the UI.
    /// </summary>
    public List<string> AttackAll()
    {
        var messages = new List<string>();

        // Collect all Shadow Wood positions first
        var woods = new List<(int X, int Y, int Owner)>();
        for (int x = 0; x < GameBoard.Width; x++)
            for (int y = 0; y < GameBoard.Height; y++)
            {
                var cell = _game.Board[x, y];
                if (cell.Content == CellContent.ShadowWood)
                    woods.Add((x, y, cell.OwnerWizardId));
            }

        foreach (var (wx, wy, owner) in woods)
        {
            // Find adjacent non-undead enemies
            var targets = new List<(int X, int Y)>();

            foreach (var (nx, ny) in _game.Board.GetAdjacentCells(wx, wy))
            {
                var cell = _game.Board[nx, ny];

                if (cell.Content == CellContent.Creature
                    && cell.Creature != null
                    && cell.Creature.OwnerWizardId != owner
                    && !cell.Creature.Stats.IsUndead)
                {
                    targets.Add((nx, ny));
                }
                else if (cell.Content == CellContent.Wizard
                         && cell.Wizard != null
                         && cell.Wizard.Id != owner)
                {
                    targets.Add((nx, ny));
                }
            }

            if (targets.Count == 0) continue;

            // Pick one random target
            var (tx, ty) = targets[_game.Rng.Next(targets.Count)];
            var targetCell = _game.Board[tx, ty];

            string targetName;
            int targetDefence;

            if (targetCell.Content == CellContent.Wizard)
            {
                targetName = targetCell.Wizard!.Name;
                targetDefence = targetCell.Wizard.EffectiveDefence;
            }
            else
            {
                targetName = targetCell.Creature!.Stats.Name;
                targetDefence = targetCell.Creature.Stats.Defence;
            }

            // Resolve melee: Shadow Wood combat vs target defence
            var result = _combat.ResolveMelee(
                ShadowWoodCombat, 0,    // Shadow Wood has no manoeuvre
                targetDefence, 0,       // ignore manoeuvre modifiers
                bothEngaged: false);

            if (result.AttackerWins)
            {
                if (targetCell.Content == CellContent.Wizard)
                {
                    _game.KillWizard(targetCell.Wizard!);
                    messages.Add($"Shadow Wood at ({wx},{wy}) kills {targetName}! ({result.Description})");
                }
                else
                {
                    _game.RemoveCreature(targetCell.Creature!);
                    messages.Add($"Shadow Wood at ({wx},{wy}) kills {targetName}! ({result.Description})");
                }
            }
            else
            {
                messages.Add($"Shadow Wood at ({wx},{wy}) attacks {targetName} but fails. ({result.Description})");
            }
        }

        return messages;
    }
}