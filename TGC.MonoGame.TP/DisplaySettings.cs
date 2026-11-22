using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Selectable screen resolutions (only those that fit the monitor, plus its native one).
// Fullscreen is the default (non-native resolutions use a real display mode switch); it can be
// toggled to a window of the same size. The choices are persisted between runs.
internal static class DisplaySettings
{
    internal readonly struct Option
    {
        public string Label { get; }
        public int Width { get; }
        public int Height { get; }

        public Option(string label, int width, int height)
        {
            Label = label;
            Width = width;
            Height = height;
        }
    }

    private static readonly Option[] Candidates =
    {
        new("800x600", 800, 600),
        new("1024x768", 1024, 768),
        new("1280x720", 1280, 720),
        new("HD 1366x768", 1366, 768),
        new("Full HD 1920x1080", 1920, 1080)
    };

    private static readonly List<Option> Options = new();
    private static GraphicsDeviceManager _manager;
    private static int _nativeWidth;
    private static int _nativeHeight;
    private static int _index;
    private static bool _fullScreen = true;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SilencioHill", "display.txt");

    public static Option Current => Options[_index];
    public static bool IsFullScreen => _fullScreen;

    // Call from the Game constructor, before the graphics device exists.
    public static void Init(GraphicsDeviceManager manager)
    {
        _manager = manager;
        var native = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _nativeWidth = native.Width;
        _nativeHeight = native.Height;

        Options.Clear();
        var nativeIndex = -1;
        foreach (var candidate in Candidates)
        {
            if (candidate.Width > _nativeWidth || candidate.Height > _nativeHeight) continue;
            if (candidate.Width == _nativeWidth && candidate.Height == _nativeHeight) nativeIndex = Options.Count;
            Options.Add(candidate);
        }
        if (nativeIndex < 0)
        {
            nativeIndex = Options.Count;
            Options.Add(new Option($"Nativa {_nativeWidth}x{_nativeHeight}", _nativeWidth, _nativeHeight));
        }

        _index = nativeIndex;
        LoadSaved();
        Apply(applyChanges: false);
    }

    // Moves to the next (+1) or previous (-1) resolution and applies it immediately.
    public static void Step(int direction)
    {
        _index = (_index + direction + Options.Count) % Options.Count;
        Apply(applyChanges: true);
        Save();
    }

    // Switches between fullscreen and a window with the same resolution (safe during a match).
    public static void ToggleFullScreen()
    {
        _fullScreen = !_fullScreen;
        Apply(applyChanges: true);
        Save();
    }

    private static void Apply(bool applyChanges)
    {
        var option = Current;
        _manager.PreferredBackBufferWidth = option.Width;
        _manager.PreferredBackBufferHeight = option.Height;
        _manager.IsFullScreen = _fullScreen;
        // Native resolution: borderless fullscreen. Anything else: switch the display mode.
        _manager.HardwareModeSwitch = option.Width != _nativeWidth || option.Height != _nativeHeight;
        if (applyChanges) _manager.ApplyChanges();
    }

    // File format: "WIDTHxHEIGHT" optionally followed by " windowed".
    private static void LoadSaved()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var tokens = File.ReadAllText(SettingsPath).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            _fullScreen = !(tokens.Length > 1 && tokens[1] == "windowed");
            var parts = tokens[0].Split('x');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h)) return;
            for (var i = 0; i < Options.Count; i++)
            {
                if (Options[i].Width != w || Options[i].Height != h) continue;
                _index = i;
                return;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("DisplaySettings: could not load: " + ex.Message); }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, $"{Current.Width}x{Current.Height}" + (_fullScreen ? "" : " windowed"));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("DisplaySettings: could not save: " + ex.Message); }
    }
}
