# Chaos: The Battle of Wizards — C# Port

A hopefully faithful port of Julian Gollop's classic 1985 ZX Spectrum game using Claude.ai to do the heavy lifting.
All creature stats and spell data verified against the original Z80 binary.

## Building & Running

```bash
dotnet run --project Chaos
```

Requires .NET 8.0 SDK or higher.

## Project Structure

```
Chaos/
├── Enums/GameEnums.cs          # Game constants and flag types
├── Models/
│   ├── CreatureStats.cs        # Creature stat block (combat, defence, etc.)
│   ├── BoardCreature.cs        # A creature placed on the board with runtime state
│   ├── Spell.cs                # Spell definition with casting chance calculation
│   ├── Wizard.cs               # Player wizard with equipment, mount, and spells
│   └── GameBoard.cs            # 15×10 grid with cell contents
├── Data/SpellDatabase.cs       # All spells and creature stats (verified from Z80)
├── Engine/
│   ├── GameState.cs            # Central state: board, wizards, turn management
│   ├── CombatEngine.cs         # Melee, ranged, and magic attack resolution
│   ├── SpellCaster.cs          # All spell casting logic
│   └── MovementEngine.cs       # Movement, engagement, and pathfinding
├── UI/ConsoleRenderer.cs       # Text-mode board renderer
├── Program.cs                  # Game loop and player input
└── Z80_VERIFICATION_REPORT.md  # Full audit trail of all binary corrections
```

## Data Verification

Every creature stat and spell property was extracted directly from the
chaos.z80 binary snapshot and cross-referenced with Sean Irvine's 1996
disassembly (comp.sys.sinclair Usenet). See Z80_VERIFICATION_REPORT.md
for the complete audit trail, including:

- Creature data table at address 0xE463 (38 bytes per record)
- Spell properties table at address 0x7D60 (7 bytes per record)
- Board boundary checks at address 0xBE58 (confirms 15×10 arena)
- Wizard starting positions at address 0x8A56

Key corrections from the Z80 binary (vs secondary sources):

- **Board is 15×10**, not 10×10
- **MagicResistance and Manoeuvre were swapped** in every source I checked
- **All non-creature casting chances were inflated** (e.g. Lightning is 90%, not 100%)
- **Dragons have 0% base casting chance** (clamped to 10% at runtime)
- **Disbelieve is 90%**, not 100% — it can actually fail!
- **Terrain spells have real range** (6-8 squares), not adjacent-only
- **Gooey Blob and Magic Fire are Chaotic** (alignment -1), not neutral
- **Turmoil is excluded from dealing** — only obtainable via Magic Wood
- **LAW/CHAOS alignment spells** were missing from most remakes

## How the Original Works

The core game loop has two passes each round:

1. **Spell selection** — each wizard picks a spell (or passes)
2. **Cast & move** — each wizard casts, then moves themselves and all owned creatures

Combat is simple: `random(0-10) + attack` vs `random(0–10) + defence`. One hit kills.
The world alignment (Law ↔ Chaos) shifts as spells are cast, affecting future casting chances.
Any creature can be cast as an illusion (100% success) but is destroyed by Disbelieve.

## What's Implemented

- ✅ All 32 creature spells with binary-verified stats
- ✅ Attack spells (Lightning, Magic Bolt, Vengeance, etc.)
- ✅ Equipment spells (Sword, Shield, Armour, etc.)
- ✅ Disbelieve / illusion system (with correct 90% base chance)
- ✅ Terrain spells with correct ranges (Magic Fire, Gooey Blob, trees, castles)
- ✅ LAW-1/2 and CHAOS-1/2 alignment spells
- ✅ Subversion, Raise Dead, Turmoil
- ✅ World alignment system
- ✅ Combat resolution (melee, ranged, magic)
- ✅ Movement with engagement rules
- ✅ Mounting system
- ✅ Basic AI for computer wizards
- ✅ 15×10 board (correct dimensions)
- ✅ Console UI with coloured output

## Next Steps

- [x] Gooey Blob / Magic Fire spreading each turn
- [x] Line-of-sight for ranged attacks
- [ ] Magic Wood granting new spells (including Turmoil)
- [ ] Verify remaining 5 wizard starting positions from binary
- [ ] Smarter AI (target selection, retreat, spell choice)
- [ ] Graphical UI (MonoGame, Godot, or WPF)
- [ ] Original Spectrum character graphics as sprites
