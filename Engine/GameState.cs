namespace Chaos.Engine;

using Chaos.Data;
using Chaos.Enums;
using Chaos.Models;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central game state. Orchestrates the turn sequence, holds the board,
/// wizards, and world alignment. This is the "brain" of the port.
///
/// Original game loop (simplified from Z80 disassembly):
///   1. For each wizard (in order):
///      a. Choose spell (or pass)
///   2. For each wizard (in order):
///      a. Cast chosen spell
///      b. Move wizard + all owned creatures
///   3. Spread Gooey Blob / Magic Fire
///   4. Check for victory (last wizard standing)
///   5. Repeat from 1
/// </summary>
public class GameState
{
    public GameBoard Board { get; } = new();
    public List<Wizard> Wizards { get; } = new();
    public Random Rng { get; }

    /// <summary>
    /// World alignment: ranges from -8 (Chaotic) to +8 (Lawful).
    /// Shifts each time a spell with a non-zero AlignmentShift is cast.
    /// Affects casting chances for aligned spells.
    /// </summary>
    public int WorldAlignment { get; set; } = 0;

    /// <summary>Current turn number (1-based).</summary>
    public int TurnNumber { get; set; } = 1;

    /// <summary>Index into Wizards for whose turn it is.</summary>
    public int CurrentWizardIndex { get; set; } = 0;

    public TurnPhase Phase { get; set; } = TurnPhase.ChooseSpell;

    /// <summary>All creatures currently on the board.</summary>
    public List<BoardCreature> Creatures { get; } = new();

    public Wizard CurrentWizard => Wizards[CurrentWizardIndex];

    public GameState(int seed = -1)
    {
        Rng = seed >= 0 ? new Random(seed) : new Random();
    }

    // ── Setup ───────────────────────────────────────────────────────

    /// <summary>
    /// Set up a new game with the given number of players.
    /// Places wizards in their starting positions (spread around the board)
    /// and deals spell hands.
    /// </summary>
    public void SetupGame(int numWizards, int numHumans = 1)
    {
        // Starting positions for 2–8 wizards.
        // The original game uses fixed positions that spread wizards evenly.
        var startPositions = GetStartPositions(numWizards);

        for (int i = 0; i < numWizards; i++)
        {
            var (sx, sy) = startPositions[i];
            var wizard = new Wizard
            {
                Id = i,
                Name = SpellDatabase.DefaultWizardNames[i],
                IsHuman = i < numHumans,
                Colour = i,
                X = sx,
                Y = sy,
                Spells = SpellDatabase.DealSpellHand(Rng)
            };

            Wizards.Add(wizard);

            // Place wizard on the board
            Board[sx, sy].Content = CellContent.Wizard;
            Board[sx, sy].Wizard = wizard;
        }
    }

    /// <summary>
    /// Spread wizards around the 15×10 board.
    /// The first 3 positions are verified from the Z80 init code at 0x8A56:
    ///   Wizard 0: LD BC,(2,2) → (col=1, row=1) in 0-based coords
    ///   Wizard 1: LD BC,(8,4) → (col=7, row=3)
    ///   Wizard 2: LD BC,(2,9) → (col=1, row=8)
    /// Positions 3–7 are estimated from symmetry and typical Chaos layouts.
    /// The original uses 1-based coordinates internally.
    /// </summary>
    private static List<(int X, int Y)> GetStartPositions(int count)
    {
        var all = new List<(int, int)>
        {
            ( 1, 1),  // Wizard 0: top-left area (verified from binary)
            ( 7, 3),  // Wizard 1: center area (verified from binary)
            ( 1, 8),  // Wizard 2: bottom-left area (verified from binary)
            (13, 1),  // Wizard 3: top-right area
            (13, 8),  // Wizard 4: bottom-right area
            ( 7, 7),  // Wizard 5: center-bottom
            ( 4, 5),  // Wizard 6: left-center
            (10, 5),  // Wizard 7: right-center
        };
        return all.Take(count).ToList();
    }

    // ── Turn progression ────────────────────────────────────────────

    /// <summary>
    /// Advance to the next living wizard. Returns false if the game is over.
    /// </summary>
    public bool AdvanceToNextWizard()
    {
        int startIndex = CurrentWizardIndex;
        do
        {
            CurrentWizardIndex = (CurrentWizardIndex + 1) % Wizards.Count;
            if (CurrentWizardIndex == 0)
                TurnNumber++;
        }
        while (!Wizards[CurrentWizardIndex].IsAlive && CurrentWizardIndex != startIndex);

        return GetAliveWizards().Count > 1;
    }

    /// <summary>
    /// Begin a new round: all wizards pick spells, then cast/move in order.
    /// This matches the original two-pass structure.
    /// </summary>
    public void BeginSpellSelectionPhase()
    {
        Phase = TurnPhase.ChooseSpell;
        CurrentWizardIndex = 0;

        // Skip to first living wizard
        while (CurrentWizardIndex < Wizards.Count && !CurrentWizard.IsAlive)
            CurrentWizardIndex++;
    }

    public void BeginCastAndMovePhase()
    {
        Phase = TurnPhase.CastSpell;
        CurrentWizardIndex = 0;

        // Reset all creature movement flags
        foreach (var creature in Creatures)
            creature.ResetForNewTurn();

        while (CurrentWizardIndex < Wizards.Count && !CurrentWizard.IsAlive)
            CurrentWizardIndex++;
    }

    // ── Queries ─────────────────────────────────────────────────────

    public List<Wizard> GetAliveWizards() =>
        Wizards.Where(w => w.IsAlive).ToList();

    public Wizard? GetWizardAt(int x, int y) =>
        Wizards.FirstOrDefault(w => w.IsAlive && w.X == x && w.Y == y);

    public BoardCreature? GetCreatureAt(int x, int y) =>
        Creatures.FirstOrDefault(c => c.X == x && c.Y == y);

    public List<BoardCreature> GetCreaturesOwnedBy(int wizardId) =>
        Creatures.Where(c => c.OwnerWizardId == wizardId).ToList();

    /// <summary>
    /// Check if a creature or wizard at (x,y) is adjacent to an enemy.
    /// This determines "engagement" — engaged units can't move freely.
    /// </summary>
    public bool IsAdjacentToEnemy(int x, int y, int ownerWizardId)
    {
        foreach (var (nx, ny) in Board.GetAdjacentCells(x, y))
        {
            var cell = Board[nx, ny];
            if (cell.Content == CellContent.Wizard && cell.Wizard?.Id != ownerWizardId)
                return true;
            if (cell.Content == CellContent.Creature && cell.Creature?.OwnerWizardId != ownerWizardId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check win condition: only one wizard (or team) left alive.
    /// </summary>
    public Wizard? CheckForWinner()
    {
        var alive = GetAliveWizards();
        return alive.Count == 1 ? alive[0] : null;
    }

    // ── Board manipulation ──────────────────────────────────────────

    /// <summary>
    /// Place a newly summoned creature on the board.
    /// </summary>
    public BoardCreature PlaceCreature(CreatureStats stats, int x, int y,
                                        int ownerWizardId, bool isIllusion)
    {
        var creature = new BoardCreature
        {
            Stats = stats.Clone(),
            X = x,
            Y = y,
            OwnerWizardId = ownerWizardId,
            IsIllusion = isIllusion,
            HasMoved = true  // Can't move on the turn it's summoned
        };

        Creatures.Add(creature);
        Board[x, y].Content = CellContent.Creature;
        Board[x, y].Creature = creature;

        return creature;
    }

    /// <summary>
    /// Remove a creature from the board (killed or disbelieved).
    /// Leaves a corpse if it wasn't an illusion.
    /// </summary>
    public void RemoveCreature(BoardCreature creature)
    {
        Creatures.Remove(creature);
        var cell = Board[creature.X, creature.Y];
        if (cell.Creature == creature)
        {
            cell.Creature = null;
            if (!creature.IsIllusion)
            {
                cell.Content = CellContent.DeadBody;
                cell.HasCorpse = true;
            }
            else
            {
                cell.Content = CellContent.Empty;
            }
        }
    }

    /// <summary>
    /// Kill a wizard. Remove them from the board, destroy all their creatures.
    /// </summary>
    public void KillWizard(Wizard wizard)
    {
        wizard.IsAlive = false;
        Board[wizard.X, wizard.Y].Content = CellContent.Empty;
        Board[wizard.X, wizard.Y].Wizard = null;

        // All owned creatures die when their wizard dies
        var owned = GetCreaturesOwnedBy(wizard.Id).ToList();
        foreach (var c in owned)
            RemoveCreature(c);
    }
}
