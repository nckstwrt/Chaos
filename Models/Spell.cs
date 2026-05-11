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
        // Convert our percentage base to 0-10 scale
        int baseChance = CastingChance / 10;

        int modifier = 0;
        if (AlignmentShift > 0 && worldAlignment > 0)
            modifier = worldAlignment;           // Lawful spell + lawful world
        else if (AlignmentShift < 0 && worldAlignment < 0)
            modifier = -worldAlignment;          // Chaotic spell + chaotic world
                                                 // Opposing or neutral → no modifier (NOT a penalty)

        int effective = Math.Clamp(baseChance + modifier, 0, 9);
        return effective;  // 0-9 scale, NOT percentage
    }

    /// <summary>
    /// How many terrain objects this spell places on the board.
    /// Verified from Z80: Wall = 4 (at 0x9B76), Magic Wood = 8,
    /// Shadow Wood = 8 (at 0x9ADD), Castle/Citadel = 1.
    /// </summary>
    public int PlacementCount { get; init; } = 1;
}
