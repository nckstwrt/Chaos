using Chaos.Engine;
using Chaos.Enums;
using Chaos.Models;
using Chaos.UI;
using System;
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

void ChooseSpellForAI(Chaos.Models.Wizard wizard, GameState game)
{
    // Simple AI: pick the strongest unused creature spell, or a random attack spell
    var available = wizard.Spells
        .Where(s => !s.IsUsed || s.Name == "Disbelieve")
        .Where(s => s.Name != "Disbelieve") // Don't choose Disbelieve randomly
        .ToList();

    if (available.Count == 0)
    {
        wizard.SelectedSpell = null;
        return;
    }

    // Prefer creature spells with good stats
    var creatures = available
        .Where(s => s.Category == SpellCategory.Creature)
        .OrderByDescending(s => s.CreatureData!.Combat + s.CreatureData.Defence)
        .ToList();

    if (creatures.Count > 0)
    {
        wizard.SelectedSpell = creatures[0];
        // Cast as illusion if the real chance is low
        int chance = wizard.SelectedSpell.GetEffectiveCastingChance(game.WorldAlignment);
        wizard.CastingAsIllusion = chance < 40;
    }
    else
    {
        // Pick a random non-creature spell
        wizard.SelectedSpell = available[game.Rng.Next(available.Count)];
    }
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

    // Terrain spells: find an empty square within range
    if (spell.Category is SpellCategory.MagicTree or SpellCategory.MagicFire
                       or SpellCategory.MagicBlob)
    {
        // Place near wizard, within spell range
        foreach (var (nx, ny) in game.Board.GetAdjacentCells(wizard.X, wizard.Y))
        {
            if (game.Board.IsEmpty(nx, ny))
                return caster.CastSpell(wizard, nx, ny);
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
