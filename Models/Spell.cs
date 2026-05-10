namespace Chaos.Models;

using Chaos.Enums;
using System;

/// <summary>
/// A spell that a wizard can cast. In the original game, each wizard starts
/// with a random hand of ~14 spells drawn from the master spell list plus
/// Disbelieve (which is always available and costs nothing).
///
/// Casting chance is the base probability (as a percentage). The world
/// alignment can shift this: a Lawful spell becomes easier to cast when
/// the world is Lawful, and harder when it's Chaotic, and vice versa.
///
/// Any creature spell can be cast as an illusion with 100% success.
/// Illusions are destroyed instantly by Disbelieve.
/// </summary>
public class Spell
{
    public string Name { get; init; } = "";
    public SpellCategory Category { get; init; }

    /// <summary>
    /// Base casting chance as a percentage (0–100).
    /// Dragons have 0% base; the effective chance is clamped to 10% minimum.
    /// Modified at runtime by world alignment.
    /// </summary>
    public int CastingChance { get; init; }

    /// <summary>
    /// How much this spell shifts the world alignment when cast.
    /// Positive = pushes toward Law, Negative = pushes toward Chaos.
    /// </summary>
    public int AlignmentShift { get; init; }

    /// <summary>
    /// For creature spells, the stats of the creature that will be summoned.
    /// Null for non-creature spells.
    /// </summary>
    public CreatureStats? CreatureData { get; init; }

    /// <summary>
    /// For ranged attack spells (Lightning, Magic Bolt, Vengeance, Dark Power),
    /// this is the damage/attack value used against the target's Magic Resistance.
    /// </summary>
    public int AttackPower { get; init; }

    /// <summary>Range for ranged spells, or 0 if not applicable.</summary>
    public int Range { get; init; }

    /// <summary>Has this spell been used? Most spells are single-use.</summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Compute the effective casting chance given the current world alignment.
    /// The original formula: base chance ± (worldAlignment × 10) depending on
    /// whether the spell's alignment matches or opposes the world state.
    /// Result is clamped to 10–100.
    /// </summary>
    public int GetEffectiveCastingChance(int worldAlignment)
    {
        // World alignment ranges from -8 to +8.
        // Each point of alignment match gives +1 to the tens digit of chance.
        int modifier = AlignmentShift switch
        {
            > 0 => worldAlignment,      // Lawful spell benefits from lawful world
            < 0 => -worldAlignment,     // Chaotic spell benefits from chaotic world
            _   => 0                    // Neutral spell is unaffected
        };

        int effective = CastingChance + (modifier * 10);
        return Math.Clamp(effective, 10, 100);
    }
}
