namespace Chaos.Data;

using Chaos.Enums;
using Chaos.Models;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The complete spell list — ALL values verified against the chaos.z80 binary.
///
/// Creature stats: extracted from records at 0xE463+ (38 bytes each).
///   Byte order: Combat, RangedCombat, Range, Defence, Movement,
///               MagicResistance, Manoeuvre, CastChance/10, AlignSign, Strength.
///
/// Spell properties: extracted from the 7-byte records at 0x7D60 (32096 decimal).
///   Byte order: ObjectNum, CastChance/10, 2×MaxDistance, AlignShift, ?, RoutineAddr.
///
/// NOTE: Sean Irvine's 1996 disassembly confirms byte[5]=MagicResistance,
/// byte[6]=Manoeuvre. Many fan wikis have these two columns swapped.
///
/// NOTE: Several spell names exist in the string table (Blind, Tempest,
/// Teleport, Dead Revenge, Consecration, Dispel, Counter Spell, Magic Sleep)
/// but were never wired to working spell code. They are NOT included here.
/// The Troll has sprites and stats but no spell table entry — also excluded.
/// </summary>
public static class SpellDatabase
{
    public static List<Spell> CreateAllSpells()
    {
        var spells = new List<Spell>();

        // ── Creature spells (stats from 0xE463+, chances from 0x7D60+) ──
        //
        // Cr() parameter order matches the BINARY byte layout:
        //   Combat, RangedCombat, Range, Defence, Movement,
        //   MagicResistance, Manoeuvre, Abilities, AlignmentValue, isMount
        //
        // NOTE on creature ordering: the original groups creatures by type
        // (mounts together, flyers together, undead together) for efficient
        // range-checking in Z80 code. We preserve Sean Irvine's internal
        // numbering order (objects 2–33) from the spell table.

        //                                Name              Cast% Align  C  RC  R  D Mv MR Ma  Abilities                                       AlVal  Mount
        spells.Add(MakeCreature("King Cobra",       80,  1, Cr( 4, 0, 0, 1, 1, 6, 1, SpecialAbility.None,                                    1)));
        spells.Add(MakeCreature("Dire Wolf",        80, -1, Cr( 3, 0, 0, 2, 3, 7, 2, SpecialAbility.None,                                   -1)));
        spells.Add(MakeCreature("Goblin",           80, -1, Cr( 2, 0, 0, 4, 1, 4, 4, SpecialAbility.None,                                   -1)));
        spells.Add(MakeCreature("Crocodile",        70,  0, Cr( 5, 0, 0, 6, 1, 2, 2, SpecialAbility.None,                                    0)));
        spells.Add(MakeCreature("Faun",             70, -1, Cr( 3, 0, 0, 2, 1, 7, 8, SpecialAbility.None,                                   -1)));
        spells.Add(MakeCreature("Lion",             50,  1, Cr( 6, 0, 0, 4, 4, 8, 3, SpecialAbility.None,                                    1)));
        spells.Add(MakeCreature("Elf",              60,  2, Cr( 1, 2, 6, 2, 1, 5, 7, SpecialAbility.Ranged,                                  2)));
        spells.Add(MakeCreature("Orc",              90, -1, Cr( 2, 0, 0, 1, 1, 4, 4, SpecialAbility.None,                                   -1)));
        spells.Add(MakeCreature("Bear",             50,  1, Cr( 6, 0, 0, 7, 2, 6, 2, SpecialAbility.None,                                    1)));
        spells.Add(MakeCreature("Gorilla",          60,  0, Cr( 6, 0, 0, 5, 1, 4, 2, SpecialAbility.None,                                    0)));
        spells.Add(MakeCreature("Ogre",             60, -1, Cr( 4, 0, 0, 7, 1, 3, 6, SpecialAbility.None,                                   -1)));
        spells.Add(MakeCreature("Hydra",            40, -1, Cr( 7, 0, 0, 8, 1, 4, 6, SpecialAbility.None,                                   -1)));
        spells.Add(MakeCreature("Giant Rat",        90,  0, Cr( 1, 0, 0, 1, 3, 8, 2, SpecialAbility.None,                                    0)));
        spells.Add(MakeCreature("Giant",            30,  1, Cr( 9, 0, 0, 7, 2, 6, 5, SpecialAbility.None,                                    1)));
        spells.Add(MakeCreature("Horse",            80,  1, Cr( 1, 0, 0, 3, 4, 8, 1, SpecialAbility.None,                                    1, isMount: true)));
        spells.Add(MakeCreature("Unicorn",          50,  2, Cr( 5, 0, 0, 4, 4, 9, 7, SpecialAbility.None,                                    2, isMount: true)));
        spells.Add(MakeCreature("Centaur",          60,  1, Cr( 1, 2, 4, 3, 4, 5, 5, SpecialAbility.Ranged,                                  1)));
        spells.Add(MakeCreature("Pegasus",          50,  2, Cr( 2, 0, 0, 4, 5, 6, 7, SpecialAbility.Flying,                                  2, isMount: true)));
        spells.Add(MakeCreature("Gryphon",          50,  1, Cr( 3, 0, 0, 5, 5, 5, 6, SpecialAbility.Flying,                                  1, isMount: true)));
        spells.Add(MakeCreature("Manticore",        30, -1, Cr( 3, 1, 3, 6, 5, 6, 8, SpecialAbility.Flying | SpecialAbility.Ranged,          -1, isMount: true)));
        spells.Add(MakeCreature("Bat",              70, -1, Cr( 1, 0, 0, 1, 5, 9, 4, SpecialAbility.Flying,                                 -1)));
        spells.Add(MakeCreature("Green Dragon",      0, -1, Cr( 5, 4, 6, 8, 3, 4, 4, SpecialAbility.Flying | SpecialAbility.Ranged,          -1, isMount: true)));
        spells.Add(MakeCreature("Red Dragon",        0, -1, Cr( 7, 3, 5, 9, 3, 4, 5, SpecialAbility.Flying | SpecialAbility.Ranged,          -1, isMount: true)));
        spells.Add(MakeCreature("Golden Dragon",     0,  2, Cr( 9, 5, 4, 9, 3, 5, 5, SpecialAbility.Flying | SpecialAbility.Ranged,           2, isMount: true)));
        spells.Add(MakeCreature("Harpy",            50, -1, Cr( 4, 0, 0, 2, 5, 8, 5, SpecialAbility.Flying,                                 -1)));
        spells.Add(MakeCreature("Eagle",            60,  1, Cr( 3, 0, 0, 3, 6, 8, 2, SpecialAbility.Flying,                                  1)));
        spells.Add(MakeCreature("Vampire",          10, -2, Cr( 6, 0, 0, 8, 4, 6, 5, SpecialAbility.Flying | SpecialAbility.Undead,          -2)));
        spells.Add(MakeCreature("Ghost",            40, -1, Cr( 1, 0, 0, 3, 2, 9, 6, SpecialAbility.Flying | SpecialAbility.Undead,          -1)));
        spells.Add(MakeCreature("Spectre",          50, -1, Cr( 4, 0, 0, 2, 1, 6, 4, SpecialAbility.Flying | SpecialAbility.Undead,          -1)));
        spells.Add(MakeCreature("Wraith",           40, -1, Cr( 5, 0, 0, 5, 2, 4, 5, SpecialAbility.Undead,                                 -1)));
        spells.Add(MakeCreature("Skeleton",         60, -1, Cr( 3, 0, 0, 2, 1, 3, 4, SpecialAbility.Undead,                                 -1)));
        spells.Add(MakeCreature("Zombie",           80, -1, Cr( 1, 0, 0, 1, 1, 2, 3, SpecialAbility.Undead,                                 -1)));

        // ── Disbelieve (always available, never consumed) ───────────
        // Spell table: CastChance=9 (90%), range=infinite, align=0

        spells.Add(new Spell
        {
            Name = "Disbelieve", Category = SpellCategory.DisbelieveIllusion,
            CastingChance = 90, AlignmentShift = 0, Range = 99
        });

        // ── Terrain / spreading spells ──────────────────────────────
        // These have REAL range from the spell table, NOT adjacent-only.
        // Gooey Blob and Magic Fire are Chaotic (align -1), not neutral.

        spells.Add(new Spell { Name = "Gooey Blob",    Category = SpellCategory.MagicBlob,  CastingChance = 80, AlignmentShift = -1, Range = 6 });
        spells.Add(new Spell { Name = "Magic Fire",    Category = SpellCategory.MagicFire,  CastingChance = 70, AlignmentShift = -1, Range = 6 });
        spells.Add(new Spell { Name = "Magic Wood",    Category = SpellCategory.MagicTree,  CastingChance = 70, AlignmentShift =  1, Range = 8 });
        spells.Add(new Spell { Name = "Shadow Wood",   Category = SpellCategory.MagicTree,  CastingChance = 30, AlignmentShift = -1, Range = 8 });
        spells.Add(new Spell { Name = "Magic Castle",  Category = SpellCategory.MagicMisc,  CastingChance = 40, AlignmentShift =  1, Range = 8 });
        spells.Add(new Spell { Name = "Dark Citadel",  Category = SpellCategory.MagicMisc,  CastingChance = 40, AlignmentShift = -1, Range = 8 });
        spells.Add(new Spell { Name = "Wall",          Category = SpellCategory.MagicMisc,  CastingChance = 70, AlignmentShift =  0, Range = 6 });

        // ── Ranged attack spells ────────────────────────────────────
        // Lightning: obj 98, range 4, 90%. Magic Bolt shares obj 112, range 6, 90%.
        // Vengeance/Decree: range 15, 70%. Dark Power/Justice: range 15, 40%.

        spells.Add(new Spell { Name = "Lightning",    Category = SpellCategory.MagicAttack, CastingChance = 90, AlignmentShift =  0, AttackPower = 3, Range = 4 });
        spells.Add(new Spell { Name = "Magic Bolt",   Category = SpellCategory.MagicAttack, CastingChance = 90, AlignmentShift =  0, AttackPower = 2, Range = 6 });
        spells.Add(new Spell { Name = "Vengeance",    Category = SpellCategory.MagicAttack, CastingChance = 70, AlignmentShift = -1, AttackPower = 4, Range = 15 });
        spells.Add(new Spell { Name = "Dark Power",   Category = SpellCategory.MagicAttack, CastingChance = 40, AlignmentShift = -2, AttackPower = 5, Range = 15 });
        spells.Add(new Spell { Name = "Decree",       Category = SpellCategory.MagicAttack, CastingChance = 70, AlignmentShift =  1, AttackPower = 3, Range = 15 });
        spells.Add(new Spell { Name = "Justice",      Category = SpellCategory.MagicAttack, CastingChance = 40, AlignmentShift =  2, AttackPower = 5, Range = 15 });

        // ── Equipment spells (cast on self, range 0) ────────────────

        spells.Add(new Spell { Name = "Magic Shield",  Category = SpellCategory.MagicDefence, CastingChance = 60,  AlignmentShift = 1 });
        spells.Add(new Spell { Name = "Magic Armour",  Category = SpellCategory.MagicDefence, CastingChance = 30,  AlignmentShift = 1 });
        spells.Add(new Spell { Name = "Magic Sword",   Category = SpellCategory.MagicDefence, CastingChance = 30,  AlignmentShift = 1 });
        spells.Add(new Spell { Name = "Magic Knife",   Category = SpellCategory.MagicDefence, CastingChance = 60,  AlignmentShift = 1 });
        spells.Add(new Spell { Name = "Magic Bow",     Category = SpellCategory.MagicDefence, CastingChance = 40,  AlignmentShift = 1 });

        // ── Utility spells ──────────────────────────────────────────

        spells.Add(new Spell { Name = "Magic Wings",   Category = SpellCategory.MagicMisc, CastingChance = 40,  AlignmentShift =  0 });
        spells.Add(new Spell { Name = "Shadow Form",   Category = SpellCategory.MagicMisc, CastingChance = 60,  AlignmentShift =  0 });
        spells.Add(new Spell { Name = "Subversion",    Category = SpellCategory.MagicMisc, CastingChance = 90,  AlignmentShift =  0, Range = 7 });
        spells.Add(new Spell { Name = "Raise Dead",    Category = SpellCategory.MagicMisc, CastingChance = 40,  AlignmentShift = -1, Range = 4 });

        // ── Alignment-shifting spells ───────────────────────────────
        // These exist in the spell table but were missing from our first port.
        // They have large alignment shifts — a key strategic tool.

        spells.Add(new Spell { Name = "Law-1",    Category = SpellCategory.AlignmentSpell, CastingChance = 70, AlignmentShift =  2 });
        spells.Add(new Spell { Name = "Law-2",    Category = SpellCategory.AlignmentSpell, CastingChance = 50, AlignmentShift =  4 });
        spells.Add(new Spell { Name = "Chaos-1",  Category = SpellCategory.AlignmentSpell, CastingChance = 70, AlignmentShift = -2 });
        spells.Add(new Spell { Name = "Chaos-2",  Category = SpellCategory.AlignmentSpell, CastingChance = 50, AlignmentShift = -4 });

        // ── Turmoil (special — NOT dealt normally) ──────────────────
        // Julian Gollop confirmed Turmoil is excluded from initial dealing.
        // It can only be obtained through Magic Wood. We store it here but
        // DealSpellHand() filters it out.

        spells.Add(new Spell { Name = "Turmoil", Category = SpellCategory.MagicMisc, CastingChance = 90, AlignmentShift = -1, Range = 10 });

        return spells;
    }

    /// <summary>
    /// Deal a random hand of spells to a wizard.
    /// The original deals from the 65-entry spell table at 0x7D60, where
    /// some spells appear twice (doubling their draw probability):
    ///   Lightning ×2, Magic Bolt ×2, Subversion ×2, Raise Dead ×2.
    /// Turmoil is excluded from dealing (only obtainable via Magic Wood).
    /// Disbelieve is always granted for free.
    /// </summary>
    public static List<Spell> DealSpellHand(Random rng, int handSize = 14)
    {
        var allSpells = CreateAllSpells();

        // Disbelieve is always granted
        var hand = new List<Spell>
        {
            allSpells.First(s => s.Name == "Disbelieve")
        };

        // Build the draw pool: exclude Disbelieve and Turmoil,
        // and add duplicate entries for spells that appear twice in the original.
        var pool = new List<Spell>();
        foreach (var spell in allSpells)
        {
            if (spell.Name == "Disbelieve") continue;
            if (spell.Name == "Turmoil") continue;

            pool.Add(spell);

            // These spells appear twice in the original spell table,
            // doubling their probability of being dealt.
            if (spell.Name is "Lightning" or "Magic Bolt"
                           or "Subversion" or "Raise Dead")
            {
                pool.Add(spell);
            }
        }

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        hand.AddRange(pool.Take(handSize));
        return hand;
    }

    /// <summary>
    /// Get the Turmoil spell (granted only by Magic Wood).
    /// </summary>
    public static Spell GetTurmoilSpell()
    {
        return CreateAllSpells().First(s => s.Name == "Turmoil");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static Spell MakeCreature(string name, int castChance, int alignShift, CreatureStats stats)
    {
        stats.Name = name;
        return new Spell
        {
            Name = name,
            Category = SpellCategory.Creature,
            CastingChance = castChance,
            AlignmentShift = alignShift,
            CreatureData = stats,
            Range = 1  // Creatures are summoned to adjacent squares
        };
    }

    /// <summary>
    /// Build creature stats. Parameter order matches the Z80 binary layout:
    ///   Combat, RangedCombat, Range, Defence, Movement,
    ///   MagicResistance, Manoeuvre, Abilities, AlignValue, isMount
    /// </summary>
    private static CreatureStats Cr(
        int combat, int rangedCombat, int range, int defence,
        int movement, int magicResistance, int manoeuvre,
        SpecialAbility abilities, int alignVal,
        bool isMount = false)
    {
        return new CreatureStats
        {
            Combat = combat,
            RangedCombat = rangedCombat,
            Range = range,
            Defence = defence,
            Movement = movement,
            MagicResistance = magicResistance,
            Manoeuvre = manoeuvre,
            Abilities = abilities,
            AlignmentValue = alignVal,
            IsMount = isMount,
            Alignment = alignVal switch
            {
                > 0 => Alignment.Law,
                < 0 => Alignment.Chaos,
                _   => Alignment.Neutral
            }
        };
    }

    /// <summary>
    /// Default wizard names from the original game.
    /// </summary>
    public static readonly string[] DefaultWizardNames =
     {
        "Julian", "Gandalf", "Great Fogey", "Dyerarti",
        "Gowin", "Merlin", "Ilian Rane", "Asimono Zark"
    };

    /// <summary>
    /// Base stats per wizard (C, D, MR, Ma), verified from Z80 binary.
    /// All wizards have RC=0, Range=0, Movement=1.
    /// </summary>
    public static readonly (int Combat, int Defence, int MagicRes, int Manoeuvre)[] WizardBaseStats =
    {
        (1, 1, 3, 7),  // Julian
        (1, 1, 3, 6),  // Gandalf
        (3, 3, 5, 6),  // Great Fogey
        (1, 2, 6, 7),  // Dyerarti
        (2, 2, 5, 0),  // Gowin
        (1, 4, 4, 0),  // Merlin
        (1, 2, 4, 0),  // Ilian Rane
        (3, 2, 6, 0),  // Asimono Zark
    };
}
