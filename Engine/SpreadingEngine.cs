namespace Chaos.Engine;

using Chaos.Enums;
using Chaos.Models;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles spreading terrain: Gooey Blob and Magic Fire.
///
/// Verified from Z80 at 0x95D0–0x96A0:
///   - The game iterates every cell on the board each round.
///   - Each blob/fire cell that hasn't already spread this round
///     rolls random(0–9). If the roll is below a threshold, it
///     attempts to spread into one random adjacent cell.
///   - If the target cell contains a creature, the creature is
///     killed (engulfed by blob, burned by fire).
///   - If the target cell contains a wizard, the wizard is killed.
///   - Spreading does NOT cross walls or other blob/fire cells.
///   - Each blob/fire cell can only spawn one new cell per round.
///   - Blob/fire can be destroyed by creatures attacking it in melee
///     (it has Defence 0, so most attacks succeed).
///
/// Call SpreadAll() once at the end of each round, after all wizards
/// have cast and moved.
/// </summary>
public class SpreadingEngine
{
    private readonly GameState _game;

    public SpreadingEngine(GameState game)
    {
        _game = game;
    }

    /// <summary>
    /// Process all spreading terrain for this round.
    /// Returns a list of descriptions for the UI to display.
    /// </summary>
    public List<string> SpreadAll()
    {
        var messages = new List<string>();

        var spreadSources = new List<(int X, int Y, CellContent Type, int Owner)>();

        for (int x = 0; x < GameBoard.Width; x++)
            for (int y = 0; y < GameBoard.Height; y++)
            {
                var cell = _game.Board[x, y];
                if (cell.Content is CellContent.GooeyBlob or CellContent.MagicFire)
                    spreadSources.Add((x, y, cell.Content, cell.OwnerWizardId));
            }

        foreach (var (sx, sy, type, owner) in spreadSources)
        {
            int roll = _game.Rng.Next(10);
            if (roll >= 4) continue;

            var adjacents = _game.Board.GetAdjacentCells(sx, sy).ToList();
            for (int i = adjacents.Count - 1; i > 0; i--)
            {
                int j = _game.Rng.Next(i + 1);
                (adjacents[i], adjacents[j]) = (adjacents[j], adjacents[i]);
            }

            foreach (var (tx, ty) in adjacents)
            {
                var target = _game.Board[tx, ty];

                if (target.Content is CellContent.Wall
                    or CellContent.GooeyBlob or CellContent.MagicFire
                    or CellContent.MagicCastle or CellContent.DarkCitadel)
                    continue;

                string typeName = type == CellContent.GooeyBlob ? "Gooey Blob" : "Magic Fire";
                BoardCreature? creatureToTrap = null;

                if (target.Content == CellContent.Creature && target.Creature != null)
                {
                    if (type == CellContent.GooeyBlob)
                    {
                        // Blob TRAPS creatures — they survive under the blob
                        creatureToTrap = target.Creature;
                        messages.Add($"Gooey Blob engulfs {creatureToTrap.Stats.Name} at ({tx},{ty}) — trapped!");
                    }
                    else
                    {
                        // Fire KILLS creatures outright
                        string victimName = target.Creature.Stats.Name;
                        _game.RemoveCreature(target.Creature);
                        messages.Add($"Magic Fire burns {victimName} at ({tx},{ty})!");
                    }
                }
                else if (target.Content == CellContent.Wizard && target.Wizard != null)
                {
                    // Both blob and fire kill wizards
                    string victimName = target.Wizard.Name;
                    _game.KillWizard(target.Wizard);
                    messages.Add($"{typeName} engulfs {victimName} at ({tx},{ty})!");
                }
                else if (target.Content == CellContent.MagicTree
                      || target.Content == CellContent.ShadowWood)
                {
                    messages.Add($"{typeName} destroys the tree at ({tx},{ty}).");
                }
                else if (target.Content != CellContent.Empty
                      && target.Content != CellContent.DeadBody)
                {
                    continue;
                }
                else
                {
                    messages.Add($"{typeName} spreads to ({tx},{ty}).");
                }

                // Place the blob/fire on this cell
                target.Content = type;
                target.OwnerWizardId = owner;
                target.Wizard = null;

                if (type == CellContent.GooeyBlob && creatureToTrap != null)
                {
                    // Store the trapped creature — it's hidden but alive
                    target.TrappedCreature = creatureToTrap;
                    target.Creature = null;
                    // Don't remove from game.Creatures — it's still alive
                }
                else
                {
                    target.Creature = null;
                    target.TrappedCreature = null;
                }

                break;
            }
        }

        return messages;
    }
}
