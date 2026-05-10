namespace Chaos.Engine;

using Chaos.Data;
using Chaos.Enums;
using Chaos.Models;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles the Magic Wood spell-granting mechanic.
///
/// Verified from Z80 at 0x9ADD–0x9B70:
///   - At the start of each wizard's turn, the game checks if the
///     wizard is standing on a Magic Wood cell (object 36, 0x24).
///   - If so, the wizard receives one random spell from the FULL
///     spell table — including Turmoil, which is otherwise excluded
///     from initial spell dealing.
///   - Shadow Wood (object 37, 0x25) does NOT grant spells.
///   - The original checks object IDs 0x24–0x27 (Magic Wood to
///     Dark Citadel range) but only grants for Magic Wood specifically.
///   - A wizard only receives one spell per turn regardless of how
///     many trees they're standing on (they can only occupy one cell).
///
/// Call CheckMagicWood() at the start of each wizard's casting phase.
/// </summary>
public class MagicWoodEngine
{
    private readonly GameState _game;

    public MagicWoodEngine(GameState game)
    {
        _game = game;
    }

    /// <summary>
    /// Check if the wizard is standing on a Magic Wood cell.
    /// If so, grant them a random new spell.
    /// Returns a message for the UI, or null if no spell was granted.
    /// </summary>
    public string? CheckMagicWood(Wizard wizard)
    {
        if (!wizard.IsAlive) return null;

        var cell = _game.Board[wizard.X, wizard.Y];

        // Only Magic Wood grants spells, not Shadow Wood
        if (cell.Content != CellContent.MagicTree) return null;

        // Grant a random spell from the full pool (including Turmoil)
        var allSpells = SpellDatabase.CreateAllSpells();

        // Remove Disbelieve (wizard always has it) and any spells
        // the wizard already has by name
        var existingNames = new HashSet<string>(wizard.Spells.Select(s => s.Name));
        var candidates = allSpells
            .Where(s => s.Name != "Disbelieve")
            .Where(s => !existingNames.Contains(s.Name))
            .ToList();

        if (candidates.Count == 0)
            return $"{wizard.Name} stands in the Magic Wood but already knows every spell!";

        var newSpell = candidates[_game.Rng.Next(candidates.Count)];
        wizard.Spells.Add(newSpell);

        return $"{wizard.Name} receives {newSpell.Name} from the Magic Wood!";
    }
}
