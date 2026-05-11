using Chaos.Engine;
using Chaos.Enums;
using Chaos.Models;
using Chaos.UI;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Chaos: The Battle of Wizards — C# Port
/// Originally by Julian Gollop, 1985 (ZX Spectrum)
///
/// This is a faithful port of the core game mechanics. The original
/// Z80 code ran in ~48K of RAM on a Spectrum; the game logic is
/// surprisingly elegant and maps well to modern C#.
///
/// Current state: playable console prototype with human spell selection
/// and movement. AI and graphical UI are next steps.
///
/// To run:  dotnet run
/// </summary>

var game = new GameState();
var renderer = new ConsoleRenderer();
var combat = new CombatEngine(game);
var spellCaster = new SpellCaster(game, combat);
var movement = new MovementEngine(game, combat);
var spreading = new SpreadingEngine(game);
var magicWood = new MagicWoodEngine(game);
var shadowWood = new ShadowWoodEngine(game, combat);

// ── Game setup ──────────────────────────────────────────────────────

Console.Clear();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║   CHAOS: THE BATTLE OF WIZARDS           ║");
Console.WriteLine("║   Originally by Julian Gollop, 1985      ║");
Console.WriteLine("║   C# Port                                ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();

int numPlayers = ReadInt("How many wizards? (2-8): ", 2, 8);
int numHumans  = ReadInt($"How many human players? (0-{numPlayers}): ", 0, numPlayers);

game.SetupGame(numPlayers, numHumans);

Console.WriteLine("\nWizards have been placed and spells dealt.");
Console.WriteLine("Press any key to begin...");
Console.ReadKey(true);

// ── Main game loop ──────────────────────────────────────────────────

bool gameOver = false;

while (!gameOver)
{
    // ── Phase 1: All wizards choose their spells ────────────────

    game.BeginSpellSelectionPhase();

    for (int i = 0; i < game.Wizards.Count; i++)
    {
        var wizard = game.Wizards[i];
        if (!wizard.IsAlive) continue;

        renderer.DrawBoard(game);
        renderer.DrawWizardStatus(game);

        if (wizard.IsHuman)
        {
            ChooseSpellForHuman(wizard, game);
        }
        else
        {
            ChooseSpellForAI(wizard, game);
        }
    }

    // ── Phase 2: Each wizard casts their spell, then moves ──────

    game.BeginCastAndMovePhase();

    for (int i = 0; i < game.Wizards.Count; i++)
    {
        var wizard = game.Wizards[i];
        if (!wizard.IsAlive) continue;

        game.CurrentWizardIndex = i;

        // ADD: Magic Wood spell granting
        string? woodResult = magicWood.CheckMagicWood(wizard);
        if (woodResult != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(woodResult);
            Console.ForegroundColor = ConsoleColor.White;
        }

        // Cast spell
        if (wizard.SelectedSpell != null)
        {
            renderer.DrawBoard(game);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n── {wizard.Name}'s Casting Phase ──\n");
            Console.ForegroundColor = ConsoleColor.White;

            string castResult;
            if (wizard.IsHuman)
            {
                castResult = CastSpellForHuman(wizard, game, spellCaster);
            }
            else
            {
                castResult = CastSpellForAI(wizard, game, spellCaster);
            }

            Console.WriteLine(castResult);

            // Handle multi-placement spells (Wall ×4, Magic Wood ×8, etc.)
            while (spellCaster.RemainingPlacements > 0)
            {
                renderer.DrawBoard(game);
                Console.WriteLine($"{spellCaster.RemainingPlacements} placements remaining. Enter coordinates or 'done' to stop.");
                Console.Write("Target X: ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                if (input == "done" || input == "d" || string.IsNullOrEmpty(input))
                {
                    spellCaster.ClearPlacements();
                    break;
                }
                if (!int.TryParse(input, out int px) || px < 0 || px >= GameBoard.Width)
                {
                    Console.WriteLine("Invalid coordinate.");
                    continue;
                }
                Console.Write("Target Y: ");
                input = Console.ReadLine()?.Trim();
                if (!int.TryParse(input, out int py) || py < 0 || py >= GameBoard.Height)
                {
                    Console.WriteLine("Invalid coordinate.");
                    continue;
                }

                string? placeError = spellCaster.PlaceSingleTerrain(
                    wizard, wizard.SelectedSpell!, px, py, spellCaster.RemainingTerrain);
                if (placeError != null)
                {
                    Console.WriteLine(placeError);
                }
                else
                {
                    Console.WriteLine($"Placed at ({px},{py}).");
                    spellCaster.DecrementPlacements();
                }
            }

            Console.WriteLine("\nPress any key...");
            Console.ReadKey(true);
        }

        // Movement phase
        if (wizard.IsAlive)
        {
            renderer.DrawBoard(game);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n── {wizard.Name}'s Movement Phase ──\n");
            Console.ForegroundColor = ConsoleColor.White;

            if (wizard.IsHuman)
            {
                DoHumanMovement(wizard, game, movement, renderer);
            }
            else
            {
                DoAIMovement(wizard, game, movement);
            }
        }

        // Check for victory
        var winner = game.CheckForWinner();
        if (winner != null)
        {
            renderer.DrawBoard(game);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n{'═',50}");
            Console.WriteLine($"  {winner.Name} WINS! The battle is over!");
            Console.WriteLine($"{'═',50}\n");
            Console.ForegroundColor = ConsoleColor.White;
            gameOver = true;
            break;
        }
    }

    // ADD: Spread Gooey Blob and Magic Fire at end of round
    if (!gameOver)
    {
        var spreadMessages = spreading.SpreadAll();
        if (spreadMessages.Count > 0)
        {
            renderer.DrawBoard(game);
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var msg in spreadMessages)
                Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nPress any key...");
            Console.ReadKey(true);
        }

        // Shadow Wood attacks adjacent non-undead enemies
        var woodAttackMessages = shadowWood.AttackAll();
        if (woodAttackMessages.Count > 0)
        {
            renderer.DrawBoard(game);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            foreach (var msg in woodAttackMessages)
                Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nPress any key...");
            Console.ReadKey(true);
        }
    }
}

// ── Human interaction helpers ───────────────────────────────────────

void ChooseSpellForHuman(Chaos.Models.Wizard wizard, GameState game)
{
    renderer.DrawSpellList(wizard, game.WorldAlignment);

    var availableSpells = wizard.Spells
        .Where(s => !s.IsUsed || s.Name == "Disbelieve")
        .ToList();

    int choice = ReadInt($"\nChoose spell (0 to pass, 1-{availableSpells.Count}): ", 0, availableSpells.Count);

    if (choice == 0)
    {
        wizard.SelectedSpell = null;
        Console.WriteLine($"{wizard.Name} will pass this turn.");
    }
    else
    {
        wizard.SelectedSpell = availableSpells[choice - 1];
        Console.WriteLine($"{wizard.Name} prepares {wizard.SelectedSpell.Name}.");

        // Ask about illusion for creature spells
        if (wizard.SelectedSpell.Category == SpellCategory.Creature)
        {
            int chance = wizard.SelectedSpell.GetEffectiveCastingChance(game.WorldAlignment);
            Console.Write($"Cast as illusion? (100% vs {chance}% real) [y/n]: ");
            string? input = Console.ReadLine()?.Trim().ToLower();
            wizard.CastingAsIllusion = input == "y" || input == "yes";
        }
    }

    Console.ReadKey(true);
}

void ChooseSpellForAI(Wizard wizard, GameState game)
{
    var available = wizard.Spells
        .Where(s => !s.IsUsed || s.Name == "Disbelieve")
        .Where(s => s.Name != "Disbelieve")
        .ToList();

    if (available.Count == 0) { wizard.SelectedSpell = null; return; }

    // 1. If adjacent to enemy wizard with no creatures protecting us, 
    //    prioritise attack spells or defensive equipment
    var nearbyEnemyWizards = game.GetAliveWizards()
        .Where(w => w.Id != wizard.Id)
        .Where(w => GameBoard.Distance(wizard.X, wizard.Y, w.X, w.Y) <= 2)
        .ToList();

    // 2. If we have no creatures, summon one (prefer mounts for mobility)
    var ownedCreatures = game.GetCreaturesOwnedBy(wizard.Id);
    if (ownedCreatures.Count == 0)
    {
        var mounts = available
            .Where(s => s.Category == SpellCategory.Creature && s.CreatureData!.IsMount)
            .OrderByDescending(s => s.CastingChance)
            .FirstOrDefault();
        if (mounts != null)
        {
            wizard.SelectedSpell = mounts;
            wizard.CastingAsIllusion = mounts.GetEffectiveCastingChance(game.WorldAlignment) < 40;
            return;
        }
    }

    // 3. Disbelieve any suspiciously powerful creature nearby
    //    (creatures summoned on the same turn they appeared = likely illusion)
    // For now, simple heuristic: Disbelieve dragons and vampires
    var suspects = game.Creatures
        .Where(c => c.OwnerWizardId != wizard.Id)
        .Where(c => c.Stats.Combat >= 6 || c.Stats.IsFlying)
        .Where(c => GameBoard.Distance(wizard.X, wizard.Y, c.X, c.Y) <= 5)
        .OrderByDescending(c => c.Stats.Combat + c.Stats.Defence)
        .FirstOrDefault();
    if (suspects != null)
    {
        var disbelieve = available.FirstOrDefault(s => s.Name == "Disbelieve");
        if (disbelieve == null) disbelieve = wizard.Spells.First(s => s.Name == "Disbelieve");
        wizard.SelectedSpell = disbelieve;
        return;
    }

    // 4. Cast attack spells on enemy wizards if in range
    var attackSpells = available
        .Where(s => s.Category == SpellCategory.MagicAttack)
        .OrderByDescending(s => s.AttackPower)
        .ToList();
    foreach (var spell in attackSpells)
    {
        var target = game.GetAliveWizards()
            .Where(w => w.Id != wizard.Id)
            .Where(w => GameBoard.Distance(wizard.X, wizard.Y, w.X, w.Y) <= spell.Range)
            .Where(w => LineOfSight.HasClearPath(game.Board, wizard.X, wizard.Y, w.X, w.Y))
            .OrderBy(w => w.Manoeuvre)  // target low-manoeuvre wizards first
            .FirstOrDefault();
        if (target != null)
        {
            wizard.SelectedSpell = spell;
            return;
        }
    }

    // 5. Equipment spells if we have none yet
    if (!wizard.HasMagicSword && !wizard.HasMagicKnife)
    {
        var weapon = available.FirstOrDefault(s => s.Name is "Magic Sword" or "Magic Knife");
        if (weapon != null) { wizard.SelectedSpell = weapon; return; }
    }
    if (!wizard.HasMagicShield && !wizard.HasMagicArmour)
    {
        var armour = available.FirstOrDefault(s => s.Name is "Magic Armour" or "Magic Shield");
        if (armour != null) { wizard.SelectedSpell = armour; return; }
    }

    // 6. Alignment spells if they'd help our hand
    var alignSpells = available.Where(s => s.Category == SpellCategory.AlignmentSpell).ToList();
    if (alignSpells.Count > 0)
    {
        // Count how many of our remaining spells benefit from each direction
        int lawCount = available.Count(s => s.AlignmentShift > 0);
        int chaosCount = available.Count(s => s.AlignmentShift < 0);
        if (lawCount > chaosCount)
        {
            var lawSpell = alignSpells.FirstOrDefault(s => s.AlignmentShift > 0);
            if (lawSpell != null) { wizard.SelectedSpell = lawSpell; return; }
        }
        else if (chaosCount > 0)
        {
            var chaosSpell = alignSpells.FirstOrDefault(s => s.AlignmentShift < 0);
            if (chaosSpell != null) { wizard.SelectedSpell = chaosSpell; return; }
        }
    }

    // 7. Subversion on powerful enemy creatures (prefer high manoeuvre targets)
    var subversion = available.FirstOrDefault(s => s.Name == "Subversion");
    if (subversion != null)
    {
        var subTarget = game.Creatures
            .Where(c => c.OwnerWizardId != wizard.Id)
            .Where(c => GameBoard.Distance(wizard.X, wizard.Y, c.X, c.Y) <= 7)
            .Where(c => c.Stats.Manoeuvre >= 5)  // only subvert high-Ma (easy) targets
            .OrderByDescending(c => c.Stats.Combat + c.Stats.Defence)
            .FirstOrDefault();
        if (subTarget != null) { wizard.SelectedSpell = subversion; return; }
    }

    // 8. Default: summon strongest creature we can
    var creatures = available
        .Where(s => s.Category == SpellCategory.Creature)
        .OrderByDescending(s => s.CreatureData!.Combat + s.CreatureData.Defence)
        .ToList();
    if (creatures.Count > 0)
    {
        wizard.SelectedSpell = creatures[0];
        int chance = wizard.SelectedSpell.GetEffectiveCastingChance(game.WorldAlignment);
        wizard.CastingAsIllusion = chance < 30;  // illusion if very unlikely to cast real
        return;
    }

    // 9. Anything remaining
    wizard.SelectedSpell = available.FirstOrDefault();
}

/// <summary>
/// AI spell selection — matches the original Z80 logic at 0x93A0–0x9480.
///
/// The original AI is deliberately simple:
///   1. Iterate spells from last to first (strongest creatures last)
///   2. Pick the last creature spell with acceptable casting chance
///   3. If chance is low (< 40%), cast as illusion instead
///   4. If no creature spells, pick the first available non-creature spell
///   5. Never uses Disbelieve (the original AI doesn't Disbelieve!)
///
/// AI difficulty (1–8) only modifies the wizard's combat stats
/// via an "ability" value added to casting chances. It does NOT
/// change the decision logic.
/// </summary>
void ChooseSpellForAIOriginal(Chaos.Models.Wizard wizard, GameState game)
{
    var available = wizard.Spells
        .Where(s => !s.IsUsed && s.Name != "Disbelieve")
        .ToList();

    if (available.Count == 0)
    {
        wizard.SelectedSpell = null;
        return;
    }

    // Prefer creature spells — pick the last one in the list
    // (strongest creatures are dealt later in the hand).
    // The original iterates in display order and keeps overwriting
    // the selection, so the last valid spell wins.
    Spell? bestCreature = null;
    foreach (var spell in available)
    {
        if (spell.Category == SpellCategory.Creature)
            bestCreature = spell;
    }

    if (bestCreature != null)
    {
        wizard.SelectedSpell = bestCreature;
        int chance = bestCreature.GetEffectiveCastingChance(game.WorldAlignment);

        // Cast as illusion if real chance is poor.
        // The original checks the effective chance against a threshold.
        wizard.CastingAsIllusion = chance < 40;
        return;
    }

    // No creature spells — pick the first available non-creature spell.
    // The original doesn't have sophisticated prioritisation here;
    // it just takes whatever is next in the list.
    wizard.SelectedSpell = available.FirstOrDefault();
}

string CastSpellForHuman(Chaos.Models.Wizard wizard, GameState game, SpellCaster caster)
{
    var spell = wizard.SelectedSpell!;
    Console.WriteLine($"Casting {spell.Name}...");

    // Self-targeting spells: equipment, alignment, self-buffs
    bool isSelfTarget = spell.Category == SpellCategory.MagicDefence
                     || spell.Category == SpellCategory.AlignmentSpell
                     || spell.Name is "Magic Wings" or "Shadow Form" or "Turmoil";

    if (isSelfTarget)
    {
        return caster.CastSpell(wizard, wizard.X, wizard.Y);
    }

    // Everything else needs a target coordinate
    Console.Write("Target X: ");
    int tx = ReadInt("", 0, GameBoard.Width - 1);
    Console.Write("Target Y: ");
    int ty = ReadInt("", 0, GameBoard.Height - 1);
    return caster.CastSpell(wizard, tx, ty);
}
string CastSpellForAI(Chaos.Models.Wizard wizard, GameState game, SpellCaster caster)
{
    var spell = wizard.SelectedSpell!;

    // Creature spells: find an adjacent empty square
    if (spell.Category == SpellCategory.Creature)
    {
        foreach (var (nx, ny) in game.Board.GetAdjacentCells(wizard.X, wizard.Y))
        {
            if (game.Board.IsEmpty(nx, ny))
                return caster.CastSpell(wizard, nx, ny);
        }
        return $"{wizard.Name} has no room to summon!";
    }

    // Attack spells: find nearest enemy wizard
    if (spell.Category == SpellCategory.MagicAttack)
    {
        var target = game.GetAliveWizards()
            .Where(w => w.Id != wizard.Id)
            .OrderBy(w => GameBoard.Distance(wizard.X, wizard.Y, w.X, w.Y))
            .FirstOrDefault();

        if (target != null && GameBoard.Distance(wizard.X, wizard.Y, target.X, target.Y) <= spell.Range)
            return caster.CastSpell(wizard, target.X, target.Y);

        return $"{wizard.Name}'s {spell.Name} has no targets in range.";
    }

    // Terrain spells: place within range, then auto-place remaining
    if (spell.Category is SpellCategory.MagicTree or SpellCategory.MagicFire
                       or SpellCategory.MagicBlob
        || spell.Name is "Wall" or "Magic Castle" or "Dark Citadel")
    {
        foreach (var (nx, ny) in game.Board.GetAdjacentCells(wizard.X, wizard.Y))
        {
            if (game.Board.IsEmpty(nx, ny))
            {
                string result = spellCaster.CastSpell(wizard, nx, ny);

                // Auto-place remaining items near wizard
                while (spellCaster.RemainingPlacements > 0)
                {
                    bool placed = false;
                    for (int radius = 1; radius <= spell.Range && !placed; radius++)
                    {
                        for (int ax = wizard.X - radius; ax <= wizard.X + radius && !placed; ax++)
                            for (int ay = wizard.Y - radius; ay <= wizard.Y + radius && !placed; ay++)
                            {
                                if (!game.Board.InBounds(ax, ay)) continue;
                                if (GameBoard.Distance(wizard.X, wizard.Y, ax, ay) != radius) continue;
                                string? err = spellCaster.PlaceSingleTerrain(
                                    wizard, spell, ax, ay, spellCaster.RemainingTerrain);
                                if (err == null)
                                {
                                    spellCaster.DecrementPlacements();
                                    placed = true;
                                }
                            }
                    }
                    if (!placed) break; // no valid positions left
                }
                spellCaster.ClearPlacements();
                return result;
            }
        }
        return $"{wizard.Name} has no room to cast {spell.Name}!";
    }

    // Self-targeting / equipment / alignment spells
    return caster.CastSpell(wizard, wizard.X, wizard.Y);
}

void DoHumanMovement(Chaos.Models.Wizard wizard, GameState game, MovementEngine mover, ConsoleRenderer r)
{
    // Move the wizard
    int movesLeft = wizard.EffectiveMovement;
    Console.WriteLine($"{wizard.Name} has {movesLeft} movement points. (Enter coordinates or 'done')");

    while (movesLeft > 0)
    {
        r.DrawBoard(game);
        Console.Write($"Move to (x y) or 'done': ");
        string? input = Console.ReadLine()?.Trim().ToLower();
        if (input == "done" || input == "d" || string.IsNullOrEmpty(input)) break;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && int.TryParse(parts[0], out int mx) && int.TryParse(parts[1], out int my))
        {
            if (GameBoard.Distance(wizard.X, wizard.Y, mx, my) == 1)
            {
                string result = mover.MoveWizard(wizard, mx, my);
                Console.WriteLine(result);
                movesLeft--;
            }
            else
            {
                Console.WriteLine("Can only move one step at a time.");
            }
        }
    }

    // Move owned creatures
    var creatures = game.GetCreaturesOwnedBy(wizard.Id);
    foreach (var creature in creatures)
    {
        if (creature.HasMoved) continue;

        r.DrawBoard(game);
        int cMovesLeft = creature.Stats.Movement;
        Console.WriteLine($"\n{creature.Stats.Name} at ({creature.X},{creature.Y}) — {cMovesLeft} moves. ('done' to skip)");

        while (cMovesLeft > 0)
        {
            Console.Write($"Move {creature.Stats.Name} to (x y) or 'done': ");
            var input = Console.ReadLine()?.Trim().ToLower();
            if (input == "done" || input == "d" || string.IsNullOrEmpty(input)) break;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out int cx) && int.TryParse(parts[1], out int cy))
            {
                if (GameBoard.Distance(creature.X, creature.Y, cx, cy) == 1)
                {
                    string result = mover.MoveCreature(creature, cx, cy);
                    Console.WriteLine(result);
                    cMovesLeft--;
                    if (creature.HasAttacked) break; // Attack ends movement
                }
                else
                {
                    Console.WriteLine("One step at a time.");
                }
            }
        }
    }
}

void DoAIMovement(Chaos.Models.Wizard wizard, GameState game, MovementEngine mover)
{
    // Simple AI: move wizard toward nearest enemy, move creatures toward enemies
    var enemies = game.GetAliveWizards().Where(w => w.Id != wizard.Id).ToList();
    if (enemies.Count == 0) return;

    var nearest = enemies.OrderBy(e => GameBoard.Distance(wizard.X, wizard.Y, e.X, e.Y)).First();

    // Move wizard one step toward nearest enemy
    int bestDist = GameBoard.Distance(wizard.X, wizard.Y, nearest.X, nearest.Y);
    foreach (var (nx, ny) in game.Board.GetAdjacentCells(wizard.X, wizard.Y))
    {
        int dist = GameBoard.Distance(nx, ny, nearest.X, nearest.Y);
        if (dist < bestDist && (game.Board.IsEmpty(nx, ny) || game.Board[nx, ny].Wizard?.Id == nearest.Id))
        {
            string result = mover.MoveWizard(wizard, nx, ny);
            Console.WriteLine(result);
            break;
        }
    }

    // Move each creature toward nearest enemy
    foreach (var creature in game.GetCreaturesOwnedBy(wizard.Id))
    {
        if (creature.HasMoved) continue;
        int steps = creature.Stats.Movement;

        while (steps > 0 && !creature.HasAttacked)
        {
            int cBest = int.MaxValue;
            int bestX = creature.X, bestY = creature.Y;

            foreach (var (nx, ny) in game.Board.GetAdjacentCells(creature.X, creature.Y))
            {
                // Find nearest enemy unit
                foreach (var e in enemies)
                {
                    int d = GameBoard.Distance(nx, ny, e.X, e.Y);
                    if (d < cBest)
                    {
                        var cell = game.Board[nx, ny];
                        if (cell.IsPassable || (cell.Wizard?.Id != wizard.Id && cell.Wizard != null)
                            || (cell.Creature?.OwnerWizardId != wizard.Id && cell.Creature != null))
                        {
                            cBest = d;
                            bestX = nx;
                            bestY = ny;
                        }
                    }
                }
            }

            if (bestX == creature.X && bestY == creature.Y) break;

            string result = mover.MoveCreature(creature, bestX, bestY);
            Console.WriteLine(result);
            steps--;
        }
    }

    System.Threading.Thread.Sleep(500); // Brief pause so human can see AI moves
}

/// <summary>
/// AI movement — matches the original Z80 logic at 0xAC36–0xB100.
///
/// The original AI movement works as follows:
///   1. For each creature/wizard owned by this player:
///   2. Find the nearest ENEMY WIZARD (not creature!) by scanning
///      the entire board (routine at 0xB50E).
///   3. For each movement step, evaluate all 8 adjacent cells and
///      pick the one that minimizes Chebyshev distance to the
///      nearest enemy wizard (routine at 0xB3F0).
///   4. If the chosen cell contains an enemy → melee attack.
///   5. If the creature has ranged combat and an enemy is in range
///      with clear LOS → fire ranged attack (at 0xADE0).
///   6. If engaged (adjacent to an enemy), attack rather than move.
///
/// The AI does NOT target enemy creatures. It goes straight for
/// wizards. Kill the wizard = all their creatures vanish.
/// </summary>
void DoAIMovementOriginal(Chaos.Models.Wizard wizard, GameState game, MovementEngine mover)
{
    // Find all enemy wizards (the ONLY targets the AI cares about)
    var enemyWizards = game.GetAliveWizards()
        .Where(w => w.Id != wizard.Id)
        .ToList();

    if (enemyWizards.Count == 0) return;

    // Move the wizard itself
    MoveUnitTowardNearestWizard(wizard.X, wizard.Y, wizard.EffectiveMovement,
        wizard.CanFly, wizard.Id, enemyWizards, game, mover, isWizard: true, wizard: wizard);

    // Move each owned creature
    foreach (var creature in game.GetCreaturesOwnedBy(wizard.Id).ToList())
    {
        if (creature.HasMoved) continue;

        // Ranged attack first (if the creature has ranged combat)
        if (creature.Stats.RangedCombat > 0 && !creature.HasAttacked)
        {
            TryRangedAttack(creature, game);
        }

        MoveUnitTowardNearestWizard(creature.X, creature.Y, creature.Stats.Movement,
            creature.Stats.IsFlying, creature.OwnerWizardId, enemyWizards,
            game, mover, isWizard: false, creature: creature);
    }

    System.Threading.Thread.Sleep(500);
}

/// <summary>
/// Move a single unit step-by-step toward the nearest enemy wizard.
/// Each step picks the adjacent cell that minimizes Chebyshev distance
/// to the closest enemy wizard.
/// </summary>
void MoveUnitTowardNearestWizard(
    int startX, int startY, int movement, bool canFly, int ownerId,
    List<Chaos.Models.Wizard> enemyWizards, GameState game, MovementEngine mover,
    bool isWizard, Chaos.Models.Wizard? wizard = null, BoardCreature? creature = null)
{
    int curX = startX, curY = startY;
    int stepsLeft = movement;

    while (stepsLeft > 0)
    {
        // Find nearest enemy wizard from current position
        var nearest = enemyWizards
            .OrderBy(w => GameBoard.Distance(curX, curY, w.X, w.Y))
            .FirstOrDefault();
        if (nearest == null) break;

        int bestDist = GameBoard.Distance(curX, curY, nearest.X, nearest.Y);
        int bestX = curX, bestY = curY;
        bool bestIsAttack = false;

        // Check if engaged — if adjacent to an enemy, try to attack them
        // rather than move away (original behavior at 0xAF2A)
        bool engaged = game.IsAdjacentToEnemy(curX, curY, ownerId);
        if (engaged)
        {
            // Attack an adjacent enemy (prefer wizards over creatures)
            foreach (var (nx, ny) in game.Board.GetAdjacentCells(curX, curY))
            {
                var cell = game.Board[nx, ny];
                if (cell.Content == CellContent.Wizard && cell.Wizard?.Id != ownerId)
                {
                    bestX = nx; bestY = ny; bestIsAttack = true;
                    break;
                }
                if (cell.Content == CellContent.Creature && cell.Creature?.OwnerWizardId != ownerId)
                {
                    bestX = nx; bestY = ny; bestIsAttack = true;
                    // Don't break — keep looking for a wizard target
                }
            }

            if (bestIsAttack)
            {
                string result;
                if (isWizard)
                    result = mover.MoveWizard(wizard!, bestX, bestY);
                else
                    result = mover.MoveCreature(creature!, bestX, bestY);
                Console.WriteLine(result);
                break;
            }
            break; // Engaged but can't attack (e.g. non-undead vs undead) — stuck
        }

        // Evaluate all 8 adjacent cells — pick the one that minimizes
        // distance to nearest enemy wizard (Chebyshev, matching 0xB3F0)
        foreach (var (nx, ny) in game.Board.GetAdjacentCells(curX, curY))
        {
            var cell = game.Board[nx, ny];
            int dist = GameBoard.Distance(nx, ny, nearest.X, nearest.Y);

            // Can we enter this cell?
            bool canEnter = cell.IsPassable
                         || (cell.Content == CellContent.Wizard && cell.Wizard?.Id != ownerId)
                         || (cell.Content == CellContent.Creature && cell.Creature?.OwnerWizardId != ownerId);

            // Flying units can traverse occupied squares but must land on empty/enemy
            if (!canEnter && canFly && cell.Content != CellContent.Wall)
                canEnter = true;

            if (canEnter && dist < bestDist)
            {
                bestDist = dist;
                bestX = nx;
                bestY = ny;
                bestIsAttack = (cell.Content == CellContent.Wizard && cell.Wizard?.Id != ownerId)
                            || (cell.Content == CellContent.Creature && cell.Creature?.OwnerWizardId != ownerId);
            }
        }

        // No improvement possible — stop
        if (bestX == curX && bestY == curY) break;

        // Execute the move/attack
        string moveResult;
        if (isWizard)
            moveResult = mover.MoveWizard(wizard!, bestX, bestY);
        else
            moveResult = mover.MoveCreature(creature!, bestX, bestY);
        Console.WriteLine(moveResult);

        // Update position
        if (isWizard) { curX = wizard!.X; curY = wizard.Y; }
        else { curX = creature!.X; curY = creature!.Y; }

        stepsLeft--;

        // Attack ends movement
        if (bestIsAttack || (creature != null && creature.HasAttacked)) break;
    }
}

/// <summary>
/// Try a ranged attack on the nearest enemy in range with LOS.
/// The original at 0xADE0 checks for enemies within range and fires.
/// </summary>
void TryRangedAttack(BoardCreature creature, GameState game)
{
    if (creature.Stats.RangedCombat <= 0 || creature.HasAttacked) return;

    var targets = LineOfSight.GetVisibleTargets(
        game.Board, creature.X, creature.Y,
        creature.Stats.Range, creature.OwnerWizardId);

    if (targets.Count == 0) return;

    // Prefer wizard targets over creature targets (matching AI priority)
    var wizardTarget = targets
        .Where(t => game.Board[t.X, t.Y].Content == CellContent.Wizard)
        .OrderBy(t => GameBoard.Distance(creature.X, creature.Y, t.X, t.Y))
        .FirstOrDefault();

    var target = wizardTarget != default
        ? wizardTarget
        : targets.OrderBy(t => GameBoard.Distance(creature.X, creature.Y, t.X, t.Y)).First();

    string result = new CombatEngine(game).ExecuteAttack(
        creature.X, creature.Y, target.X, target.Y, isRanged: true);
    creature.HasAttacked = true;
    Console.WriteLine(result);
}
// ── Utility ─────────────────────────────────────────────────────────

static int ReadInt(string prompt, int min, int max)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine()?.Trim(), out int val) && val >= min && val <= max)
            return val;
        Console.WriteLine($"  Please enter a number between {min} and {max}.");
    }
}
