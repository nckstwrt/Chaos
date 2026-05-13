namespace Chaos.UI.RaylibUI;

using Raylib_cs;
using System.Numerics;
using Chaos.Engine;
using Chaos.Enums;
using Chaos.Models;
using Chaos.Data;
using System.Linq;
using System;
using System.Collections.Generic;

/// <summary>
/// Raylib graphical frontend for Chaos.
///
/// Renders to a 256×192 RenderTexture (exact Spectrum resolution)
/// then scales up to the window with nearest-neighbour filtering.
/// All input is immediate keypress — no Enter key needed.
///
/// Key bindings:
///   Arrow keys    — move cursor / move unit step by step
///   1–9, 0        — select spell (0 = pass)
///   I / R         — illusion / real for creature spells
///   Space / Enter — confirm target / execute move / attack
///   D             — done (end this unit's movement)
///   N             — skip to next creature
///   Escape        — cancel targeting
/// </summary>
public class RaylibGame
{
    // ── Spectrum display ─────────────────────────────────────────
    private const int NativeW = 256;
    private const int NativeH = 192;
    private const int Scale = 4;

    // Board: 15×10 cells × 16px each = 240×160, offset 8px from left
    private const int BoardX = 8;
    private const int BoardY = 0;
    private const int Cell = 16;
    private const int StatusY = 160;

    // ── Spectrum colours ─────────────────────────────────────────
    private static readonly Color[] WizardColours =
    {
        new(255,0,0,255), new(255,0,255,255), new(0,255,0,255),
        new(0,255,255,255), new(255,255,0,255), new(255,255,255,255),
        new(192,192,0,255), new(0,192,192,255),
    };

    private static readonly Color BorderBlue = new(0, 0, 128, 255);
    private static readonly Color GridBlue = new(0, 0, 64, 255);
    private static readonly Color DimWhite = new(192, 192, 192, 255);
    private static readonly Color DimGrey = new(128, 128, 128, 255);

    // ── Game engine ──────────────────────────────────────────────
    private readonly GameState _game;
    private readonly CombatEngine _combat;
    private readonly SpellCaster _spellCaster;
    private readonly MovementEngine _movement;
    private readonly SpreadingEngine _spreading;
    private readonly MagicWoodEngine _magicWood;

    // ── Rendering ────────────────────────────────────────────────
    private RenderTexture2D _target;
    private readonly SpriteStore _sprites = new();
    private readonly SpectrumFont _font = new();
    private readonly string _spritePath;

    // ── UI state machine ─────────────────────────────────────────
    private enum Phase
    {
        SpellSelect,
        IllusionChoice,
        Targeting,
        MultiPlace,
        MoveWizard,
        MoveCreature,
        AITurn,
        Message,     // show status, wait for any key
        GameOver,
    }

    private Phase _phase;
    private int _curX, _curY;
    private int _wizIdx;
    private int _creIdx;
    private int _movesLeft;
    private int _scrollOff;
    private int _placesLeft;
    private CellContent _placeTerrain;
    private string _msg1 = "";
    private string _msg2 = "";
    private int _frame;

    public RaylibGame(string spritePath)
    {
        _spritePath = spritePath;
        _game = new GameState();
        _combat = new CombatEngine(_game);
        _spellCaster = new SpellCaster(_game, _combat);
        _movement = new MovementEngine(_game, _combat);
        _spreading = new SpreadingEngine(_game);
        _magicWood = new MagicWoodEngine(_game);
    }

    // ═════════════════════════════════════════════════════════════
    //  ENTRY POINT
    // ═════════════════════════════════════════════════════════════

    public void Run(int numWizards, int numHumans)
    {
        Raylib.InitWindow(NativeW * Scale, NativeH * Scale,
            "CHAOS: The Battle of Wizards");
        Raylib.SetTargetFPS(60);
        Raylib.SetWindowState(ConfigFlags.ResizableWindow);

        _target = Raylib.LoadRenderTexture(NativeW, NativeH);
        Raylib.SetTextureFilter(_target.Texture, TextureFilter.Point);

        _font.Init();
        _sprites.LoadAll(_spritePath);

        _game.SetupGame(numWizards, numHumans);
        _game.BeginSpellSelectionPhase();
        _wizIdx = 0;
        SkipDeadWizards();
        _phase = Phase.SpellSelect;

        while (!Raylib.WindowShouldClose())
        {
            _frame++;
            HandleInput();
            Update();
            Draw();
        }

        _sprites.UnloadAll();
        _font.Unload();
        Raylib.UnloadRenderTexture(_target);
        Raylib.CloseWindow();
    }

    // ═════════════════════════════════════════════════════════════
    //  INPUT
    // ═════════════════════════════════════════════════════════════

    private void HandleInput()
    {
        switch (_phase)
        {
            case Phase.Message:
                if (AnyKeyPressed()) BeginWizMove();
                break;
            case Phase.GameOver:
                break;
            case Phase.SpellSelect:
                InputSpellSelect();
                break;
            case Phase.IllusionChoice:
                InputIllusion();
                break;
            case Phase.Targeting:
            case Phase.MultiPlace:
                InputCursor();
                break;
            case Phase.MoveWizard:
            case Phase.MoveCreature:
                InputMovement();
                break;
        }
    }

    private void InputSpellSelect()
    {
        var wiz = _game.Wizards[_wizIdx];
        if (!wiz.IsHuman) return;

        var avail = GetAvailableSpells(wiz);

        if (Raylib.IsKeyPressed(KeyboardKey.Zero))
        {
            wiz.SelectedSpell = null;
            NextSpellSelect();
            return;
        }

        for (int k = 1; k <= 9; k++)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Zero + k))
            {
                int idx = k - 1 + _scrollOff;
                if (idx < avail.Count)
                {
                    wiz.SelectedSpell = avail[idx];
                    wiz.CastingAsIllusion = false;
                    if (wiz.SelectedSpell.Category == SpellCategory.Creature)
                    {
                        int ch = wiz.SelectedSpell.GetEffectiveCastingChance(_game.WorldAlignment);
                        _msg1 = $"{wiz.SelectedSpell.Name} ({ch * 10}%)";
                        _msg2 = "(I)llusion 100% or (R)eal?";
                        _phase = Phase.IllusionChoice;
                    }
                    else
                    {
                        NextSpellSelect();
                    }
                }
                return;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down)) _scrollOff++;
        if (Raylib.IsKeyPressed(KeyboardKey.Up) && _scrollOff > 0) _scrollOff--;
    }

    private void InputIllusion()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.I) || Raylib.IsKeyPressed(KeyboardKey.Y))
        {
            _game.Wizards[_wizIdx].CastingAsIllusion = true;
            NextSpellSelect();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.R) || Raylib.IsKeyPressed(KeyboardKey.N))
        {
            _game.Wizards[_wizIdx].CastingAsIllusion = false;
            NextSpellSelect();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _game.Wizards[_wizIdx].SelectedSpell = null;
            _phase = Phase.SpellSelect;
        }
    }

    private void InputCursor()
    {
        MoveCursor();

        if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            if (_phase == Phase.Targeting)
            {
                var wiz = _game.Wizards[_wizIdx];
                _msg1 = _spellCaster.CastSpell(wiz, _curX, _curY);

                if (_spellCaster.RemainingPlacements > 0)
                {
                    _placesLeft = _spellCaster.RemainingPlacements;
                    _placeTerrain = _spellCaster.RemainingTerrain;
                    _msg2 = $"{_placesLeft} more. Arrows+Space, D=done";
                    _phase = Phase.MultiPlace;
                }
                else
                {
                    ShowMsgThenMove();
                }
            }
            else if (_phase == Phase.MultiPlace)
            {
                var wiz = _game.Wizards[_wizIdx];
                string? err = _spellCaster.PlaceSingleTerrain(
                    wiz, wiz.SelectedSpell!, _curX, _curY, _placeTerrain);
                if (err != null)
                {
                    _msg2 = err;
                }
                else
                {
                    _spellCaster.DecrementPlacements();
                    _placesLeft = _spellCaster.RemainingPlacements;
                    if (_placesLeft <= 0)
                    {
                        _spellCaster.ClearPlacements();
                        ShowMsgThenMove();
                    }
                    else
                    {
                        _msg2 = $"{_placesLeft} more. Arrows+Space, D=done";
                    }
                }
            }
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _spellCaster.ClearPlacements();
            if (_phase == Phase.Targeting)
                BeginWizMove();
            else
                ShowMsgThenMove();
        }
    }

    private void InputMovement()
    {
        MoveCursor();

        var wiz = _game.Wizards[_wizIdx];
        bool isWiz = _phase == Phase.MoveWizard;

        if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            if (isWiz)
            {
                if (GameBoard.Distance(wiz.X, wiz.Y, _curX, _curY) == 1)
                {
                    _msg1 = _movement.MoveWizard(wiz, _curX, _curY);
                    _movesLeft--;
                    _curX = wiz.X; _curY = wiz.Y;
                    if (_movesLeft <= 0) FinishWizMove();
                }
            }
            else
            {
                var creatures = _game.GetCreaturesOwnedBy(wiz.Id);
                if (_creIdx < creatures.Count)
                {
                    var c = creatures[_creIdx];
                    if (GameBoard.Distance(c.X, c.Y, _curX, _curY) == 1)
                    {
                        _msg1 = _movement.MoveCreature(c, _curX, _curY);
                        _movesLeft--;
                        _curX = c.X; _curY = c.Y;
                        if (_movesLeft <= 0 || c.HasAttacked) NextCreature();
                    }
                }
            }
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.D))
        {
            if (isWiz) FinishWizMove();
            else NextCreature();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.N) && !isWiz)
        {
            NextCreature();
        }
    }

    private void MoveCursor()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up) && _curY > 0) _curY--;
        if (Raylib.IsKeyPressed(KeyboardKey.Down) && _curY < GameBoard.Height - 1) _curY++;
        if (Raylib.IsKeyPressed(KeyboardKey.Left) && _curX > 0) _curX--;
        if (Raylib.IsKeyPressed(KeyboardKey.Right) && _curX < GameBoard.Width - 1) _curX++;
    }

    private static bool AnyKeyPressed() => Raylib.GetKeyPressed() != 0;

    // ═════════════════════════════════════════════════════════════
    //  PHASE TRANSITIONS
    // ═════════════════════════════════════════════════════════════

    private void NextSpellSelect()
    {
        _scrollOff = 0;
        _wizIdx++;
        if (SkipDeadWizards())
        {
            _phase = Phase.SpellSelect;
        }
        else
        {
            // All selected — begin casting
            _game.BeginCastAndMovePhase();
            _wizIdx = 0;
            SkipDeadWizards();
            BeginCastForCurrent();
        }
    }

    private void BeginCastForCurrent()
    {
        if (_wizIdx >= _game.Wizards.Count) { DoEndOfRound(); return; }

        var wiz = _game.Wizards[_wizIdx];
        _game.CurrentWizardIndex = _wizIdx;

        // Magic Wood grant
        string? woodMsg = _magicWood.CheckMagicWood(wiz);
        if (woodMsg != null) _msg1 = woodMsg;

        if (!wiz.IsHuman)
        {
            _phase = Phase.AITurn;
            return;
        }

        if (wiz.SelectedSpell != null)
        {
            bool self = wiz.SelectedSpell.Category is SpellCategory.MagicDefence
                        or SpellCategory.AlignmentSpell
                     || wiz.SelectedSpell.Name is "Magic Wings" or "Shadow Form" or "Turmoil";

            if (self)
            {
                _msg1 = _spellCaster.CastSpell(wiz, wiz.X, wiz.Y);
                _spellCaster.ClearPlacements();
                ShowMsgThenMove();
            }
            else
            {
                _curX = wiz.X; _curY = wiz.Y;
                _msg1 = $"Target {wiz.SelectedSpell.Name}";
                _msg2 = "Arrows + Space to target";
                _phase = Phase.Targeting;
            }
        }
        else
        {
            BeginWizMove();
        }
    }

    private void BeginWizMove()
    {
        var wiz = _game.Wizards[_wizIdx];
        _curX = wiz.X; _curY = wiz.Y;
        _movesLeft = wiz.EffectiveMovement;
        _msg1 = $"{wiz.Name}: {_movesLeft} moves";
        _msg2 = "Arrows+Space=move, D=done";
        _phase = Phase.MoveWizard;
    }

    private void FinishWizMove()
    {
        _creIdx = 0;
        NextCreature();
    }

    private void NextCreature()
    {
        var wiz = _game.Wizards[_wizIdx];
        var creatures = _game.GetCreaturesOwnedBy(wiz.Id);

        // Find next unmoved creature
        while (_creIdx < creatures.Count && creatures[_creIdx].HasMoved)
            _creIdx++;

        if (_creIdx < creatures.Count && wiz.IsHuman)
        {
            var c = creatures[_creIdx];
            _curX = c.X; _curY = c.Y;
            _movesLeft = c.Stats.Movement;
            _msg1 = $"{c.Stats.Name}: {_movesLeft} moves";
            _msg2 = "Arrows+Space, D=done, N=next";
            _phase = Phase.MoveCreature;
        }
        else
        {
            FinishWizTurn();
        }
    }

    private void FinishWizTurn()
    {
        if (_game.CheckForWinner() is { } winner)
        {
            _msg1 = $"{winner.Name} WINS THE BATTLE!";
            _msg2 = "";
            _phase = Phase.GameOver;
            return;
        }

        _wizIdx++;
        if (SkipDeadWizards())
            BeginCastForCurrent();
        else
            DoEndOfRound();
    }

    private void DoEndOfRound()
    {
        var msgs = _spreading.SpreadAll();
        if (msgs.Count > 0)
            _msg1 = string.Join(" ", msgs.Take(3));

        _game.BeginSpellSelectionPhase();
        _wizIdx = 0;
        _scrollOff = 0;
        SkipDeadWizards();
        _phase = Phase.SpellSelect;
    }

    private bool SkipDeadWizards()
    {
        while (_wizIdx < _game.Wizards.Count && !_game.Wizards[_wizIdx].IsAlive)
            _wizIdx++;
        return _wizIdx < _game.Wizards.Count;
    }

    private void ShowMsgThenMove()
    {
        _phase = Phase.Message;
    }

    // ═════════════════════════════════════════════════════════════
    //  UPDATE (AI)
    // ═════════════════════════════════════════════════════════════

    private void Update()
    {
        // AI spell selection — auto-pick and advance
        if (_phase == Phase.SpellSelect)
        {
            var wiz = _game.Wizards[_wizIdx];
            if (!wiz.IsHuman)
            {
                var avail = wiz.Spells
                    .Where(s => !s.IsUsed && s.Name != "Disbelieve").ToList();

                Spell? chosen = null;
                foreach (var s in avail)
                    if (s.Category == SpellCategory.Creature) chosen = s;
                if (chosen == null) chosen = avail.FirstOrDefault();

                if (chosen != null)
                {
                    wiz.SelectedSpell = chosen;
                    wiz.CastingAsIllusion = chosen.Category == SpellCategory.Creature
                        && chosen.GetEffectiveCastingChance(_game.WorldAlignment) < 4;
                }
                else
                {
                    wiz.SelectedSpell = null;
                }

                NextSpellSelect();
            }
            return;
        }

        if (_phase != Phase.AITurn) return;

        // AI casting and movement
        var aiWiz = _game.Wizards[_wizIdx];

        var aiAvail = aiWiz.Spells.Where(s => !s.IsUsed && s.Name != "Disbelieve").ToList();
        Spell? aiChosen = null;
        foreach (var s in aiAvail)
            if (s.Category == SpellCategory.Creature) aiChosen = s;
        if (aiChosen == null) aiChosen = aiAvail.FirstOrDefault();

        if (aiChosen != null)
        {
            aiWiz.SelectedSpell = aiChosen;
            aiWiz.CastingAsIllusion = aiChosen.Category == SpellCategory.Creature
                && aiChosen.GetEffectiveCastingChance(_game.WorldAlignment) < 4;

            if (aiChosen.Category == SpellCategory.Creature)
            {
                foreach (var (nx, ny) in _game.Board.GetAdjacentCells(aiWiz.X, aiWiz.Y))
                {
                    if (_game.Board.IsEmpty(nx, ny))
                    {
                        _spellCaster.CastSpell(aiWiz, nx, ny);
                        _spellCaster.ClearPlacements();
                        break;
                    }
                }
            }
            else
            {
                _spellCaster.CastSpell(aiWiz, aiWiz.X, aiWiz.Y);
                _spellCaster.ClearPlacements();
            }
        }

        var enemies = _game.GetAliveWizards().Where(w => w.Id != aiWiz.Id).ToList();
        if (enemies.Count > 0)
        {
            AIMoveUnit(aiWiz.X, aiWiz.Y, aiWiz.EffectiveMovement, aiWiz.Id, enemies,
                (x, y) => { _movement.MoveWizard(aiWiz, x, y); return (aiWiz.X, aiWiz.Y); });

            foreach (var c in _game.GetCreaturesOwnedBy(aiWiz.Id).ToList())
            {
                if (c.HasMoved) continue;
                AIMoveUnit(c.X, c.Y, c.Stats.Movement, c.OwnerWizardId, enemies,
                    (x, y) => { _movement.MoveCreature(c, x, y); return (c.X, c.Y); });
            }
        }

        FinishWizTurn();
    }

    private void AIMoveUnit(int sx, int sy, int moves, int ownerId,
        List<Wizard> enemies, Func<int, int, (int, int)> moveFunc)
    {
        int cx = sx, cy = sy;
        for (int i = 0; i < moves; i++)
        {
            var nearest = enemies.OrderBy(e => GameBoard.Distance(cx, cy, e.X, e.Y)).First();
            int bestD = GameBoard.Distance(cx, cy, nearest.X, nearest.Y);
            int bx = cx, by = cy;

            foreach (var (nx, ny) in _game.Board.GetAdjacentCells(cx, cy))
            {
                int d = GameBoard.Distance(nx, ny, nearest.X, nearest.Y);
                var cell = _game.Board[nx, ny];
                bool canEnter = cell.IsPassable
                    || (cell.Content == CellContent.Wizard && cell.Wizard?.Id != ownerId)
                    || (cell.Content == CellContent.Creature && cell.Creature?.OwnerWizardId != ownerId);
                if (d < bestD && canEnter) { bestD = d; bx = nx; by = ny; }
            }

            if (bx == cx && by == cy) break;
            (cx, cy) = moveFunc(bx, by);
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  DRAW
    // ═════════════════════════════════════════════════════════════

    private void Draw()
    {
        // Draw to native-resolution render texture
        Raylib.BeginTextureMode(_target);
        Raylib.ClearBackground(Color.Black);

        // Status bar — solid blue like original
        /*
        Raylib.DrawRectangle(0, StatusY, NativeW, NativeH - StatusY, BorderPaper);

        // Grid lines
        for (int x = 0; x <= GameBoard.Width; x++)
            Raylib.DrawLine(BoardX + x * Cell, BoardY,
                            BoardX + x * Cell, BoardY + GameBoard.Height * Cell, GridBlue);
        for (int y = 0; y <= GameBoard.Height; y++)
            Raylib.DrawLine(BoardX, BoardY + y * Cell,
                            BoardX + GameBoard.Width * Cell, BoardY + y * Cell, GridBlue);
        */
        // Cell contents
        for (int bx = 0; bx < GameBoard.Width; bx++)
        for (int by = 0; by < GameBoard.Height; by++)
            DrawCell(bx, by);

        // Cursor
        if (_phase is Phase.Targeting or Phase.MultiPlace
            or Phase.MoveWizard or Phase.MoveCreature)
        {
            if ((_frame / 8) % 2 == 0)
            {
                Raylib.DrawRectangleLines(
                    BoardX + _curX * Cell, BoardY + _curY * Cell,
                    Cell, Cell, Color.White);
            }
        }

        // ── Spectrum-authentic gradient dither border ──
        int boardRight = BoardX + GameBoard.Width * Cell;  // 248
        int boardBottom = StatusY;                          // 160

        // Top edge (between corners)
        DrawBorderHStrip(8, 0, boardRight - 8, BorderTop);

        // Bottom edge (between corners)
        DrawBorderHStrip(7, boardBottom - 8, boardRight - 7, BorderBottom);

        // Left edge (between corners)
        DrawBorderStrip(0, 8, boardBottom - 16, BorderLeft);

        // Right edge (between corners)
        DrawBorderStrip(boardRight, 8, boardBottom - 16, BorderRight);

        // Four corners
        DrawBorderCorner(0, 0, BorderTopLeft);
        DrawBorderCorner(boardRight, 0, BorderTopRight);
        DrawBorderCorner(0, boardBottom - 8, BorderBottomLeft);
        DrawBorderCorner(boardRight, boardBottom - 8, BorderBottomRight);

        // Status bar
        Raylib.DrawRectangle(0, StatusY, NativeW, NativeH - StatusY, Color.Black);
        DrawStatus();

        Raylib.EndTextureMode();

        // Scale up to window
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        float scaleX = (float)Raylib.GetScreenWidth() / NativeW;
        float scaleY = (float)Raylib.GetScreenHeight() / NativeH;
        float scale = Math.Min(scaleX, scaleY);
        float offsetX = (Raylib.GetScreenWidth() - NativeW * scale) / 2;
        float offsetY = (Raylib.GetScreenHeight() - NativeH * scale) / 2;

        // RenderTexture Y is flipped — use negative height in source rect
        Raylib.DrawTexturePro(
            _target.Texture,
            new Rectangle(0, 0, NativeW, -NativeH),
            new Rectangle(offsetX, offsetY, NativeW * scale, NativeH * scale),
            Vector2.Zero, 0, Color.White);

        Raylib.EndDrawing();
    }

    private void DrawCell(int bx, int by)
    {
        var cell = _game.Board[bx, by];
        int px = BoardX + bx * Cell;
        int py = BoardY + by * Cell;

        switch (cell.Content)
        {
            case CellContent.Wizard when cell.Wizard != null:
                DrawSpriteOrFallback(px, py,
                    $"wizard_{cell.Wizard.Id + 1}_frame0",
                    'W', WizardColours[cell.Wizard.Id % 8]);
                break;

            case CellContent.Creature when cell.Creature != null:
                int obj = GetObjId(cell.Creature.Stats.Name);
                int fc = GetFrames(obj);
                int f = PingPong(_frame / 15, fc);
                DrawSpriteOrFallback(px, py,
                    $"creature_{obj:D2}_frame{f}",
                    'c', WizardColours[cell.Creature.OwnerWizardId % 8]);
                break;

            case CellContent.GooeyBlob:
                int bfc = GetFrames(34);
                DrawSpriteOrFallback(px, py,
                    $"creature_34_frame{PingPong(_frame / 15, bfc)}",
                    'g', new Color(0, 255, 0, 255));
                break;

            case CellContent.MagicFire:
                int ffc = GetFrames(35);
                DrawSpriteOrFallback(px, py,
                    $"creature_35_frame{PingPong(_frame / 15, ffc)}",
                    'f', new Color(255, 0, 0, 255));
                break;

            case CellContent.MagicTree:
                _font.DrawChar('T', px + 4, py + 4, new Color(0, 192, 0, 255));
                break;

            case CellContent.ShadowWood:
                _font.DrawChar('T', px + 4, py + 4, new Color(0, 128, 0, 255));
                break;

            case CellContent.MagicCastle:
                _font.DrawChar('C', px + 4, py + 2, new Color(255, 255, 0, 255));
                _font.DrawChar('C', px + 4, py + 8, new Color(255, 255, 0, 255));
                break;

            case CellContent.DarkCitadel:
                _font.DrawChar('D', px + 4, py + 2, new Color(192, 0, 192, 255));
                _font.DrawChar('D', px + 4, py + 8, new Color(192, 0, 192, 255));
                break;

            case CellContent.Wall:
                Raylib.DrawRectangle(px + 1, py + 1, 14, 14, DimGrey);
                break;

            case CellContent.DeadBody:
                _font.DrawChar('x', px + 4, py + 4, DimGrey);
                break;
        }
    }

    private void DrawSpriteOrFallback(int x, int y, string key, char fallback, Color colour)
    {
        if (_sprites.Has(key))
            _sprites.Draw(key, x, y);
        else
            _font.DrawChar(fallback, x + 4, y + 4, colour);
    }

    private void DrawStatus()
    {
        var wiz = _wizIdx < _game.Wizards.Count ? _game.Wizards[_wizIdx] : null;
        Color wizCol = wiz != null ? WizardColours[wiz.Id % 8] : DimWhite;

        if (_phase == Phase.SpellSelect && wiz is { IsHuman: true })
        {
            var avail = GetAvailableSpells(wiz);
            int total = avail.Count;
            string header = _scrollOff > 0 || total > 2
                ? $"{wiz.Name} (0=pass,Up/Dn)"
                : $"{wiz.Name} (0=pass)";
            _font.DrawString(header, 0, StatusY, wizCol);

            int maxShow = 2;
            for (int i = 0; i < maxShow && i + _scrollOff < avail.Count; i++)
            {
                int idx = i + _scrollOff;
                var sp = avail[idx];
                int ch = sp.GetEffectiveCastingChance(_game.WorldAlignment);
                string line = $"{i + 1}.{sp.Name} {ch * 10}%";
                if (line.Length > 32) line = line[..32];

                Color c = DimWhite;
                if (sp.AlignmentShift > 0) c = new Color(0, 192, 192, 255);
                if (sp.AlignmentShift < 0) c = new Color(192, 0, 192, 255);
                _font.DrawString(line, 0, StatusY + 8 + i * 8, c);
            }
        }
        else
        {
            if (wiz != null)
                _font.DrawString(wiz.Name, 0, StatusY, wizCol);

            if (_msg1.Length > 0)
                _font.DrawString(_msg1.Length > 32 ? _msg1[..32] : _msg1,
                    0, StatusY + 8, DimWhite);

            if (_msg2.Length > 0)
                _font.DrawString(_msg2.Length > 32 ? _msg2[..32] : _msg2,
                    0, StatusY + 16, DimGrey);
        }

        // Turn/alignment info
        _font.DrawString($"Turn {_game.TurnNumber} Align:{_game.WorldAlignment:+0;-0;0}",
            0, StatusY + 24, DimGrey);
    }

    // ── Border pattern data extracted from Z80 screen memory ────
    // Attributes: ink=cyan, paper=blue (bright). Each byte is one
    // 8-pixel row, MSB = leftmost. Patterns tile every 8 pixels.

    // Left edge column: dense on left, fading right (repeats every 4 rows)
    private static readonly byte[] BorderLeft =
        { 0xD0, 0xE4, 0xD0, 0xEA, 0xD0, 0xE4, 0xD0, 0xEA };

    // Right edge column: mirror of left (repeats every 4 rows)
    private static readonly byte[] BorderRight =
        { 0x57, 0x0B, 0x27, 0x0B, 0x57, 0x0B, 0x27, 0x0B };

    // Top edge row: solid at top, fading down (one 8-row cell)
    private static readonly byte[] BorderTop =
        { 0xFF, 0xFF, 0xAA, 0x55, 0x88, 0x22, 0x88, 0x00 };

    // Bottom edge row: mirror of top (fading up to solid at bottom)
    private static readonly byte[] BorderBottom =
        { 0x00, 0x88, 0x22, 0x88, 0x55, 0xAA, 0xFF, 0xFF };

    // Top-left corner: combines top and left gradients
    private static readonly byte[] BorderTopLeft =
        { 0xFF, 0xFF, 0xEA, 0xF5, 0xD0, 0xE4, 0xD0, 0xEA };

    // Top-right corner: combines top and right gradients
    private static readonly byte[] BorderTopRight =
        { 0xFF, 0xFF, 0xAF, 0x5B, 0x87, 0x2B, 0x87, 0x0B };

    private static readonly byte[] BorderBottomLeft =
    { 0xD0, 0xE1, 0xD4, 0xE1, 0xDA, 0xF5, 0xFF, 0xFF };

    private static readonly byte[] BorderBottomRight =
          { 0x57, 0x0B, 0x27, 0x0b, 0xAF, 0x57, 0xFF, 0xFF };

    private static readonly Color BorderInk = new(0, 0, 255, 255);   // bright cyan
    private static readonly Color BorderPaper = new(0, 0, 0, 255);   // blue

    /// <summary>
    /// Draw an 8px-wide vertical strip using a repeating 8-row bit pattern.
    /// </summary>
    private void DrawBorderStrip(int x, int y, int height, byte[] pattern)
    {
        for (int py = 0; py < height; py++)
        {
            byte bits = pattern[py % pattern.Length];
            for (int px = 0; px < 8; px++)
            {
                bool ink = (bits & (0x80 >> px)) != 0;
                Raylib.DrawPixel(x + px, y + py, ink ? BorderInk : BorderPaper);
            }
        }
    }

    /// <summary>
    /// Draw an 8px-tall horizontal strip using a repeating 8-column bit pattern.
    /// Each 8-pixel-wide tile uses the same pattern.
    /// </summary>
    private void DrawBorderHStrip(int x, int y, int width, byte[] pattern)
    {
        for (int row = 0; row < 8; row++)
        {
            byte bits = pattern[row];
            for (int px = 0; px < width; px++)
            {
                bool ink = (bits & (0x80 >> (px % 8))) != 0;
                Raylib.DrawPixel(x + px, y + row, ink ? BorderInk : BorderPaper);
            }
        }
    }

    /// <summary>
    /// Draw a single 8×8 corner cell from its pattern.
    /// </summary>
    private void DrawBorderCorner(int x, int y, byte[] pattern)
    {
        for (int row = 0; row < 8; row++)
        {
            byte bits = pattern[row];
            for (int px = 0; px < 8; px++)
            {
                bool ink = (bits & (0x80 >> px)) != 0;
                Raylib.DrawPixel(x + px, y + row, ink ? BorderInk : BorderPaper);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════

    private static List<Spell> GetAvailableSpells(Wizard w) =>
        w.Spells.Where(s => !s.IsUsed || s.Name == "Disbelieve").ToList();

    private static readonly Dictionary<string, int> ObjIds = new()
    {
        ["King Cobra"]=2,["Dire Wolf"]=3,["Goblin"]=4,["Crocodile"]=5,
        ["Faun"]=6,["Lion"]=7,["Elf"]=8,["Orc"]=9,["Bear"]=10,
        ["Gorilla"]=11,["Ogre"]=12,["Hydra"]=13,["Giant Rat"]=14,
        ["Giant"]=15,["Horse"]=16,["Unicorn"]=17,["Centaur"]=18,
        ["Pegasus"]=19,["Gryphon"]=20,["Manticore"]=21,["Bat"]=22,
        ["Green Dragon"]=23,["Red Dragon"]=24,["Golden Dragon"]=25,
        ["Harpy"]=26,["Eagle"]=27,["Vampire"]=28,["Ghost"]=29,
        ["Spectre"]=30,["Wraith"]=31,["Skeleton"]=32,["Zombie"]=33,
    };

    private static readonly Dictionary<int, int> Frames = new()
    {
        [2]=3,[3]=3,[4]=3,[5]=3,[6]=3,[7]=3,[8]=4,[9]=3,[10]=3,
        [11]=3,[12]=3,[13]=3,[14]=3,[15]=3,[16]=3,[17]=3,[18]=3,
        [19]=3,[20]=3,[21]=3,[22]=3,[23]=3,[24]=3,[25]=3,[26]=3,
        [27]=3,[28]=3,[29]=4,[30]=1,[31]=4,[32]=3,[33]=4,[34]=3,[35]=4,
    };

    private static int PingPong(int tick, int frameCount)
    {
        if (frameCount <= 1) return 0;
        int cycle = (frameCount - 1) * 2;    // 3 frames → cycle of 4
        int pos = tick % cycle;               // 0,1,2,3,0,1,2,3,...
        return pos < frameCount ? pos : cycle - pos;  // 0,1,2,1,0,1,2,1,...
    }

    private static int GetObjId(string name) =>
        ObjIds.GetValueOrDefault(name, 2);

    private static int GetFrames(int id) =>
        Frames.GetValueOrDefault(id, 1);
}
