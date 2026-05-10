# Chaos: The Battle of Wizards — Spell Guide

Welcome to Chaos! This guide explains every spell in the game, what it
does, and how to use it effectively. Each wizard starts with a random hand
of about 14 spells plus Disbelieve, which is always available.

---

## How Spells Work

Every spell has a **casting chance** shown as a percentage. When you try
to cast a spell, the game rolls against this number — if the roll is under
your chance, the spell succeeds. If not, it fizzles and you've wasted your
turn (and the spell is still used up).

Spells are aligned as **Lawful**, **Chaotic**, or **Neutral**. Each time a
Lawful spell is cast, the world shifts toward Law; each Chaotic spell shifts
it toward Chaos. This matters because a Lawful world makes Lawful spells
easier to cast and a Chaotic world makes Chaotic spells easier. Neutral
spells are unaffected. The current world alignment is shown at the top of
the screen.

Most spells can only be cast once. The exception is Disbelieve, which you
always have available every turn.

---

## Disbelieve

**Casting Chance:** 90% · **Alignment:** Neutral · **Range:** Unlimited

The most important spell in the game, and the only one you never lose.
Disbelieve targets any creature on the board. If that creature is an
**illusion**, it vanishes instantly. If it's real, nothing happens — but
now you know.

This is the counterplay to the illusion system. Any creature spell can be
cast as a guaranteed illusion (see below), so Disbelieve keeps the bluffing
game honest. Use it when an opponent summons something suspiciously powerful
on their first turn.

---

## Creature Spells

Creature spells summon a monster onto any empty square next to your wizard.
The creature then fights for you — you control its movement and attacks on
subsequent turns. If your wizard is killed, all your creatures vanish.

### The Illusion Choice

When you select a creature spell, you're asked: **cast as illusion?**

- **Real casting** rolls against the casting chance. If it fails, the spell
  is wasted. If it succeeds, the creature is real and can only be killed
  through combat.
- **Illusion casting** always succeeds (100%). The creature looks and fights
  exactly like a real one — but if any wizard casts Disbelieve on it, it
  disappears instantly.

This is the core bluff of Chaos. A Golden Dragon illusion is terrifying
until someone calls your bluff. Conversely, nobody wants to waste their
Disbelieve on your very real Giant Rat.

### Creature Stats Explained

Each creature has these stats:

- **Combat (C):** Attack strength in melee. Higher is better.
- **Ranged Combat (RC):** Attack strength at distance. 0 means no ranged attack.
- **Range (R):** How far ranged attacks can reach.
- **Defence (D):** How hard it is to kill. Compared against the attacker's Combat.
- **Movement (Mv):** Squares it can move per turn.
- **Magic Resistance (MR):** Defence against magical attacks like Lightning.
- **Manoeuvre (Ma):** Ability to disengage from adjacent enemies. Also modifies
  combat when both attacker and defender are engaged with other enemies.

Combat works like this: `random(0–7) + attacker's Combat` vs
`random(0–7) + defender's Defence`. If the attack total is higher,
the defender is killed in one hit. There is no health — everything dies
in a single successful attack.

### Mounts

Some creatures can be **ridden** by your wizard. Move your wizard onto the
mount to ride it. While mounted, you use the mount's movement and flying
ability, giving your wizard much greater mobility. You can dismount later
if needed.

### Undead

Undead creatures cannot be melee attacked by non-undead creatures.

### Creature List

Creatures are listed below grouped roughly by type, from easiest to hardest
to summon.

#### Common Creatures

| Creature   | Combat | Ranged Combat | Range | Defence | Movement | Magic Resistance | Manoeuvre | Cast | Align   | Notes                                                                |
| ---------- | ------ | ------------- | ----- | ------- | -------- | ---------------- | --------- | ---- | ------- | -------------------------------------------------------------------- |
| Giant Rat  | 1      | 0             | 0     | 1       | 3        | 8                | 2         | 90%  | Neutral | Fast and weak, but very easy to summon and hard to target with magic |
| Orc        | 2      | 0             | 0     | 1       | 1        | 4                | 4         | 90%  | Chaos   | Cheap foot soldier                                                   |
| Goblin     | 2      | 0             | 0     | 4       | 1        | 4                | 4         | 80%  | Chaos   | Better defence than Orc                                              |
| Zombie     | 1      | 0             | 0     | 1       | 1        | 2                | 3         | 80%  | Chaos   | Undead — weak but immune to some effects                             |
| King Cobra | 4      | 0             | 0     | 1       | 1        | 6                | 1         | 80%  | Law     | Decent attack, fragile                                               |
| Dire Wolf  | 3      | 0             | 0     | 2       | 3        | 7                | 2         | 80%  | Chaos   | Good mobility for a cheap creature                                   |
| Horse      | 1      | 0             | 0     | 3       | 4        | 8                | 1         | 80%  | Law     | **Mount.** Weak fighter but lets your wizard move fast               |

#### Mid-Tier Creatures

| Creature  | Combat | Ranged Combat | Range | Defence | Movement | Magic Resistance | Manoeuvre | Cast | Align   | Notes                                               |
| --------- | ------ | ------------- | ----- | ------- | -------- | ---------------- | --------- | ---- | ------- | --------------------------------------------------- |
| Bat       | 1      | 0             | 0     | 1       | 5        | 9                | 4         | 70%  | Chaos   | **Flying.** Very fast scout, nearly immune to magic |
| Faun      | 3      | 0             | 0     | 2       | 1        | 7                | 8         | 70%  | Chaos   | Excellent manoeuvre — hard to pin down              |
| Crocodile | 5      | 0             | 0     | 6       | 1        | 2                | 2         | 70%  | Neutral | Strong and tough, but slow and easy to zap          |
| Elf       | 1      | 2             | 6     | 2       | 1        | 5                | 7         | 60%  | Law ×2  | **Ranged.** Weak melee but fires arrows at range 6  |
| Centaur   | 1      | 2             | 4     | 3       | 4        | 5                | 5         | 60%  | Law     | **Ranged.** Mobile archer — good all-rounder        |
| Gorilla   | 6      | 0             | 0     | 5       | 1        | 4                | 2         | 60%  | Neutral | Strong attacker, decent defence                     |
| Ogre      | 4      | 0             | 0     | 7       | 1        | 3                | 6         | 60%  | Chaos   | Very tough (D:7) — a solid defensive creature       |
| Skeleton  | 3      | 0             | 0     | 2       | 1        | 3                | 4         | 60%  | Chaos   | Undead. Fragile but reliable to summon              |
| Eagle     | 3      | 0             | 0     | 3       | 6        | 8                | 2         | 60%  | Law     | **Flying.** Fastest creature in the game (Mv:6)     |

#### Powerful Creatures

| Creature | Combat | Ranged Combat | Range | Defence | Movement | Magic Resistance | Manoeuvre | Cast | Align  | Notes                                               |
| -------- | ------ | ------------- | ----- | ------- | -------- | ---------------- | --------- | ---- | ------ | --------------------------------------------------- |
| Bear     | 6      | 0             | 0     | 7       | 2        | 6                | 2         | 50%  | Law    | Powerful and very tough                             |
| Lion     | 6      | 0             | 0     | 4       | 4        | 8                | 3         | 50%  | Law    | Strong, fast, and very magic-resistant              |
| Unicorn  | 5      | 0             | 0     | 4       | 4        | 9                | 7         | 50%  | Law ×2 | **Mount.** Highest magic resistance of any mount    |
| Pegasus  | 2      | 0             | 0     | 4       | 5        | 6                | 7         | 50%  | Law ×2 | **Mount, Flying.** Great mobility, decent stats     |
| Gryphon  | 3      | 0             | 0     | 5       | 5        | 5                | 6         | 50%  | Law    | **Mount, Flying.** Tougher than Pegasus             |
| Harpy    | 4      | 0             | 0     | 2       | 5        | 8                | 5         | 50%  | Chaos  | **Flying.** Fast with strong magic resistance       |
| Spectre  | 4      | 0             | 0     | 2       | 1        | 6                | 4         | 50%  | Chaos  | **Flying, Undead.** Unique combination              |
| Wraith   | 5      | 0             | 0     | 5       | 2        | 4                | 5         | 40%  | Chaos  | Undead. Well-rounded ground fighter                 |
| Ghost    | 1      | 0             | 0     | 3       | 2        | 9                | 6         | 40%  | Chaos  | **Flying, Undead.** Highest magic resistance (MR:9) |
| Hydra    | 7      | 0             | 0     | 8       | 1        | 4                | 6         | 40%  | Chaos  | Devastating attack and defence, but slow            |

#### Elite Creatures

| Creature      | Combat | Ranged Combat | Range | Defence | Movement | Magic Resistance | Manoeuvre | Cast | Align    | Notes                                               |
| ------------- | ------ | ------------- | ----- | ------- | -------- | ---------------- | --------- | ---- | -------- | --------------------------------------------------- |
| Giant         | 9      | 0             | 0     | 7       | 2        | 6                | 5         | 30%  | Law      | Highest melee attack in the game                    |
| Manticore     | 3      | 1             | 3     | 6       | 5        | 6                | 8         | 30%  | Chaos    | **Mount, Flying, Ranged.** Does everything          |
| Vampire       | 6      | 0             | 0     | 8       | 4        | 6                | 5         | 10%  | Chaos ×2 | **Flying, Undead.** Very powerful all-rounder       |
| Green Dragon  | 5      | 4             | 6     | 8       | 3        | 4                | 4         | 0%   | Chaos    | **Mount, Flying, Ranged.** Fire breath at range 6   |
| Red Dragon    | 7      | 3             | 5     | 9       | 3        | 4                | 5         | 0%   | Chaos    | **Mount, Flying, Ranged.** Strongest dragon fighter |
| Golden Dragon | 9      | 5             | 4     | 9       | 3        | 5                | 5         | 0%   | Law ×2   | **Mount, Flying, Ranged.** The ultimate creature    |

The three dragons have a 0% base casting chance — in a neutral world, they
have roughly a 10% chance. You'll need the world strongly shifted toward
their alignment (Chaotic for Green/Red, Lawful for Golden) to have a
realistic shot. Or just cast them as illusions and hope nobody Disbelieves.

---

## Attack Spells

These spells let your wizard blast a target from range. They roll your spell's
attack power against the target's Magic Resistance using the same formula as
combat: `random(0–7) + power` vs `random(0–7) + magic resistance`.

| Spell      | Cast | Range | Power | Align    | Best used against                          |
| ---------- | ---- | ----- | ----- | -------- | ------------------------------------------ |
| Lightning  | 90%  | 4     | 3     | Neutral  | Nearby creatures with low MR               |
| Magic Bolt | 90%  | 6     | 2     | Neutral  | Slightly weaker but longer range           |
| Vengeance  | 70%  | 15    | 4     | Chaos    | Anywhere on the board, strong              |
| Decree     | 70%  | 15    | 3     | Law      | Lawful counterpart to Vengeance            |
| Dark Power | 40%  | 15    | 5     | Chaos ×2 | The most powerful attack, but hard to cast |
| Justice    | 40%  | 15    | 5     | Law ×2   | Lawful counterpart to Dark Power           |

Lightning and Magic Bolt are your bread-and-butter ranged attacks — reliable
and always neutral. Vengeance/Decree are longer range but harder to cast.
Dark Power and Justice are devastating but unreliable at 40%.

Creatures with high Magic Resistance (like Bat at MR:9 or Unicorn at MR:9)
are nearly impossible to kill with attack spells. Use melee creatures
against them instead.

---

## Equipment Spells

These spells permanently boost your wizard's stats. They cast automatically
on yourself — no target needed. All are Lawful.

| Spell        | Cast | Effect               | Align |
| ------------ | ---- | -------------------- | ----- |
| Magic Knife  | 60%  | Combat +2            | Law   |
| Magic Sword  | 30%  | Combat +4            | Law   |
| Magic Shield | 60%  | Defence +2           | Law   |
| Magic Armour | 30%  | Defence +4           | Law   |
| Magic Bow    | 40%  | Grants ranged attack | Law   |

Magic Sword and Magic Armour are very powerful but only have a 30% base
chance. If the world is Lawful, these become much more castable. Magic
Knife and Shield are weaker but more reliable at 60%.

Magic Bow is unique — it gives your wizard a ranged attack, letting them
shoot at enemies from a distance. Very useful for staying alive while
your creatures do the melee fighting.

---

## Movement & Defence Spells

| Spell       | Cast | Effect                        | Align   |
| ----------- | ---- | ----------------------------- | ------- |
| Magic Wings | 40%  | Grants flying movement (Mv:6) | Neutral |
| Shadow Form | 60%  | Defence +2, Movement +2       | Neutral |

**Magic Wings** lets your wizard fly. Flying units can move over occupied
squares and have 6 movement points — a massive upgrade from the wizard's
default of 1. This pairs extremely well with a Magic Sword for a
hit-and-run wizard.

**Shadow Form** is a nice all-round buff: harder to kill and faster on
foot. The bonuses stack with equipment spells.

---

## Terrain Spells

These spells place special terrain on the board. Unlike creature spells,
many of them can be placed at considerable range — not just adjacent to
your wizard.

### Shelters

| Spell        | Cast | Range | Align |
| ------------ | ---- | ----- | ----- |
| Magic Castle | 40%  | 8     | Law   |
| Dark Citadel | 40%  | 8     | Chaos |

Castles and Citadels provide shelter — a wizard inside one is harder to
attack. They're equivalent except for alignment. Place one near your
wizard for protection, or at range to create a future safe position.

### Forests

| Spell       | Cast | Range | Align |
| ----------- | ---- | ----- | ----- |
| Magic Wood  | 70%  | 8     | Law   |
| Shadow Wood | 30%  | 8     | Chaos |

**Magic Wood** is one of the most important spells in the game. It creates
a tree on the board. If your wizard stands in a Magic Wood at the start
of a turn, they receive a **new random spell** — potentially anything in
the game, including the otherwise unobtainable Turmoil. Trees also provide
some defensive cover. You can place trees up to 8 squares away, so plan
ahead.

**Shadow Wood** works similarly but is Chaotic and much harder to cast at
30%. It also has a low combat stat, meaning enemy creatures can attack
and destroy it.

### Walls

| Spell | Cast | Range | Align   |
| ----- | ---- | ----- | ------- |
| Wall  | 70%  | 6     | Neutral |

Walls block movement. Place them to create chokepoints, protect your wizard,
or cut off enemy creatures. Simple but tactically very useful.

### Spreading Terrain

| Spell      | Cast | Range | Align |
| ---------- | ---- | ----- | ----- |
| Magic Fire | 70%  | 6     | Chaos |
| Gooey Blob | 80%  | 6     | Chaos |

These two are unique: they **spread** into adjacent squares each turn,
potentially engulfing creatures and wizards. They're chaotic and
unpredictable — they can spread into your own forces as easily as the
enemy's.

**Magic Fire** burns anything it touches. It can destroy creatures and is
very dangerous in a crowded area.

**Gooey Blob** engulfs and traps creatures. Anything caught in the blob
is stuck and can't move. The blob has a mind of its own — once cast, you
can't control where it spreads.

Both can be cast at range 6, so you can drop them near enemy forces from
a safe distance. Just make sure you're not downwind.

---

## Targeted Utility Spells

### Subversion

**Casting Chance:** 90% · **Alignment:** Neutral · **Range:** 7

Target an enemy creature within 7 squares. If it succeeds, that creature
switches sides and now belongs to you. The roll is against the creature's
Magic Resistance — high-MR creatures like Unicorn (MR:9) are nearly
impossible to subvert, while low-MR creatures like Crocodile (MR:2) are
easy pickings.

Subversion is incredibly powerful when it works. Stealing an enemy's best
creature is a massive swing. At 90% casting chance, it's reliable to cast
— but the MR check means results vary.

### Raise Dead

**Casting Chance:** 40% · **Alignment:** Chaos · **Range:** 4

Target a dead body (corpse) within 4 squares. If it succeeds, the corpse
rises as an undead creature under your control. The risen creature has
basic stats — it won't be as strong as the original creature, but it's a
free unit.

Bodies are left behind when real creatures are killed (illusions leave
nothing). Look for corpses on the battlefield and raise them for extra forces.

---

## Alignment Spells

These spells don't attack or summon anything — they shift the world
alignment, making future aligned spells easier or harder to cast.

| Spell   | Cast | Alignment Shift              |
| ------- | ---- | ---------------------------- |
| Law-1   | 70%  | World shifts +2 toward Law   |
| Law-2   | 50%  | World shifts +4 toward Law   |
| Chaos-1 | 70%  | World shifts -2 toward Chaos |
| Chaos-2 | 50%  | World shifts -4 toward Chaos |

These are strategic spells. If you have several powerful Lawful spells in
your hand (say Magic Sword at 30%), casting Law-1 or Law-2 first can boost
the world alignment enough to give those spells a much better chance of
succeeding.

Chaos-2 with its massive -4 shift can set up Dark Power, Vengeance, or
Chaotic creature spells for much better odds.

---

## Turmoil

**Casting Chance:** 90% · **Alignment:** Chaos · **Range:** 10

The most dramatic spell in the game — and the hardest to obtain. Turmoil
is **never dealt** in your starting hand. The only way to get it is by
standing in a Magic Wood and receiving it as your random new spell.

When cast, Turmoil randomly scatters every wizard and every creature on
the board to new positions. It's pure chaos — formations are destroyed,
engagements are broken, and the entire battlefield is reshuffled. This can
save you when you're cornered, or ruin an opponent who had a dominant
board position.

Use it as a desperation move when losing, or to disrupt an opponent who's
built up an overwhelming army around their wizard.

---

## Tips for New Players

**Start with creature spells.** Summoning creatures is the core of the
game. Look at the casting chance — early on, go for reliable creatures
(80%+) rather than gambling on a 30% Giant.

**Use illusions wisely.** An illusion Golden Dragon is terrifying for one
turn, but experienced players will Disbelieve it quickly. Illusions work
best when your opponent has already used their Disbelieve for the turn,
or when they have too many targets to check.

**Don't forget Disbelieve.** It costs nothing and you always have it. If
an opponent summons something powerful early, it's probably an illusion.

**Get a mount.** Horse (80%), Pegasus (50%), or Unicorn (50%) transform
your wizard from a slow, vulnerable target into a mobile threat. Mount
up early if you can.

**Mind the alignment.** If you have mostly Chaotic spells, casting a
Chaos-1 or Chaos-2 early can make your entire hand more reliable. If your
spells are mixed, stay neutral.

**Magic Wood is top-tier.** A 70% spell that gives you a new spell every
turn? Place trees near your wizard and keep growing your options.

**Protect your wizard.** If your wizard dies, everything you've summoned
vanishes. Stay behind your creatures, get equipment spells, and avoid
unnecessary risks.
