namespace Chaos.Models;

using Chaos.Enums;

/// <summary>
/// Stats for a creature on the board. These mirror the original game's
/// data tables stored at addresses like 0xC000+ in the Spectrum memory.
///
/// Each creature in the original has a fixed block of stats:
///   Combat, Ranged Combat, Range, Defence, Movement, Manoeuvre,
///   Magic Resistance, and various flags.
/// </summary>
public class CreatureStats
{
    public string Name { get; set; } = "";

    // ── Combat stats (original range 0–9) ──────────────────────────

    /// <summary>Attack strength in melee. Attacker rolls Combat vs defender's Defence.</summary>
    public int Combat { get; set; }

    /// <summary>Ranged attack strength (0 = no ranged attack).</summary>
    public int RangedCombat { get; set; }

    /// <summary>Maximum range for ranged attacks, in squares.</summary>
    public int Range { get; set; }

    /// <summary>Defensive strength. Compared against attacker's Combat roll.</summary>
    public int Defence { get; set; }

    /// <summary>Squares this creature can move per turn.</summary>
    public int Movement { get; set; }

    /// <summary>
    /// Manoeuvre rating — used in the "engaged to engaged" combat modifier.
    /// Higher manoeuvre means a creature is harder to pin down.
    /// </summary>
    public int Manoeuvre { get; set; }

    /// <summary>
    /// Resistance to magical attacks. Checked when targeted by direct spells.
    /// Original range 0–9, where 9 is near-immune.
    /// </summary>
    public int MagicResistance { get; set; }

    // ── Flags ───────────────────────────────────────────────────────

    public SpecialAbility Abilities { get; set; } = SpecialAbility.None;
    public Alignment Alignment { get; set; } = Alignment.Neutral;

    /// <summary>
    /// Alignment value used when computing world alignment shift.
    /// Positive = Lawful, Negative = Chaotic. Original range roughly -3 to +3.
    /// </summary>
    public int AlignmentValue { get; set; }

    /// <summary>
    /// Whether this creature is rideable by a wizard (Pegasus, Unicorn, etc.).
    /// In the original, specific creature IDs are flagged as mounts.
    /// </summary>
    public bool IsMount { get; set; }

    public bool IsFlying => Abilities.HasFlag(SpecialAbility.Flying);
    public bool IsUndead => Abilities.HasFlag(SpecialAbility.Undead);

    /// <summary>Create a runtime copy (for spawning a creature onto the board).</summary>
    public CreatureStats Clone() => (CreatureStats)MemberwiseClone();
}
