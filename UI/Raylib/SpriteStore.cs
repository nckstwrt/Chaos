namespace Chaos.UI.RaylibUI;

using Raylib_cs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

/// <summary>
/// Loads 16×16 PNG sprites into Raylib Texture2D objects.
/// All textures use point filtering for crisp pixel scaling.
/// </summary>
public class SpriteStore
{
    private readonly Dictionary<string, Texture2D> _textures = new();

    public void LoadAll(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"Sprite directory not found: {directory}");
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*.png"))
        {
            string key = Path.GetFileNameWithoutExtension(file);
            if (key is "spectrum_font" or "manifest" or "all_creatures" or "all_wizards")
                continue;

            var tex = Raylib.LoadTexture(file);
            Raylib.SetTextureFilter(tex, TextureFilter.Point);
            _textures[key] = tex;
        }

        Console.WriteLine($"Loaded {_textures.Count} sprites.");
    }

    /// <summary>Draw a sprite at (x,y) with optional colour tint.</summary>
    public void Draw(string key, int x, int y, Color? tint = null)
    {
        if (_textures.TryGetValue(key, out var tex))
            Raylib.DrawTexture(tex, x, y, tint ?? Color.White);
    }

    public bool Has(string key) => _textures.ContainsKey(key);

    public void UnloadAll()
    {
        foreach (var tex in _textures.Values)
            Raylib.UnloadTexture(tex);
        _textures.Clear();
    }
}
