namespace Chaos.Models;

using Chaos.Enums;
using System;
using System.Collections.Generic;

/// <summary>
/// The 15×10 game arena. Verified from Z80 boundary check code at 0xBE58:
///   LD A,15; CP C; RET M (column must be &lt; 15)
///   LD A,10; CP B; RET M (row must be &lt; 10)
///
/// Each creature sprite is 2×2 characters on the Spectrum screen.
/// 15 × 2 = 30 chars fits in the 32-column display with 1-char borders.
/// 10 × 2 = 20 chars fits in the 24-row display with a status area below.
///
/// State tables in the original are 320 bytes (32 bytes/row × 10 rows),
/// with unused border columns padding each row to 32.
/// </summary>
public class GameBoard
{
    public const int Width = 15;
    public const int Height = 10;

    private readonly BoardCell[,] _cells = new BoardCell[Width, Height];

    public GameBoard()
    {
        Clear();
    }

    public void Clear()
    {
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
            _cells[x, y] = new BoardCell();
    }

    public BoardCell this[int x, int y]
    {
        get => _cells[x, y];
        set => _cells[x, y] = value;
    }

    public bool InBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>
    /// Check if a cell is empty and available for placement.
    /// </summary>
    public bool IsEmpty(int x, int y) =>
        InBounds(x, y) && _cells[x, y].Content == CellContent.Empty;

    /// <summary>
    /// Get all cells adjacent to (x,y), including diagonals (8-way).
    /// The original game uses 8-directional adjacency for everything.
    /// </summary>
    public IEnumerable<(int X, int Y)> GetAdjacentCells(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx;
            int ny = y + dy;
            if (InBounds(nx, ny))
                yield return (nx, ny);
        }
    }

    /// <summary>
    /// Compute straight-line distance (Chebyshev / "chessboard" distance).
    /// This is what the original uses for range calculations.
    /// </summary>
    public static int Distance(int x1, int y1, int x2, int y2) =>
        Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
}

/// <summary>
/// A single cell on the board.
/// </summary>
public class BoardCell
{
    public CellContent Content { get; set; } = CellContent.Empty;

    /// <summary>The creature in this cell (if Content is Creature).</summary>
    public BoardCreature? Creature { get; set; }

    /// <summary>The wizard in this cell (if Content is Wizard).</summary>
    public Wizard? Wizard { get; set; }

    /// <summary>
    /// Owner wizard ID for terrain features (Magic Fire, Gooey Blob, etc.).
    /// </summary>
    public int OwnerWizardId { get; set; } = -1;

    /// <summary>
    /// A dead creature body may lie under a living creature.
    /// Corpses are relevant because Raise Dead can resurrect them.
    /// </summary>
    public bool HasCorpse { get; set; }

    /// <summary>Is this cell currently empty and pathable?</summary>
    public bool IsPassable => Content == CellContent.Empty
                           || Content == CellContent.DeadBody;

    /// <summary>
    /// A creature trapped under a Gooey Blob. The creature is hidden
    /// but alive. If the blob is destroyed (by attacking it), the
    /// trapped creature is freed and reappears on this cell.
    /// Verified from original: "any unit covered up by a gooey blob
    /// will be able to carry on once it is uncovered."
    /// Only Gooey Blob traps — Magic Fire kills outright.
    /// </summary>
    public BoardCreature? TrappedCreature { get; set; }
}
