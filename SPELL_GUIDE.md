# Chaos: The Battle of Wizards — Spell Guide

Welcome to Chaos! This guide explains every spell in the game, what it
does, and how to use it effectively. All stats and mechanics have been
verified against the original 1985 ZX Spectrum binary.

Each wizard starts with a random hand of about 14 spells plus Disbelieve,
which is always available. Some spells (Lightning, Magic Bolt, Subversion,
Raise Dead) appear twice in the internal spell table, making them more
likely to show up in your hand.

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

Most spells can only be cast once. The exceptions are Disbelieve (always
available every turn) and spells gained from Magic Wood.

---

## How Combat Works

Before diving into spells, it helps to understand how combat is resolved.
The game uses three different systems depending on the attack type, each
checking a **different** defending stat:

### Melee Combat

When a creature or wizard attacks an adjacent enemy:

    random(0–9) + attacker's Combat  vs  random(0–9) + defender's Defence

If the attack total is higher, the defender dies in one hit. There is no
health system — everything dies in a single successful attack.

### Ranged Creature Attacks

When a creature with Ranged Combat (Elf, Centaur, dragons) shoots:

    random(0–9) + attacker's Ranged Combat  vs  defender's Magic Resistance

Note: ranged attacks check **Magic Resistance**, not Defence. This means a
creature with high Defence but low MR (like Crocodile: D=6, MR=2) is easy
to shoot but hard to hit in melee.

### Attack Spells

When a wizard casts Lightning, Magic Bolt, Vengeance, etc:

    The spell's attack power  vs  defender's Manoeuvre

Attack spells check **Manoeuvre** — the target's agility/evasion — not Magic
Resistance. A creature like Bat (Ma=4) is fairly easy to hit with Lightning,
despite having the highest Magic Resistance in the game (MR=9). Conversely,
Faun (Ma=8) is very hard to hit with spells despite middling MR.

### Line of Sight

Ranged creature attacks and attack spells require a clear line of sight to
the target. Creatures, wizards, walls, trees, and castles all block the
line. You can't shoot through obstacles. Fire and Gooey Blob do not block
line of sight.

---

## Creature Stats Explained

Each creature has these stats:

- **Combat (C):** Melee attack strength. Higher is better.
- **Ranged Combat (RC):** Ranged attack strength. 0 means no ranged attack.
- **Range (R):** Maximum distance for ranged attacks.
- **Defence (D):** Resistance to melee attacks.
- **Movement (Mv):** Squares it can move per turn.
- **Magic Resistance (MR):** Resistance to ranged creature attacks (arrows,
  dragon breath). Despite the name, this does NOT defend against attack spells.
- **Manoeuvre (Ma):** Agility. Defends against attack spells, and also
  determines how easily the creature can be subverted (higher = easier to
  steal). Modifies melee combat when both attacker and defender are engaged
  with other enemies.

---

## Disbelieve

**Casting Chance:** 90% · **Alignment:** Neutral · **Range:** Unlimited

The most important spell in the game, and the only one you never lose.
Disbelieve targets any creature on the board. If that creature is an
**illusion**, it vanishes instantly. If it's real, nothing happens — but
now you know.

Note that Disbelieve is 90%, not 100% — it can actually fail, though a
Lawful world alignment can push it above 100%. It's the counterplay to
the illusion system. Any creature spell can be cast as a guaranteed
illusion (see below), so Disbelieve keeps the bluffing game honest.

---

## Creature Spells

Creature spells summon a monster onto any empty square next to your wizard.
The creature fights for you — you control its movement and attacks on
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

### Mounts

Some creatures can be **ridden** by your wizard. Move your wizard onto the
mount to ride it. While mounted, you use the mount's movement and flying
ability, giving your wizard much greater mobility.

### The Undead Rule

Undead creatures have a unique and powerful property: **they cannot be
attacked in melee by non-undead creatures or wizards.** If a Lion walks
up to a Zombie and tries to bite it, the game says "UNDEAD — CANNOT BE
ATTACKED" and the attack simply doesn't happen. The attacker wastes its turn.

This means a humble Zombie with Combat 1 and Defence 1 is completely
invulnerable to a Golden Dragon in melee. The only ways to kill an
undead creature are:

- **Another undead creature** — undead can fight each other normally
- **Ranged attacks** — creatures with Ranged Combat (Elf, Centaur, dragons)
  can shoot undead from a distance
- **Attack spells** — Lightning, Magic Bolt, Vengeance, etc. all work
- **Disbelieve** — if it's an illusion, Disbelieve destroys it as normal
- **Subversion** — take control of it instead of killing it

This makes undead creatures far more valuable than their raw stats suggest.
A cheap Skeleton (60% cast chance) can block a corridor indefinitely
against non-undead attackers. And since wizards aren't undead, an undead
creature parked next to an enemy wizard is a serious threat — the wizard
can't fight back in melee and must use spells or flee.

### Creature List

Creatures are grouped roughly by power tier, from easiest to hardest
to summon.

#### Common Creatures

| Creature   |  C | RC |  R |  D | Mv | MR | Ma | Cast | Align | Notes |
|------------|----|----|----|----|----|----|-----|------|-------|-------|
| Giant Rat  |  1 |  0 |  0 |  1 |  3 |  8 |  2 |  90% | Neutral | Fast, weak, but nearly immune to ranged attacks |
| Orc        |  2 |  0 |  0 |  1 |  1 |  4 |  4 |  90% | Chaos | Cheap foot soldier |
| Goblin     |  2 |  0 |  0 |  4 |  1 |  4 |  4 |  80% | Chaos | Better defence than Orc |
| Zombie     |  1 |  0 |  0 |  1 |  1 |  2 |  3 |  80% | Chaos | **Undead.** Stats look terrible but immune to non-undead melee |
| King Cobra |  4 |  0 |  0 |  1 |  1 |  6 |  1 |  80% | Law | Good attack, fragile, hard to subvert (Ma=1) |
| Dire Wolf  |  3 |  0 |  0 |  2 |  3 |  7 |  2 |  80% | Chaos | Good mobility for a cheap creature |
| Horse      |  1 |  0 |  0 |  3 |  4 |  8 |  1 |  80% | Law | **Mount.** Weak fighter but lets your wizard move fast |

#### Mid-Tier Creatures

| Creature   |  C | RC |  R |  D | Mv | MR | Ma | Cast | Align | Notes |
|------------|----|----|----|----|----|----|-----|------|-------|-------|
| Bat        |  1 |  0 |  0 |  1 |  5 |  9 |  4 |  70% | Chaos | **Flying.** Fastest scout, highest MR in the game |
| Faun       |  3 |  0 |  0 |  2 |  1 |  7 |  8 |  70% | Chaos | Highest manoeuvre — dodges spells but very easy to subvert |
| Crocodile  |  5 |  0 |  0 |  6 |  1 |  2 |  2 |  70% | Neutral | Strong and tough, but very vulnerable to ranged/spells |
| Elf        |  1 |  2 |  6 |  2 |  1 |  5 |  7 |  60% | Law ×2 | **Ranged.** Weak melee but fires arrows at range 6 |
| Centaur    |  1 |  2 |  4 |  3 |  4 |  5 |  5 |  60% | Law | **Ranged.** Mobile archer — good all-rounder |
| Gorilla    |  6 |  0 |  0 |  5 |  1 |  4 |  2 |  60% | Neutral | Strong attacker, decent defence, hard to subvert |
| Ogre       |  4 |  0 |  0 |  7 |  1 |  3 |  6 |  60% | Chaos | Very tough (D=7) — a solid defensive creature |
| Skeleton   |  3 |  0 |  0 |  2 |  1 |  3 |  4 |  60% | Chaos | **Undead.** Reliable to summon, immune to non-undead melee |
| Eagle      |  3 |  0 |  0 |  3 |  6 |  8 |  2 |  60% | Law | **Flying.** Fastest creature in the game (Mv=6) |

#### Powerful Creatures

| Creature   |  C | RC |  R |  D | Mv | MR | Ma | Cast | Align | Notes |
|------------|----|----|----|----|----|----|-----|------|-------|-------|
| Bear       |  6 |  0 |  0 |  7 |  2 |  6 |  2 |  50% | Law | Powerful and very tough, very hard to subvert (Ma=2) |
| Lion       |  6 |  0 |  0 |  4 |  4 |  8 |  3 |  50% | Law | Strong, fast, and nearly immune to ranged attacks |
| Unicorn    |  5 |  0 |  0 |  4 |  4 |  9 |  7 |  50% | Law ×2 | **Mount.** Highest MR of any mount |
| Pegasus    |  2 |  0 |  0 |  4 |  5 |  6 |  7 |  50% | Law ×2 | **Mount, Flying.** Great mobility, decent stats |
| Gryphon    |  3 |  0 |  0 |  5 |  5 |  5 |  6 |  50% | Law | **Mount, Flying.** Tougher than Pegasus |
| Harpy      |  4 |  0 |  0 |  2 |  5 |  8 |  5 |  50% | Chaos | **Flying.** Fast with strong magic resistance |
| Spectre    |  4 |  0 |  0 |  2 |  1 |  6 |  4 |  50% | Chaos | **Flying, Undead.** Immune to non-undead melee and flies |
| Wraith     |  5 |  0 |  0 |  5 |  2 |  4 |  5 |  40% | Chaos | **Undead.** Well-rounded ground fighter |
| Ghost      |  1 |  0 |  0 |  3 |  2 |  9 |  6 |  40% | Chaos | **Flying, Undead.** Highest MR, immune to non-undead melee |
| Hydra      |  7 |  0 |  0 |  8 |  1 |  4 |  6 |  40% | Chaos | Devastating attack and defence, but slow |

#### Elite Creatures

| Creature      |  C | RC |  R |  D | Mv | MR | Ma | Cast | Align | Notes |
|---------------|----|----|----|----|----|----|-----|------|-------|-------|
| Giant         |  9 |  0 |  0 |  7 |  2 |  6 |  5 |  30% | Law | Highest melee attack in the game |
| Manticore     |  3 |  1 |  3 |  6 |  5 |  6 |  8 |  30% | Chaos | **Mount, Flying, Ranged.** Does everything |
| Vampire       |  6 |  0 |  0 |  8 |  4 |  6 |  5 |  10% | Chaos ×2 | **Flying, Undead.** Immune to non-undead melee and very tough |
| Green Dragon  |  5 |  4 |  6 |  8 |  3 |  4 |  4 |   0% | Chaos | **Mount, Flying, Ranged.** Fire breath at range 6 |
| Red Dragon    |  7 |  3 |  5 |  9 |  3 |  4 |  5 |   0% | Chaos | **Mount, Flying, Ranged.** Strongest dragon fighter |
| Golden Dragon |  9 |  5 |  4 |  9 |  3 |  5 |  5 |   0% | Law ×2 | **Mount, Flying, Ranged.** The ultimate creature |

The three dragons have a 0% base casting chance — in a neutral world, they
have roughly a 10% chance after the minimum clamp. You'll need the world
strongly shifted toward their alignment (Chaotic for Green/Red, Lawful for
Golden) to have a realistic shot. Or just cast them as illusions and hope
nobody Disbelieves.

---

## Attack Spells

These spells let your wizard blast a target from range. They require line
of sight — you can't shoot through creatures, walls, or trees. The spell
rolls its attack power against the target's **Manoeuvre** (not Magic
Resistance):

    spell attack power  vs  target's Manoeuvre

| Spell      | Cast | Range | Power | Align   | Best used against |
|------------|------|-------|-------|---------|-------------------|
| Lightning  |  90% |   4   |   3   | Neutral | Nearby low-Manoeuvre creatures |
| Magic Bolt |  90% |   6   |   2   | Neutral | Slightly weaker but longer range |
| Vengeance  |  70% |  15   |   4   | Chaos   | Anywhere on the board, strong |
| Decree     |  70% |  15   |   3   | Law     | Lawful counterpart to Vengeance |
| Dark Power |  40% |  15   |   5   | Chaos ×2 | The most powerful attack, but hard to cast |
| Justice    |  40% |  15   |   5   | Law ×2  | Lawful counterpart to Dark Power |

Lightning and Magic Bolt appear twice in the spell table, so they're more
likely to show up in your hand. Both are reliable at 90% and always neutral.

Because attack spells check Manoeuvre, not Magic Resistance, the best
spell targets are creatures with low Ma: King Cobra (Ma=1), Horse (Ma=1),
Bear (Ma=2), Crocodile (Ma=2), Giant Rat (Ma=2), Eagle (Ma=2), Gorilla
(Ma=2). Conversely, Faun (Ma=8), Manticore (Ma=8), and Unicorn (Ma=7)
are very hard to hit with spells.

---

## Equipment Spells

These spells permanently boost your wizard's stats. They cast automatically
on yourself — no target needed. All are Lawful.

| Spell        | Cast | Effect | Align |
|--------------|------|--------|-------|
| Magic Knife  |  60% | Combat +2 | Law |
| Magic Sword  |  30% | Combat +4 | Law |
| Magic Shield |  60% | Defence +2 | Law |
| Magic Armour |  30% | Defence +4 | Law |
| Magic Bow    |  40% | Grants ranged attack | Law |

Magic Sword and Magic Armour are powerful but only have a 30% base chance.
If the world is Lawful, these become much more castable. Magic Knife and
Shield are weaker but more reliable at 60%.

Magic Bow gives your wizard a ranged attack, letting them shoot at enemies
from a distance. Very useful for staying alive while your creatures handle
melee.

---

## Movement & Defence Spells

| Spell       | Cast | Effect | Align |
|-------------|------|--------|-------|
| Magic Wings |  40% | Grants flying movement (Mv=6) | Neutral |
| Shadow Form |  60% | Defence +2, Movement +2 | Neutral |

**Magic Wings** lets your wizard fly. Flying units can move over occupied
squares and have 6 movement points — a massive upgrade from the wizard's
default of 1.

**Shadow Form** is a nice all-round buff: harder to kill and faster on
foot.

---

## Terrain Spells

These spells place special terrain on the board. Many of them can be
placed at considerable range — not just next to your wizard.

### Shelters

| Spell        | Cast | Range | Align  |
|--------------|------|-------|--------|
| Magic Castle |  40% |   8   | Law    |
| Dark Citadel |  40% |   8   | Chaos  |

Castles and Citadels provide shelter. A wizard or creature inside one
receives **+3 Defence** in combat, making them significantly harder to
kill in melee. This bonus is verified from the binary — the combat code
at 0xB232 checks bit 3 of the cell's flags and adds 3 to the defence
modifier when set.

Place one near your wizard for protection, or at range to create a future
defensive position. A wizard with Magic Armour (+4) standing in a Castle
(+3) has an effective +7 Defence bonus, making them very hard to touch.

### Forests

| Spell       | Cast | Range | Align  |
|-------------|------|-------|--------|
| Magic Wood  |  70% |   8   | Law    |
| Shadow Wood |  30% |   8   | Chaos  |

**Magic Wood** is one of the most important spells in the game. It creates
a tree on the board. If your wizard stands in a Magic Wood at the start
of a turn, they receive a **new random spell** — potentially anything in
the game, including the otherwise unobtainable Turmoil. Trees also block
line of sight for ranged attacks, providing cover. You can place trees up
to 8 squares away, so plan ahead.

**Shadow Wood** works the same way but is Chaotic and much harder to cast
at 30%.

### Walls

| Spell | Cast | Range | Align   |
|-------|------|-------|---------|
| Wall  |  70% |   6   | Neutral |

Walls block movement and line of sight. Place them to create chokepoints,
protect your wizard, or cut off enemy ranged attackers.

### Spreading Terrain

| Spell      | Cast | Range | Align |
|------------|------|-------|-------|
| Magic Fire |  70% |   6   | Chaos |
| Gooey Blob |  80% |   6   | Chaos |

These two are unique: they **spread** into adjacent squares each turn.
At the end of every round, each blob or fire cell has roughly a 30–40%
chance of spreading into one random adjacent empty cell. If the spread
reaches a creature or wizard, it is engulfed and destroyed. Spreading
does not cross walls, castles, or citadels, but it will burn through trees.

**Magic Fire** burns anything it touches, destroying creatures and wizards
alike.

**Gooey Blob** engulfs and traps anything in its path.

Both can be cast at range 6, so you can drop them near enemy forces from
a safe distance. But be warned — once cast, you cannot control where they
spread, and they will happily engulf your own creatures too. The chaos is
the point.

---

## Targeted Utility Spells

### Subversion

**Casting Chance:** 90% · **Alignment:** Neutral · **Range:** 7

Target an enemy creature within 7 squares. First the spell must succeed
its 90% casting roll. Then it makes a second check against the target
creature's **Manoeuvre**:

    random(0–9)  vs  target's Manoeuvre + 1

If the random roll is **below** the threshold, the creature switches sides
and now belongs to you. This means — counterintuitively — that creatures
with **higher Manoeuvre are easier to subvert**. Nimble, independent-minded
creatures can be turned more easily, while stubborn, plodding creatures
resist.

| Creature   | Ma | Subversion chance | Overall (with 90% cast) |
|------------|---:|------------------:|------------------------:|
| King Cobra |  1 |              20%  |                    18%  |
| Horse      |  1 |              20%  |                    18%  |
| Bear       |  2 |              30%  |                    27%  |
| Crocodile  |  2 |              30%  |                    27%  |
| Giant      |  5 |              60%  |                    54%  |
| Hydra      |  6 |              70%  |                    63%  |
| Unicorn    |  7 |              80%  |                    72%  |
| Faun       |  8 |              90%  |                    81%  |
| Manticore  |  8 |              90%  |                    81%  |

Subversion appears twice in the spell table, so you're more likely to
receive it in your hand. It's one of the strongest spells — stealing
an enemy's best creature is a massive swing.

### Raise Dead

**Casting Chance:** 40% · **Alignment:** Chaos · **Range:** 4

Target a dead body (corpse) within 4 squares. If it succeeds, the corpse
rises as an undead creature under your control. The risen creature has
basic stats, but it's a free unit — and being undead, it's immune to
non-undead melee attacks.

Bodies are left behind when real creatures are killed (illusions leave
nothing). Raise Dead appears twice in the spell table, making it more
common in your hand.

---

## Alignment Spells

These spells don't attack or summon anything — they shift the world
alignment, making future aligned spells easier or harder to cast.

| Spell   | Cast | Alignment Shift |
|---------|------|-----------------|
| Law-1   |  70% | World shifts +2 toward Law |
| Law-2   |  50% | World shifts +4 toward Law |
| Chaos-1 |  70% | World shifts -2 toward Chaos |
| Chaos-2 |  50% | World shifts -4 toward Chaos |

These are strategic spells. If you have several powerful Lawful spells in
your hand (say Magic Sword at 30% or Golden Dragon at 0%), casting Law-2
first can shift the world alignment by +4, boosting those spells by +40
percentage points.

Chaos-2 with its massive -4 shift can set up Dark Power, Vengeance, or
Chaotic creature spells for much better odds.

---

## Turmoil

**Casting Chance:** 90% · **Alignment:** Chaos

The most dramatic spell in the game — and the hardest to obtain. Turmoil
is **never dealt** in your starting hand. The only way to get it is by
standing in a Magic Wood and receiving it as your random new spell.

When cast, Turmoil randomly scatters every wizard and every creature on
the board to new positions. Formations are destroyed, engagements are
broken, and the entire battlefield is reshuffled.

Use it as a desperation move when losing, or to disrupt an opponent who's
built up an overwhelming army around their wizard.

---

## Tips for New Players

**Start with creature spells.** Summoning creatures is the core of the
game. Early on, go for reliable creatures (80%+) rather than gambling on
a 30% Giant.

**Use illusions wisely.** An illusion Golden Dragon is terrifying for one
turn, but experienced players will Disbelieve it quickly. Illusions work
best when your opponent has already used Disbelieve, or when they have too
many targets to check them all.

**Don't forget Disbelieve.** It costs nothing and you always have it. If
an opponent summons something powerful suspiciously early, it's probably
an illusion.

**Get a mount.** Horse (80%), Pegasus (50%), or Unicorn (50%) transform
your wizard from a slow, vulnerable target into a mobile threat.

**Undead are stronger than they look.** A Zombie has terrible stats, but
it can't be killed in melee by anything that isn't undead. Use undead
creatures as blockers — park a Skeleton in a chokepoint and non-undead
enemies simply can't get past.

**Mind the stats.** Each defence stat protects against a different attack
type: Defence blocks melee, Magic Resistance blocks ranged creature
attacks, and Manoeuvre blocks attack spells. A creature that looks tough
in one area may be vulnerable in another. Crocodile has excellent Defence
(6) but terrible MR (2) and Ma (2) — great in melee, easy to snipe.

**Shelter in a castle.** Magic Castle and Dark Citadel give +3 Defence.
That's as good as a Magic Shield and Magic Knife combined. Build one
near your wizard.

**Magic Wood is top-tier.** A 70% spell that gives you a new spell every
turn you stand on it? Place trees near your wizard and keep growing your
options. It's also the only way to get Turmoil.

**Mind the alignment.** If you have mostly Chaotic spells, casting a
Chaos-1 or Chaos-2 early can make your entire hand more reliable.

**Subversion targets.** High-manoeuvre creatures like Manticore (Ma=8) and
Faun (Ma=8) are very easy to subvert at 90% after casting. Low-manoeuvre
creatures like Bear (Ma=2) and Horse (Ma=1) are much harder to steal.
Choose your Subversion targets accordingly.

**Watch for spreading terrain.** Gooey Blob and Magic Fire grow each
round. Don't stand near them, and don't place them near your own forces.
They're best used offensively at range 6 against enemy clusters.

**Protect your wizard.** If your wizard dies, everything you've summoned
vanishes. Stay behind your creatures, get equipment spells, build a castle,
and avoid unnecessary risks.
