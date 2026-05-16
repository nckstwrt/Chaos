namespace Chaos.UI.RaylibUI;

using Chaos.Models;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Animated beam effect for ranged attacks (Lightning, Magic Bolt, etc.)
/// and ranged creature attacks (dragon breath, elf arrows, etc.).
///
/// From the Z80 binary at 0x9CE0: the beam draws 9 segments along a
/// line from attacker to target, one per frame with a brief delay.
/// Colour attribute 0x47 = bright white. The beam uses 8×8 pixel
/// patterns from 0xBFB7 (dot/wedge shapes at line endpoints).
///
/// The effect plays over ~30 frames: beam extends out, holds briefly,
/// then fades. During the effect, game input is paused.
/// </summary>
public class BeamEffect
{
    private int _fromX, _fromY, _toX, _toY;
    private int _frame;
    private int _totalFrames;
    private bool _active;
    private Color _colour;

    // The Bresenham-stepped positions along the beam line
    private List<(int X, int Y)> _segments = new();

    public bool IsActive => _active;

    /// <summary>
    /// Start a beam effect from one board cell to another.
    /// Coordinates are board positions (0-14, 0-9).
    /// </summary>
    public void Start(int fromBoardX, int fromBoardY, int toBoardX, int toBoardY,
                      Color? colour = null)
    {
        _fromX = fromBoardX;
        _fromY = fromBoardY;
        _toX = toBoardX;
        _toY = toBoardY;
        _colour = colour ?? new Color(255, 255, 255, 255); // bright white
        _frame = 0;
        _active = true;

        // Build the segment list using Bresenham line
        _segments = BuildLine(fromBoardX, fromBoardY, toBoardX, toBoardY);
        // Total: segments extend out (1 per 3 frames) + hold (10 frames) + fade (5 frames)
        _totalFrames = _segments.Count * 3 + 15;
    }

    /// <summary>
    /// Advance one frame. Returns false when the effect is finished.
    /// </summary>
    public bool Tick()
    {
        if (!_active) return false;
        _frame++;
        if (_frame >= _totalFrames)
        {
            _active = false;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Draw the beam onto the render texture.
    /// Call this during the Draw() phase, after the board but before the border.
    /// boardOffsetX/Y are the pixel offsets of the board on screen.
    /// cellSize is the pixel size of each cell (16).
    /// </summary>
    public void Draw(int boardOffsetX, int boardOffsetY, int cellSize)
    {
        if (!_active) return;

        int extendPhase = _segments.Count * 3;
        int holdEnd = extendPhase + 10;

        // How many segments are currently visible
        int visibleCount;
        byte alpha;

        if (_frame < extendPhase)
        {
            // Extending: one new segment every 3 frames
            visibleCount = _frame / 3 + 1;
            alpha = 255;
        }
        else if (_frame < holdEnd)
        {
            // Holding: all segments visible, flash effect
            visibleCount = _segments.Count;
            alpha = (byte)((_frame % 4 < 2) ? 255 : 180);
        }
        else
        {
            // Fading out
            visibleCount = _segments.Count;
            float fadeProgress = (float)(_frame - holdEnd) / (_totalFrames - holdEnd);
            alpha = (byte)(255 * (1f - fadeProgress));
        }

        visibleCount = Math.Min(visibleCount, _segments.Count);
        Color drawColour = new(_colour.R, _colour.G, _colour.B, alpha);

        // Draw each visible segment as a bright cell-sized flash
        for (int i = 0; i < visibleCount; i++)
        {
            var (bx, by) = _segments[i];
            int px = boardOffsetX + bx * cellSize;
            int py = boardOffsetY + by * cellSize;

            // Skip the source cell (attacker's position)
            if (i == 0) continue;

            if (i == visibleCount - 1 && _frame < extendPhase)
            {
                // Leading edge: draw expanding wedge pattern
                DrawBeamHead(px, py, cellSize, drawColour);
            }
            else
            {
                // Middle segments: draw beam line through cell center
                DrawBeamSegment(px, py, cellSize, bx, by, drawColour);
            }
        }

        // Impact flash on the target cell when beam arrives
        if (visibleCount >= _segments.Count && _frame < holdEnd)
        {
            var (tx, ty) = _segments[^1];
            int tpx = boardOffsetX + tx * cellSize;
            int tpy = boardOffsetY + ty * cellSize;

            // Expanding impact flash
            bool flash = (_frame % 3) < 2;
            if (flash)
            {
                Color impactColour = new((byte)255, (byte)255, (byte)0, alpha); // yellow flash
                Raylib.DrawRectangle(tpx, tpy, cellSize, cellSize, impactColour);
            }
        }
    }

    private void DrawBeamSegment(int px, int py, int cellSize,
                                  int boardX, int boardY, Color colour)
    {
        // Draw a 2-pixel-wide line through the cell center, oriented
        // toward the target
        int cx = px + cellSize / 2;
        int cy = py + cellSize / 2;

        // Direction from this cell toward target
        int dx = Math.Sign(_toX - boardX);
        int dy = Math.Sign(_toY - boardY);

        if (dx == 0 && dy == 0) { dx = 1; } // fallback

        // Draw a short thick line segment through the cell
        int len = cellSize / 2;
        for (int t = -len; t <= len; t++)
        {
            int x1 = cx + dx * t;
            int y1 = cy + dy * t;
            Raylib.DrawPixel(x1, y1, colour);
            // Second pixel for thickness
            if (dy != 0) Raylib.DrawPixel(x1 + 1, y1, colour);
            else Raylib.DrawPixel(x1, y1 + 1, colour);
        }
    }

    private void DrawBeamHead(int px, int py, int cellSize, Color colour)
    {
        // Expanding wedge at the leading edge of the beam
        int cx = px + cellSize / 2;
        int cy = py + cellSize / 2;

        // Small cross/diamond pattern
        Raylib.DrawPixel(cx, cy, colour);
        Raylib.DrawPixel(cx - 1, cy, colour);
        Raylib.DrawPixel(cx + 1, cy, colour);
        Raylib.DrawPixel(cx, cy - 1, colour);
        Raylib.DrawPixel(cx, cy + 1, colour);
        Raylib.DrawPixel(cx - 1, cy - 1, colour);
        Raylib.DrawPixel(cx + 1, cy + 1, colour);
    }

    /// <summary>
    /// Bresenham line from (x1,y1) to (x2,y2) in board coordinates.
    /// Returns all cells the beam passes through.
    /// </summary>
    private static List<(int X, int Y)> BuildLine(int x1, int y1, int x2, int y2)
    {
        var result = new List<(int, int)>();
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        int cx = x1, cy = y1;

        while (true)
        {
            result.Add((cx, cy));
            if (cx == x2 && cy == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx) { err += dx; cy += sy; }
        }

        return result;
    }
}