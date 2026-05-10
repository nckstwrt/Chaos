namespace Chaos.UI;

using Chaos.Enums;
using Chaos.Models;
using Chaos.Engine;
using System;
using System.Linq;

/// <summary>
/// Renders the game state to the console using coloured text characters.
/// This is a placeholder UI — the intent is that a graphical frontend
/// (WPF, MonoGame, Godot, etc.) could replace this without changing
/// any game logic.
///
/// The original Spectrum used its 8-colour palette with BRIGHT variants.
/// We approximate this with ConsoleColor.
/// </summary>
public class ConsoleRenderer
{
    // Map wizard colour index to console colours
    private static readonly ConsoleColor[] WizardColours =
    {
        ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Yellow,
        ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.White,
        ConsoleColor.Blue, ConsoleColor.DarkYellow
    };

    /// <summary>
    /// Draw the full game board to the console.
    /// </summary>
    public void DrawBoard(GameState game)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  Turn {game.TurnNumber}  |  World Alignment: {AlignmentString(game.WorldAlignment)}");
        Console.WriteLine();

        // Column headers
        Console.Write("   ");
        for (int x = 0; x < GameBoard.Width; x++)
            Console.Write($"{x,2} ");
        Console.WriteLine();

        // Draw each row
        for (int y = 0; y < GameBoard.Height; y++)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" {y} ");

            for (int x = 0; x < GameBoard.Width; x++)
            {
                DrawCell(game, x, y);
            }
            Console.WriteLine();
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
    }

    private void DrawCell(GameState game, int x, int y)
    {
        var cell = game.Board[x, y];
        switch (cell.Content)
        {
            case CellContent.Wizard:
                var w = cell.Wizard!;
                Console.ForegroundColor = WizardColours[w.Colour % WizardColours.Length];
                Console.Write(w.IsMounted ? "[W]" : " W ");
                break;

            case CellContent.Creature:
                var c = cell.Creature!;
                Console.ForegroundColor = WizardColours[c.OwnerWizardId % WizardColours.Length];
                string symbol = GetCreatureSymbol(c.Stats.Name);
                Console.Write(c.IsIllusion ? $"({symbol})" : $"[{symbol}]");
                break;

            case CellContent.DeadBody:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" x ");
                break;

            case CellContent.MagicTree:
            case CellContent.ShadowWood:
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(" T ");
                break;

            case CellContent.MagicFire:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(" f ");
                break;

            case CellContent.GooeyBlob:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(" g ");
                break;

            case CellContent.MagicCastle:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("[C]");
                break;

            case CellContent.DarkCitadel:
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write("[D]");
                break;

            case CellContent.Wall:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("###");
                break;

            default:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" · ");
                break;
        }
    }

    /// <summary>
    /// Get a 1-character symbol for a creature name.
    /// Uses the first letter, with overrides for duplicates.
    /// </summary>
    private static string GetCreatureSymbol(string name)
    {
        return name switch
        {
            "King Cobra"    => "K",
            "Dire Wolf"     => "w",
            "Goblin"        => "o",
            "Crocodile"     => "c",
            "Bat"           => "b",
            "Elf"           => "e",
            "Orc"           => "O",
            "Bear"          => "B",
            "Gorilla"       => "G",
            "Ogre"          => "g",
            "Hydra"         => "H",
            "Giant Rat"     => "r",
            "Giant"         => "J",  // J for Jotun
            "Horse"         => "h",
            "Unicorn"       => "u",
            "Centaur"       => "n",
            "Pegasus"       => "p",
            "Gryphon"       => "Y",
            "Manticore"     => "M",
            "Vampire"       => "V",
            "Spectre"       => "S",
            "Wraith"        => "R",
            "Skeleton"      => "s",
            "Zombie"        => "Z",
            "Ghost"         => "t",
            "Eagle"         => "E",
            "Green Dragon"  => "d",
            "Red Dragon"    => "D",
            "Golden Dragon" => "A",  // Aureum
            "Harpy"         => "y",
            "Lion"          => "L",
            "Faun"          => "F",
            _               => name.Length > 0 ? name[0].ToString() : "?"
        };
    }

    /// <summary>
    /// Display the spell list for a wizard choosing their spell.
    /// </summary>
    public void DrawSpellList(Wizard wizard, int worldAlignment)
    {
        Console.ForegroundColor = WizardColours[wizard.Colour % WizardColours.Length];
        Console.WriteLine($"\n═══ {wizard.Name}'s Spells ═══\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" 0. (Pass — cast no spell)");

        int index = 1;
        foreach (var spell in wizard.Spells)
        {
            if (spell.IsUsed && spell.Name != "Disbelieve") continue;

            int chance = spell.GetEffectiveCastingChance(worldAlignment);
            string alignMark = spell.AlignmentShift switch
            {
                > 0 => " (Law)",
                < 0 => " (Chaos)",
                _   => ""
            };

            string info = spell.Category == SpellCategory.Creature
                ? $"  [C:{spell.CreatureData!.Combat} D:{spell.CreatureData.Defence} Mv:{spell.CreatureData.Movement}]"
                : "";

            Console.ForegroundColor = spell.AlignmentShift switch
            {
                > 0 => ConsoleColor.Cyan,
                < 0 => ConsoleColor.Magenta,
                _   => ConsoleColor.White
            };

            Console.WriteLine($" {index,2}. {spell.Name,-20} {chance,3}%{alignMark}{info}");
            index++;
        }

        Console.ForegroundColor = ConsoleColor.White;
    }

    /// <summary>
    /// Display info about all living wizards.
    /// </summary>
    public void DrawWizardStatus(GameState game)
    {
        Console.WriteLine("\n── Wizards ──");
        foreach (var w in game.Wizards)
        {
            Console.ForegroundColor = w.IsAlive
                ? WizardColours[w.Colour % WizardColours.Length]
                : ConsoleColor.DarkGray;

            string status = w.IsAlive ? $"({w.X},{w.Y})" : "DEAD";
            string mount = w.IsMounted ? $" riding {w.Mount!.Stats.Name}" : "";
            string equip = string.Join(", ",
                new[] {
                    w.HasMagicSword  ? "Sword"  : null,
                    w.HasMagicKnife  ? "Knife"  : null,
                    w.HasMagicShield ? "Shield" : null,
                    w.HasMagicArmour ? "Armour" : null,
                    w.HasMagicBow    ? "Bow"    : null,
                    w.HasMagicWings  ? "Wings"  : null,
                    w.HasShadowForm  ? "Shadow" : null
                }.Where(s => s != null));

            Console.Write($"  {w.Name,-12} {status,-8}{mount}");
            if (equip.Length > 0) Console.Write($"  [{equip}]");
            int creatureCount = game.GetCreaturesOwnedBy(w.Id).Count;
            if (creatureCount > 0) Console.Write($"  {creatureCount} creatures");
            Console.WriteLine();
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    private static string AlignmentString(int alignment)
    {
        if (alignment > 0) return $"Law +{alignment}";
        if (alignment < 0) return $"Chaos {alignment}";
        return "Neutral";
    }
}
