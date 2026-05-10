using System;

namespace Chaos.Enums;

/// <summary>
/// The phases of a single player's turn, matching the original game flow.
/// In the original Z80 code, these map to the main game loop states.
/// </summary>
public enum TurnPhase
{
    ChooseSpell,
    CastSpell,
    Movement,
    GameOver
}

/// <summary>
/// Alignment axis from Law (positive) through Neutral to Chaos (negative).
/// The world alignment shifts as spells are cast, affecting casting chances.
/// Original range: -8 (most chaotic) to +8 (most lawful).
/// </summary>
public enum Alignment
{
    Chaos = -1,
    Neutral = 0,
    Law = 1
}

/// <summary>
/// Categories of spells in the original game.
/// </summary>
public enum SpellCategory
{
    Creature,       // Summons a creature within range of the wizard
    DisbelieveIllusion, // Attempts to destroy an illusionary creature
    MagicAttack,    // Ranged attack spells (Lightning, Magic Bolt, etc.)
    MagicDefence,   // Protective spells (Magic Shield, Magic Armour, etc.)
    MagicMisc,      // Utility spells (Magic Wings, Shadow Form, etc.)
    MagicTree,      // Grows magic trees (Magic Wood, Shadow Wood)
    MagicFire,      // Spreads fire (Magic Fire)
    MagicBlob,      // Grows blobs (Gooey Blob)
    AlignmentSpell  // Pure alignment shift (LAW-1, LAW-2, CHAOS-1, CHAOS-2)
}

/// <summary>
/// What occupies a board cell.
/// </summary>
public enum CellContent
{
    Empty,
    Wizard,
    Creature,
    DeadBody,       // A corpse — creatures can occupy the same square
    MagicTree,
    MagicFire,
    GooeyBlob,
    MagicCastle,
    DarkCitadel,
    ShadowWood,
    Wall
}

/// <summary>
/// Flags for special creature/wizard abilities.
/// </summary>
[Flags]
public enum SpecialAbility
{
    None        = 0,
    Flying      = 1 << 0,  // Can move over occupied squares
    Undead      = 1 << 1,  // Immune to melee from non-undead; only killed by other undead, ranged, or spells
    MountRider  = 1 << 2,  // Wizard is riding this creature
    Ranged      = 1 << 3,  // Has a ranged attack
    Subversion  = 1 << 4,  // Can attempt to take over enemy creatures
}
