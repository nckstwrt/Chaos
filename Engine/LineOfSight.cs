namespace Chaos.Engine;

using Chaos.Enums;
using Chaos.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Line-of-sight checking for ranged attacks and spells.
///
/// Verified from Z80 at 0xBEEF–0xBF30:
///   The original uses a Bresenham-style line walk between the
///   attacker and target. It steps along the major axis, accumulating
///   error on the minor axis, and checks each cell along the way.
///   If any intermediate cell is occupied (creature, wizard, wall,
///   terrain), the line of sight is blocked.
///
///   The endpoints (attacker and target positions) are NOT checked —
///   only the cells in between. This means you can always shoot at
///   something you're adjacent to (distance 1 = no intermediate cells).
///
/// Usage:
///   if (!LineOfSight.HasClearPath(game.Board, ax, ay, tx, ty))
///       return "No line of sight to target!";
/// </summary>
public static class LineOfSight
{
    /// <summary>
    /// Check whether there is a clear line of sight from (x1,y1) to (x2,y2).
    /// Returns true if the path is clear (no obstacles between the two points).
    /// The start and end cells themselves are not checked — only intermediate cells.
    /// </summary>
    public static bool HasClearPath(GameBoard board, int x1, int y1, int x2, int y2)
    {
        // Adjacent cells always have clear LOS (no intermediate cells)
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        if (dx <= 1 && dy <= 1)
            return true;

        // Bresenham line walk from (x1,y1) to (x2,y2)
        // Check every intermediate cell for obstructions
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;

        // Use the Bresenham algorithm matching the original Z80 implementation:
        // step along the major axis, accumulate error on the minor axis
        if (dx >= dy)
        {
            // X is the major axis
            int error = dx / 2;
            int y = y1;

            for (int x = x1 + sx; x != x2; x += sx)
            {
                error -= dy;
                if (error < 0)
                {
                    y += sy;
                    error += dx;
                }

                if (IsBlocking(board, x, y))
                    return false;
            }
        }
        else
        {
            // Y is the major axis
            int error = dy / 2;
            int x = x1;

            for (int y = y1 + sy; y != y2; y += sy)
            {
                error -= dx;
                if (error < 0)
                {
                    x += sx;
                    error += dy;
                }

                if (IsBlocking(board, x, y))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if a cell blocks line of sight.
    /// Walls, creatures, wizards, trees, castles, and citadels all block.
    /// Empty cells, dead bodies, and fire/blob do NOT block.
    /// </summary>
    private static bool IsBlocking(GameBoard board, int x, int y)
    {
        if (!board.InBounds(x, y))
            return true;

        var content = board[x, y].Content;
        return content switch
        {
            CellContent.Empty       => false,
            CellContent.DeadBody    => false,
            CellContent.MagicFire   => false, // Fire is low to the ground
            CellContent.GooeyBlob   => false, // Blob is low
            _                       => true   // Everything else blocks
        };
    }

    /// <summary>
    /// Get all valid ranged targets from (x,y) that have clear line of sight.
    /// Filters out targets that are blocked by obstacles.
    /// </summary>
    public static List<(int X, int Y)> GetVisibleTargets(
        GameBoard board, int x, int y, int range, int ownerWizardId)
    {
        var targets = new List<(int, int)>();

        for (int tx = 0; tx < GameBoard.Width; tx++)
        for (int ty = 0; ty < GameBoard.Height; ty++)
        {
            if (tx == x && ty == y) continue;
            if (GameBoard.Distance(x, y, tx, ty) > range) continue;

            var cell = board[tx, ty];
            bool isEnemy = (cell.Content == CellContent.Creature
                            && cell.Creature?.OwnerWizardId != ownerWizardId)
                        || (cell.Content == CellContent.Wizard
                            && cell.Wizard?.Id != ownerWizardId);

            if (isEnemy && HasClearPath(board, x, y, tx, ty))
                targets.Add((tx, ty));
        }

        return targets;
    }
}
