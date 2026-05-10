# Chaos C# Port — Z80 Binary Verification Report

## Verified against: chaos.z80 snapshot + Sean Irvine's 1996 disassembly

---

## BUG #1 (CRITICAL): Manoeuvre & MagicResistance SWAPPED for all 33 creatures

The creature record byte layout at address 0xE463 is:

    byte[0] = Combat
    byte[1] = Ranged Combat
    byte[2] = Range
    byte[3] = Defence
    byte[4] = Movement
    byte[5] = Magic Resistance   ← our code had Manoeuvre here
    byte[6] = Manoeuvre           ← our code had MagicResistance here

**Every Cr() call in SpellDatabase.cs had the 6th and 7th arguments backwards.**

Confirmed via Sean Irvine's column labels and verified against known creature values
(e.g. Elf: MagRes=5 Man=7, Bear: MagRes=6 Man=2).

---

## BUG #2: Board is 15×10, not 10×10

Boundary check code at 0xBE58:

    LD A, 15 ; CP C ; RET M   → column must be < 15
    LD A, 10 ; CP B ; RET M   → row must be < 10

The arena is **15 columns × 10 rows**. State tables are 320 bytes
(32 bytes/row × 10 rows, with 17 unused border columns per row).

GameBoard.cs must change: `Width = 15`, `Height = 10`.

---

## BUG #3: Dragon casting chances are 0%, not 10%

From the spell table at 0x7D60:

| Creature      | Our code | Binary | Note                               |
| ------------- | -------- | ------ | ---------------------------------- |
| Green Dragon  | 10%      | 0%     | Base 0%, clamped to 10% at runtime |
| Red Dragon    | 10%      | 0%     | Same                               |
| Golden Dragon | 10%      | 0%     | Same                               |

The GetEffectiveCastingChance() clamp handles the minimum—the BASE value should be 0.

---

## BUG #4: Disbelieve casting chance is 90%, not 100%

Spell table byte[1] = 9 → 90%. Disbelieve can actually fail in the original game
(though a lawful world can push it above 100%). Our code hardcoded 100%.

---

## BUG #5: ALL non-creature spell casting chances are wrong

Every single non-creature spell has the wrong casting chance:

| Spell        | Our code | Actual  | Spell        | Our code | Actual  |
| ------------ | -------- | ------- | ------------ | -------- | ------- |
| Gooey Blob   | 100%     | **80%** | Magic Fire   | 100%     | **70%** |
| Lightning    | 100%     | **90%** | Magic Bolt   | 100%     | **90%** |
| Vengeance    | 80%      | **70%** | Dark Power   | 50%      | **40%** |
| Decree       | 80%      | **70%** | Justice      | 50%      | **40%** |
| Magic Shield | 80%      | **60%** | Magic Armour | 60%      | **30%** |
| Magic Sword  | 60%      | **30%** | Magic Knife  | 80%      | **60%** |
| Magic Bow    | 50%      | **40%** | Magic Wings  | 60%      | **40%** |
| Shadow Form  | 80%      | **60%** | Subversion   | 100%     | **90%** |
| Raise Dead   | 60%      | **40%** | Turmoil      | 100%     | **90%** |
| Magic Wood   | 80%      | **70%** | Shadow Wood  | 50%      | **30%** |
| Magic Castle | 50%      | **40%** | Dark Citadel | 50%      | **40%** |
| Wall         | 80%      | **70%** |              |          |         |

---

## BUG #6: Wrong alignment shifts for 3 spells

| Spell       | Our code | Actual |
| ----------- | -------- | ------ |
| Gooey Blob  | 0        | **-1** |
| Magic Fire  | 0        | **-1** |
| Shadow Form | -1       | **0**  |

---

## BUG #7: Spell ranges are wrong for many spells

Our code assumed adjacent-only (range 1) for terrain spells. The actual ranges
from the spell table's "2× max distance" field:

| Spell        | Our range | Actual range            |
| ------------ | --------- | ----------------------- |
| Gooey Blob   | adjacent  | **6**                   |
| Magic Fire   | adjacent  | **6**                   |
| Wall         | adjacent  | **6**                   |
| Magic Wood   | adjacent  | **8**                   |
| Shadow Wood  | adjacent  | **8**                   |
| Magic Castle | adjacent  | **8**                   |
| Dark Citadel | adjacent  | **8**                   |
| Subversion   | unlimited | **7**                   |
| Raise Dead   | unlimited | **4**                   |
| Disbelieve   | unlimited | **unlimited** (correct) |

---

## BUG #8: 4 missing spells — LAW-1, LAW-2, CHAOS-1, CHAOS-2

Pure alignment-shifting spells that exist in the spell table:

| Spell   | Cast% | Alignment shift |
| ------- | ----- | --------------- |
| LAW-1   | 70%   | +2              |
| LAW-2   | 50%   | +4              |
| CHAOS-1 | 70%   | -2              |
| CHAOS-2 | 50%   | -4              |

These are significant strategic spells for manipulating world alignment.

---

## BUG #9: Troll should NOT be a castable creature

The Troll has sprite data and creature stats in RAM (at 0xE70D) but has
**no entry in the spell table** at 0x7D60. Sean Irvine confirms the Troll
is unused in gameplay. Our code incorrectly included it as a summonable creature.

---

## BUG #10: Duplicate spell entries affect dealing probability

Several spells appear TWICE in the 65-entry spell table, doubling their
chance of appearing in a wizard's hand:

- Blind / Magic Bolt (obj 112): ×2
- Lightning (obj 98): ×2
- Subversion (obj 117): ×2
- Raise Dead (obj 115): ×2
- Turmoil (obj 118): ×2

Our DealSpellHand() treated each spell as equally likely.

---

## BUG #11: Turmoil excluded from normal spell dealing

Julian Gollop confirmed: Turmoil was deliberately left out of the initial
spell allocation. It can only be obtained through Magic Wood (which grants
a random new spell). Our code dealt Turmoil normally.

---

## BUG #12: Blind is a ranged attack spell, not a misc spell

"Blind" (obj 112) uses the same casting routine as Lightning (0x9C59).
It's a ranged attack spell with 90% chance and range 6, not a utility spell.
It also appears to share identity with Magic Bolt in the spell table.

---

## BUG #13: Missing spells from gameplay

Sean Irvine notes these spell NAMES exist in the string table but do not
appear during normal play: Blind, Tempest, Teleport, Dead Revenge,
Consecration, Dispel, Counter Spell, Magic Sleep. Some of these may have
been intended for the game but were never connected to working code.
Our port should NOT include them as castable spells.

---

## Appendix A: Verified creature stats (correct column order)

Source: creature data table at 0xE463, 38 bytes per record.
Cross-referenced with Sean Irvine's 1996 comp.sys.sinclair post.

| Name          | C   | RC  | R   | D   | Mv  | MR  | Ma  | Cast% | Align |
| ------------- | --- | --- | --- | --- | --- | --- | --- | ----- | ----- |
| King Cobra    | 4   | 0   | 0   | 1   | 1   | 6   | 1   | 80%   | +1    |
| Dire Wolf     | 3   | 0   | 0   | 2   | 3   | 7   | 2   | 80%   | -1    |
| Goblin        | 2   | 0   | 0   | 4   | 1   | 4   | 4   | 80%   | -1    |
| Crocodile     | 5   | 0   | 0   | 6   | 1   | 2   | 2   | 70%   | 0     |
| Faun          | 3   | 0   | 0   | 2   | 1   | 7   | 8   | 70%   | -1    |
| Lion          | 6   | 0   | 0   | 4   | 4   | 8   | 3   | 50%   | +1    |
| Elf           | 1   | 2   | 6   | 2   | 1   | 5   | 7   | 60%   | +2    |
| Orc           | 2   | 0   | 0   | 1   | 1   | 4   | 4   | 90%   | -1    |
| Bear          | 6   | 0   | 0   | 7   | 2   | 6   | 2   | 50%   | +1    |
| Gorilla       | 6   | 0   | 0   | 5   | 1   | 4   | 2   | 60%   | 0     |
| Ogre          | 4   | 0   | 0   | 7   | 1   | 3   | 6   | 60%   | -1    |
| Hydra         | 7   | 0   | 0   | 8   | 1   | 4   | 6   | 40%   | -1    |
| Giant Rat     | 1   | 0   | 0   | 1   | 3   | 8   | 2   | 90%   | 0     |
| Giant         | 9   | 0   | 0   | 7   | 2   | 6   | 5   | 30%   | +1    |
| Horse         | 1   | 0   | 0   | 3   | 4   | 8   | 1   | 80%   | +1    |
| Unicorn       | 5   | 0   | 0   | 4   | 4   | 9   | 7   | 50%   | +2    |
| Centaur       | 1   | 2   | 4   | 3   | 4   | 5   | 5   | 60%   | +1    |
| Pegasus       | 2   | 0   | 0   | 4   | 5   | 6   | 7   | 50%   | +2    |
| Gryphon       | 3   | 0   | 0   | 5   | 5   | 5   | 6   | 50%   | +1    |
| Manticore     | 3   | 1   | 3   | 6   | 5   | 6   | 8   | 30%   | -1    |
| Bat           | 1   | 0   | 0   | 1   | 5   | 9   | 4   | 70%   | -1    |
| Green Dragon  | 5   | 4   | 6   | 8   | 3   | 4   | 4   | 0%    | -1    |
| Red Dragon    | 7   | 3   | 5   | 9   | 3   | 4   | 5   | 0%    | -1    |
| Golden Dragon | 9   | 5   | 4   | 9   | 3   | 5   | 5   | 0%    | +2    |
| Harpy         | 4   | 0   | 0   | 2   | 5   | 8   | 5   | 50%   | -1    |
| Eagle         | 3   | 0   | 0   | 3   | 6   | 8   | 2   | 60%   | +1    |
| Vampire       | 6   | 0   | 0   | 8   | 4   | 6   | 5   | 10%   | -2    |
| Ghost         | 1   | 0   | 0   | 3   | 2   | 9   | 6   | 40%   | -1    |
| Spectre       | 4   | 0   | 0   | 2   | 1   | 6   | 4   | 50%   | -1    |
| Wraith        | 5   | 0   | 0   | 5   | 2   | 4   | 5   | 40%   | -1    |
| Skeleton      | 3   | 0   | 0   | 2   | 1   | 3   | 4   | 60%   | -1    |
| Zombie        | 1   | 0   | 0   | 1   | 1   | 2   | 3   | 80%   | -1    |

C=Combat, RC=Ranged Combat, R=Range, D=Defence, Mv=Movement,
MR=Magic Resistance, Ma=Manoeuvre

---

## Appendix B: Complete spell table from 0x7D60

Source: 65 entries × 7 bytes each at address 32096 (0x7D60).
Format: ObjectNum, CastChance/10, 2×MaxDistance, AlignShift, ???, RoutineAddr.

| #   | Spell         | Cast% | Range | Align | Routine | Notes                           |
| --- | ------------- | ----- | ----- | ----- | ------- | ------------------------------- |
| 0   | Disbelieve    | 90%   | ∞     | 0     | 0x99F1  | Always available                |
| 1   | King Cobra    | 80%   | 1     | +1    | 0x9975  | Creature                        |
| 2   | Dire Wolf     | 80%   | 1     | -1    | 0x9975  | Creature                        |
| 3   | Goblin        | 80%   | 1     | -1    | 0x9975  | Creature                        |
| 4   | Crocodile     | 70%   | 1     | 0     | 0x9975  | Creature                        |
| 5   | Faun          | 70%   | 1     | -1    | 0x9975  | Creature                        |
| 6   | Lion          | 50%   | 1     | +1    | 0x9975  | Creature                        |
| 7   | Elf           | 60%   | 1     | +2    | 0x9975  | Creature                        |
| 8   | Orc           | 90%   | 1     | -1    | 0x9975  | Creature                        |
| 9   | Bear          | 50%   | 1     | +1    | 0x9975  | Creature                        |
| 10  | Gorilla       | 60%   | 1     | 0     | 0x9975  | Creature                        |
| 11  | Ogre          | 60%   | 1     | -1    | 0x9975  | Creature                        |
| 12  | Hydra         | 40%   | 1     | -1    | 0x9975  | Creature                        |
| 13  | Giant Rat     | 90%   | 1     | 0     | 0x9975  | Creature                        |
| 14  | Giant         | 30%   | 1     | +1    | 0x9975  | Creature                        |
| 15  | Horse         | 80%   | 1     | +1    | 0x9975  | Creature, mount                 |
| 16  | Unicorn       | 50%   | 1     | +2    | 0x9975  | Creature, mount                 |
| 17  | Centaur       | 60%   | 1     | +1    | 0x9975  | Creature, ranged                |
| 18  | Pegasus       | 50%   | 1     | +2    | 0x9975  | Creature, mount, flying         |
| 19  | Gryphon       | 50%   | 1     | +1    | 0x9975  | Creature, mount, flying         |
| 20  | Manticore     | 30%   | 1     | -1    | 0x9975  | Creature, mount, flying, ranged |
| 21  | Bat           | 70%   | 1     | -1    | 0x9975  | Creature, flying                |
| 22  | Green Dragon  | 0%    | 1     | -1    | 0x9975  | Creature, mount, flying, ranged |
| 23  | Red Dragon    | 0%    | 1     | -1    | 0x9975  | Creature, mount, flying, ranged |
| 24  | Golden Dragon | 0%    | 1     | +2    | 0x9975  | Creature, mount, flying, ranged |
| 25  | Harpy         | 50%   | 1     | -1    | 0x9975  | Creature, flying                |
| 26  | Eagle         | 60%   | 1     | +1    | 0x9975  | Creature, flying                |
| 27  | Vampire       | 10%   | 1     | -2    | 0x9975  | Creature, flying, undead        |
| 28  | Ghost         | 40%   | 1     | -1    | 0x9975  | Creature, flying, undead        |
| 29  | Spectre       | 50%   | 1     | -1    | 0x9975  | Creature, flying, undead        |
| 30  | Wraith        | 40%   | 1     | -1    | 0x9975  | Creature, undead                |
| 31  | Skeleton      | 60%   | 1     | -1    | 0x9975  | Creature, undead                |
| 32  | Zombie        | 80%   | 1     | -1    | 0x9975  | Creature, undead                |
| 33  | Gooey Blob    | 80%   | 6     | -1    | 0x9975  | Terrain, spreads                |
| 34  | Magic Fire    | 70%   | 6     | -1    | 0x9975  | Terrain, spreads                |
| 35  | Magic Wood    | 70%   | 8     | +1    | 0x9ADD  | Terrain, grants spells          |
| 36  | Shadow Wood   | 30%   | 8     | -1    | 0x9ADD  | Terrain                         |
| 37  | Magic Castle  | 40%   | 8     | +1    | 0x9ADD  | Terrain, shelter                |
| 38  | Dark Citadel  | 40%   | 8     | -1    | 0x9ADD  | Terrain, shelter                |
| 39  | Wall          | 70%   | 6     | 0     | 0x9B76  | Terrain, blocker                |
| 40  | Magic Bolt    | 90%   | 6     | 0     | 0x9C59  | Attack (obj 112)                |
| 41  | Magic Bolt    | 90%   | 6     | 0     | 0x9C59  | Duplicate entry                 |
| 42  | Lightning     | 90%   | 4     | 0     | 0x9C59  | Attack (obj 98)                 |
| 43  | Lightning     | 90%   | 4     | 0     | 0x9C59  | Duplicate entry                 |
| 44  | Vengeance     | 70%   | 15    | -1    | 0x9DE0  | Attack                          |
| 45  | Decree        | 70%   | 15    | +1    | 0x9DE0  | Attack                          |
| 46  | Dark Power    | 40%   | 15    | -2    | 0x9DE0  | Attack                          |
| 47  | Justice       | 40%   | 15    | +2    | 0x9DE0  | Attack                          |
| 48  | Magic Shield  | 60%   | 0     | +1    | 0x8404  | Equipment                       |
| 49  | Magic Armour  | 30%   | 0     | +1    | 0x836A  | Equipment                       |
| 50  | Magic Sword   | 30%   | 0     | +1    | 0x839C  | Equipment                       |
| 51  | Magic Knife   | 60%   | 0     | +1    | 0x83D0  | Equipment                       |
| 52  | Magic Bow     | 40%   | 0     | +1    | 0x846A  | Equipment                       |
| 53  | Magic Wings   | 40%   | 0     | 0     | 0x8438  | Self buff                       |
| 54  | LAW-1         | 70%   | 0     | +2    | 0x84B0  | Alignment shift                 |
| 55  | LAW-2         | 50%   | 0     | +4    | 0x84B0  | Alignment shift                 |
| 56  | CHAOS-1       | 70%   | 0     | -2    | 0x84B0  | Alignment shift                 |
| 57  | CHAOS-2       | 50%   | 0     | -4    | 0x84B0  | Alignment shift                 |
| 58  | Shadow Form   | 60%   | 0     | 0     | 0x84C0  | Self buff                       |
| 59  | Subversion    | 90%   | 7     | 0     | 0x84F7  | Targeted                        |
| 60  | Subversion    | 90%   | 7     | 0     | 0x84F7  | Duplicate entry                 |
| 61  | Raise Dead    | 40%   | 4     | -1    | 0x85F6  | Targeted                        |
| 62  | Raise Dead    | 40%   | 4     | -1    | 0x85F6  | Duplicate entry                 |
| 63  | Turmoil       | 90%   | 10    | -1    | 0x86EF  | Excluded from dealing           |
| 64  | Turmoil       | 90%   | 10    | -1    | 0x86EF  | Duplicate, excluded             |

---

## Summary

| Category               | Count                          | Impact                               |
| ---------------------- | ------------------------------ | ------------------------------------ |
| Stats swapped (MR↔Ma)  | All 33 creatures               | Breaks all combat & spell resistance |
| Wrong board size       | 15×10 vs 10×10                 | Breaks board layout entirely         |
| Wrong casting chances  | 23+ spells                     | Changes game balance dramatically    |
| Wrong alignment shifts | 3 spells                       | Affects world alignment tracking     |
| Wrong spell ranges     | 9 spells                       | Changes targeting rules              |
| Missing spells         | 4 alignment spells             | Removes strategic options            |
| Incorrectly included   | Troll + 8 unimplemented spells | Adds non-existent content            |
| Dealing probabilities  | 5 spells should be doubled     | Changes spell distribution           |
