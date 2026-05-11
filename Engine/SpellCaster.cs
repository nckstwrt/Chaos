namespace Chaos.Engine;

using Chaos.Enums;
using Chaos.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles spell casting logic. In the original game, spell casting is
/// the first action in a wizard's turn (before movement).
///
/// Key mechanics:
///   - Creature spells: place a creature on an adjacent empty square.
///     Can be cast as an illusion (100% success) or for real (roll vs chance).
///   - Disbelieve: target any creature; if it's an illusion, it's destroyed.
///   - Attack spells: target any creature/wizard within range, roll magic attack.
///   - Equipment spells: auto-target the casting wizard, always succeed.
///   - Terrain spells: place on adjacent square(s).
///   - Subversion: attempt to take control of an enemy creature.
///
/// The world alignment shifts by the spell's AlignmentShift value
/// each time a spell is successfully cast.
/// </summary>
public class SpellCaster
{
    private readonly GameState _game;
    private readonly CombatEngine _combat;

    public SpellCaster(GameState game, CombatEngine combat)
    {
        _game = game;
        _combat = combat;
    }

    /// <summary>
    /// Attempt to cast the wizard's selected spell. Returns a message
    /// describing what happened.
    /// </summary>
    public string CastSpell(Wizard wizard, int targetX, int targetY)
    {
        var spell = wizard.SelectedSpell;
        if (spell == null) return $"{wizard.Name} passes.";

        return spell.Category switch
        {
            SpellCategory.Creature             => CastCreatureSpell(wizard, spell, targetX, targetY),
            SpellCategory.DisbelieveIllusion    => CastDisbelieve(wizard, spell, targetX, targetY),
            SpellCategory.MagicAttack           => CastAttackSpell(wizard, spell, targetX, targetY),
            SpellCategory.MagicDefence          => CastEquipmentSpell(wizard, spell),
            SpellCategory.MagicMisc             => CastMiscSpell(wizard, spell, targetX, targetY),
            SpellCategory.MagicTree             => CastTerrainSpell(wizard, spell, targetX, targetY, CellContent.MagicTree),
            SpellCategory.MagicFire             => CastTerrainSpell(wizard, spell, targetX, targetY, CellContent.MagicFire),
            SpellCategory.MagicBlob             => CastTerrainSpell(wizard, spell, targetX, targetY, CellContent.GooeyBlob),
            SpellCategory.AlignmentSpell        => CastAlignmentSpell(wizard, spell),
            _                                   => "Unknown spell type."
        };
    }

    // ── Creature summoning ──────────────────────────────────────────

    private string CastCreatureSpell(Wizard wizard, Spell spell, int tx, int ty)
    {
        // Validate: target must be adjacent and empty
        if (GameBoard.Distance(wizard.X, wizard.Y, tx, ty) > 1)
            return "Target must be adjacent to the wizard.";
        if (!_game.Board.IsEmpty(tx, ty))
            return "That square is occupied.";

        // Illusions always succeed
        if (wizard.CastingAsIllusion)
        {
            var creature = _game.PlaceCreature(spell.CreatureData!, tx, ty, wizard.Id, isIllusion: true);
            spell.IsUsed = true;
            return $"{wizard.Name} casts {spell.Name}! (illusion)";
        }

        // Real casting: roll against the effective casting chance
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(10);

        if (roll < chance)
        {
            var creature = _game.PlaceCreature(spell.CreatureData!, tx, ty, wizard.Id, isIllusion: false);
            ShiftWorldAlignment(spell);
            spell.IsUsed = true;

            // Check if wizard can mount
            if (creature.Stats.IsMount && GameBoard.Distance(wizard.X, wizard.Y, tx, ty) == 1)
            {
                // Mounting is optional — for now, auto-mount logic can be added later
            }

            return $"{wizard.Name} casts {spell.Name}! ({chance}% chance, rolled {roll})";
        }
        else
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s {spell.Name} spell fails. ({chance}% chance, rolled {roll})";
        }
    }

    // ── Disbelieve ──────────────────────────────────────────────────

    private string CastDisbelieve(Wizard wizard, Spell spell, int tx, int ty)
    {
        var creature = _game.GetCreatureAt(tx, ty);
        if (creature == null)
            return "No creature to disbelieve.";

        // Disbelieve has a 90% base casting chance (not 100% as often assumed).
        // It can fail! World alignment can modify this.
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(10);
        if (roll >= chance)
            return $"{wizard.Name}'s Disbelieve fails! ({chance}% chance, rolled {roll})";

        // Disbelieve always succeeds against illusions, always fails against real
        if (creature.IsIllusion)
        {
            _game.RemoveCreature(creature);
            return $"{creature.Stats.Name} was an illusion — it vanishes!";
        }
        else
        {
            return $"{creature.Stats.Name} is real.";
        }
        // Note: Disbelieve is never consumed — it can be used every turn
    }

    // ── Attack spells ───────────────────────────────────────────────

    private string CastAttackSpell(Wizard wizard, Spell spell, int tx, int ty)
    {
        int dist = GameBoard.Distance(wizard.X, wizard.Y, tx, ty);
        if (dist > spell.Range)
            return "Target is out of range.";

        // Check for a valid target
        var targetCell = _game.Board[tx, ty];
        if (targetCell.Content != CellContent.Creature && targetCell.Content != CellContent.Wizard)
            return "No valid target at that location.";

        // Line of sight check
        if (!LineOfSight.HasClearPath(_game.Board, wizard.X, wizard.Y, tx, ty))
            return "No line of sight to target.";

        // Roll to cast the spell first
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int castRoll = _game.Rng.Next(10);
        if (castRoll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s {spell.Name} fizzles! ({chance}% chance, rolled {castRoll})";
        }

        ShiftWorldAlignment(spell);
        spell.IsUsed = true;

        // Now resolve the magical attack
        int defManoeuvre;
        string targetName;

        if (targetCell.Content == CellContent.Wizard)
        {
            defManoeuvre = targetCell.Wizard!.Manoeuvre;
            targetName = targetCell.Wizard.Name;
        }
        else
        {
            defManoeuvre = targetCell.Creature!.Stats.Manoeuvre;
            targetName = targetCell.Creature.Stats.Name;
        }

        var result = _combat.ResolveMagicAttack(spell.AttackPower, defManoeuvre);

        if (result.AttackerWins)
        {
            if (targetCell.Content == CellContent.Wizard)
                _game.KillWizard(targetCell.Wizard!);
            else
                _game.RemoveCreature(targetCell.Creature!);

            return $"{wizard.Name}'s {spell.Name} destroys {targetName}! ({result.Description})";
        }

        return $"{targetName} resists {wizard.Name}'s {spell.Name}. ({result.Description})";
    }

    // ── Equipment spells ────────────────────────────────────────────

    private string CastEquipmentSpell(Wizard wizard, Spell spell)
    {
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(10);
        if (roll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s {spell.Name} fails.";
        }

        switch (spell.Name)
        {
            case "Magic Shield":  wizard.HasMagicShield = true; break;
            case "Magic Armour":  wizard.HasMagicArmour = true; break;
            case "Magic Sword":   wizard.HasMagicSword  = true; break;
            case "Magic Knife":   wizard.HasMagicKnife  = true; break;
            case "Magic Bow":     wizard.HasMagicBow    = true; break;
        }

        ShiftWorldAlignment(spell);
        spell.IsUsed = true;
        return $"{wizard.Name} casts {spell.Name}!";
    }

    // ── Misc spells ─────────────────────────────────────────────────

    private string CastMiscSpell(Wizard wizard, Spell spell, int tx, int ty)
    {
        return spell.Name switch
        {
            "Magic Wings"  => CastSelfBuff(wizard, spell, () => wizard.HasMagicWings = true),
            "Shadow Form"  => CastSelfBuff(wizard, spell, () => wizard.HasShadowForm = true),
            "Subversion"   => CastSubversion(wizard, spell, tx, ty),
            "Raise Dead"   => CastRaiseDead(wizard, spell, tx, ty),
            "Turmoil"      => CastTurmoil(wizard, spell),
            "Magic Castle" => CastTerrainSpell(wizard, spell, tx, ty, CellContent.MagicCastle),
            "Dark Citadel" => CastTerrainSpell(wizard, spell, tx, ty, CellContent.DarkCitadel),
            "Wall"         => CastTerrainSpell(wizard, spell, tx, ty, CellContent.Wall),
            _              => $"Unimplemented spell: {spell.Name}"
        };
    }

    private string CastSelfBuff(Wizard wizard, Spell spell, Action applyEffect)
    {
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(10);
        if (roll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s {spell.Name} fails.";
        }

        applyEffect();
        ShiftWorldAlignment(spell);
        spell.IsUsed = true;
        return $"{wizard.Name} casts {spell.Name}!";
    }

    private string CastSubversion(Wizard wizard, Spell spell, int tx, int ty)
    {
        int dist = GameBoard.Distance(wizard.X, wizard.Y, tx, ty);
        if (dist > spell.Range)
            return $"Target is out of range (max {spell.Range}).";

        var creature = _game.GetCreatureAt(tx, ty);
        if (creature == null) return "No creature to subvert.";
        if (creature.OwnerWizardId == wizard.Id) return "That's your own creature!";

        // Roll to cast
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int castRoll = _game.Rng.Next(10);
        if (castRoll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s Subversion fails to cast. ({chance}% chance)";
        }

        // Subversion: roll against MANOEUVRE, not magic resistance.
        // From Z80 at 0x85B4: LD E,0x13 (Manoeuvre), then
        //   threshold = manoeuvre + 1
        //   if random(0-9) < threshold → SUCCESS
        // Higher manoeuvre = EASIER to subvert (more independent-minded).
        int threshold = creature.Stats.Manoeuvre + 1;
        int roll = _game.Rng.Next(10);
        spell.IsUsed = true;
        if (roll < threshold)
        {
            creature.OwnerWizardId = wizard.Id;
            return $"{creature.Stats.Name} is now under {wizard.Name}'s control! (rolled {roll} vs threshold {threshold})";
        }
        return $"{creature.Stats.Name} resists subversion. (rolled {roll} vs threshold {threshold})";
    }

    private string CastRaiseDead(Wizard wizard, Spell spell, int tx, int ty)
    {
        int dist = GameBoard.Distance(wizard.X, wizard.Y, tx, ty);
        if (dist > spell.Range)
            return $"Target is out of range (max {spell.Range}).";

        if (!_game.Board.InBounds(tx, ty)) return "Invalid target.";
        var cell = _game.Board[tx, ty];
        if (!cell.HasCorpse) return "No dead body there.";

        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(10);
        if (roll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s Raise Dead fails.";
        }

        // Raise the corpse as an undead creature owned by the caster
        // In the original, raised dead become generic undead with low stats
        var undeadStats = new CreatureStats
        {
            Name = "Undead",
            Combat = 1, Defence = 1, Movement = 1, Manoeuvre = 2,
            MagicResistance = 2,
            Abilities = SpecialAbility.Undead,
            AlignmentValue = -1
        };

        cell.HasCorpse = false;
        _game.PlaceCreature(undeadStats, tx, ty, wizard.Id, isIllusion: false);
        ShiftWorldAlignment(spell);
        spell.IsUsed = true;
        return $"{wizard.Name} raises the dead!";
    }

    private string CastTurmoil(Wizard wizard, Spell spell)
    {
        // Turmoil: randomly scatter all creatures and wizards to new positions.
        // This is one of the most dramatic spells in the game!
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(10);
        if (roll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s Turmoil fails.";
        }

        // Collect all occupied positions, clear them, then redistribute
        var emptySquares = new List<(int X, int Y)>();
        for (int x = 0; x < GameBoard.Width; x++)
        for (int y = 0; y < GameBoard.Height; y++)
        {
            if (_game.Board[x, y].Content == CellContent.Empty)
                emptySquares.Add((x, y));
        }

        // Shuffle empty squares
        for (int i = emptySquares.Count - 1; i > 0; i--)
        {
            int j = _game.Rng.Next(i + 1);
            (emptySquares[i], emptySquares[j]) = (emptySquares[j], emptySquares[i]);
        }

        // Relocate each wizard and creature to a random empty square
        int idx = 0;
        foreach (var w in _game.GetAliveWizards())
        {
            if (idx >= emptySquares.Count) break;
            _game.Board[w.X, w.Y].Wizard = null;
            _game.Board[w.X, w.Y].Content = CellContent.Empty;
            (w.X, w.Y) = emptySquares[idx++];
            _game.Board[w.X, w.Y].Wizard = w;
            _game.Board[w.X, w.Y].Content = CellContent.Wizard;
        }

        foreach (var c in _game.Creatures)
        {
            if (idx >= emptySquares.Count) break;
            _game.Board[c.X, c.Y].Creature = null;
            _game.Board[c.X, c.Y].Content = CellContent.Empty;
            (c.X, c.Y) = emptySquares[idx++];
            _game.Board[c.X, c.Y].Creature = c;
            _game.Board[c.X, c.Y].Content = CellContent.Creature;
        }

        ShiftWorldAlignment(spell);
        spell.IsUsed = true;
        return $"{wizard.Name} casts Turmoil! Everything is scattered!";
    }

    // ── Terrain ─────────────────────────────────────────────────────

    /// <summary>
    /// Place a single terrain object at the target position.
    /// Called once per placement — the caller loops for multi-placement spells.
    /// Returns null on success, or an error message on failure.
    /// Does NOT consume the spell or roll casting chance — the caller
    /// handles that before the placement loop begins.
    /// </summary>
    public string? PlaceSingleTerrain(Wizard wizard, Spell spell, int tx, int ty, CellContent terrain)
    {
        int dist = GameBoard.Distance(wizard.X, wizard.Y, tx, ty);
        if (dist > spell.Range)
            return $"Target is out of range (max {spell.Range}).";
        if (!_game.Board.IsEmpty(tx, ty))
            return "That square is occupied.";

        // Magic Wood and Shadow Wood: no two trees can be adjacent.
        // Verified from instructions: "No two trees can be adjacent."
        if (terrain is CellContent.MagicTree or CellContent.ShadowWood)
        {
            foreach (var (nx, ny) in _game.Board.GetAdjacentCells(tx, ty))
            {
                var adj = _game.Board[nx, ny];
                if (adj.Content is CellContent.MagicTree or CellContent.ShadowWood)
                    return "Trees cannot be placed adjacent to other trees.";
            }
        }

        _game.Board[tx, ty].Content = terrain;
        _game.Board[tx, ty].OwnerWizardId = wizard.Id;
        return null; // success
    }

    private string CastTerrainSpell(Wizard wizard, Spell spell, int tx, int ty, CellContent terrain)
    {
        // Roll casting chance once for the whole spell
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(100);
        if (roll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s {spell.Name} fails.";
        }

        // Place the first object at the targeted position
        string? error = PlaceSingleTerrain(wizard, spell, tx, ty, terrain);
        if (error != null)
        {
            spell.IsUsed = true;
            return error;
        }

        ShiftWorldAlignment(spell);
        spell.IsUsed = true;

        if (spell.PlacementCount <= 1)
            return $"{wizard.Name} casts {spell.Name}!";

        // Multi-placement: remaining placements handled by the caller
        // (Program.cs for humans, AI casting for computer)
        // Store how many remain so the caller can loop
        _remainingPlacements = spell.PlacementCount - 1;
        _remainingTerrain = terrain;
        return $"{wizard.Name} casts {spell.Name}! Place {_remainingPlacements} more.";
    }

    // State for multi-placement spells (read by Program.cs)
    private int _remainingPlacements = 0;
    private CellContent _remainingTerrain = CellContent.Empty;

    /// <summary>How many more placements remain after the initial cast.</summary>
    public int RemainingPlacements => _remainingPlacements;

    /// <summary>What terrain type is being placed.</summary>
    public CellContent RemainingTerrain => _remainingTerrain;

    /// <summary>Call after each additional placement to decrement the counter.</summary>
    public void DecrementPlacements() => _remainingPlacements--;

    /// <summary>Cancel remaining placements (player pressed done).</summary>
    public void ClearPlacements() => _remainingPlacements = 0;

    // ── Alignment spells ───────────────────────────────────────────

    private string CastAlignmentSpell(Wizard wizard, Spell spell)
    {
        // LAW-1/2 and CHAOS-1/2: pure alignment-shifting spells.
        // They have large alignment shifts (+2, +4, -2, -4) and are
        // a key strategic tool for making aligned spells easier to cast.
        int chance = spell.GetEffectiveCastingChance(_game.WorldAlignment);
        int roll = _game.Rng.Next(100);
        if (roll >= chance)
        {
            spell.IsUsed = true;
            return $"{wizard.Name}'s {spell.Name} fails. ({chance}% chance)";
        }

        ShiftWorldAlignment(spell);
        spell.IsUsed = true;
        string direction = spell.AlignmentShift > 0 ? "Lawful" : "Chaotic";
        return $"{wizard.Name} casts {spell.Name}! The world shifts toward {direction}. (Alignment: {_game.WorldAlignment:+0;-0;0})";
    }

    // ── Alignment tracking ──────────────────────────────────────────

    private void ShiftWorldAlignment(Spell spell)
    {
        _game.WorldAlignment = Math.Clamp(
            _game.WorldAlignment + spell.AlignmentShift, -8, 8);
    }
}
