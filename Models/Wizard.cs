namespace Chaos.Models;

using Chaos.Enums;
using System.Collections.Generic;

/// <summary>
/// A wizard in the game. Up to 8 wizards can play (the original supported 2–8).
/// Each wizard has base combat stats (weaker than most creatures), a hand of
/// spells, and can optionally be mounted on a creature for mobility.
///
/// Wizard IDs 0–7. The original game assigned names from a built-in list and
/// let human players rename themselves. CPU wizards used the default names.
/// </summary>
public class Wizard
{
    // ── Identity ────────────────────────────────────────────────────

    public int Id { get; init; }
    public string Name { get; set; } = "";
    public bool IsHuman { get; set; }
    public bool IsAlive { get; set; } = true;

    /// <summary>
    /// Colour index 0–7, used for rendering. In the original these mapped
    /// to Spectrum INK colours.
    /// </summary>
    public int Colour { get; init; }

    // ── Position ────────────────────────────────────────────────────

    public int X { get; set; }
    public int Y { get; set; }

    // ── Base combat stats (original defaults) ──────────────────────

    /// <summary>Base wizard combat is low (typically 1–3).</summary>
    public int Combat { get; set; } = 1;
    public int RangedCombat { get; set; } = 0;
    public int Range { get; set; } = 0;
    public int Defence { get; set; } = 1;
    public int Movement { get; set; } = 1;
    public int Manoeuvre { get; set; } = 3;
    public int MagicResistance { get; set; } = 3;

    // ── Equipment flags (modified by spells) ────────────────────────

    public bool HasMagicShield { get; set; }    // Defence +2
    public bool HasMagicArmour { get; set; }    // Defence +4
    public bool HasMagicSword { get; set; }     // Combat +4
    public bool HasMagicKnife { get; set; }     // Combat +2
    public bool HasMagicBow { get; set; }       // Grants ranged attack
    public bool HasMagicWings { get; set; }     // Grants flying movement
    public bool HasShadowForm { get; set; }     // Improved defence & movement

    // ── Mount ───────────────────────────────────────────────────────

    /// <summary>
    /// If the wizard is riding a creature, this holds that creature's stats.
    /// Movement, flying, and combat use the mount's values instead.
    /// </summary>
    public BoardCreature? Mount { get; set; }
    public bool IsMounted => Mount != null;

    // ── Spells ──────────────────────────────────────────────────────

    public List<Spell> Spells { get; init; } = new();

    /// <summary>The spell selected this turn, or null if passing.</summary>
    public Spell? SelectedSpell { get; set; }

    /// <summary>If true, the selected creature spell will be cast as an illusion.</summary>
    public bool CastingAsIllusion { get; set; }

    // ── Computed effective stats (accounting for equipment/mount) ───

    public int EffectiveCombat
    {
        get
        {
            int c = Combat;
            if (HasMagicSword) c += 4;
            if (HasMagicKnife) c += 2;
            return c;
        }
    }

    public int EffectiveDefence
    {
        get
        {
            int d = Defence;
            if (HasMagicShield) d += 2;
            if (HasMagicArmour) d += 4;
            if (HasShadowForm) d += 2;
            return d;
        }
    }

    public int EffectiveMovement
    {
        get
        {
            if (IsMounted) return Mount!.Stats.Movement;
            int m = Movement;
            if (HasMagicWings) m = 6;  // Wings grant 6 flying movement
            if (HasShadowForm) m += 2;
            return m;
        }
    }

    public bool CanFly => IsMounted ? Mount!.Stats.IsFlying : HasMagicWings;
}
