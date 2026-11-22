using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace TGC.MonoGame.TP;

// Sound playback plus the player-adjustable master volume (0-100), persisted between runs.
// Usage: SoundManager.Play("Door"), SoundManager.PlayMusic("IndoorAmbience"), SoundManager.Volume = 50.
internal static class SoundManager
{
    public const int MinVolume = 0;
    public const int MaxVolume = 100;

    private const string Folder = "SFX/";
    private static readonly Dictionary<string, SoundEffect> Effects = new();
    private static ContentManager _content;
    private static string _currentSong;
    private static float _musicBaseVolume = 0.4f;
    private static int _volume = MaxVolume;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SilencioHill", "volume.txt");

    // Master volume as a percentage (0-100). Applies immediately to the music that is playing.
    public static int Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, MinVolume, MaxVolume);
            ApplyMusicVolume();
            SaveVolume();
        }
    }

    private static float MasterGain => _volume / (float)MaxVolume;

    public static void Init(ContentManager content)
    {
        _content = content;
        LoadVolume();
    }

    public static void Play(string name, float volume = 1f)
    {
        if (!Effects.TryGetValue(name, out var effect))
        {
            effect = _content.Load<SoundEffect>(Folder + name);
            Effects[name] = effect;
        }
        effect.Play(Math.Clamp(volume, 0f, 1f) * MasterGain, 0f, 0f);
    }

    // Looping music/ambience; if the same song is already playing it is not restarted.
    public static void PlayMusic(string name, float volume = 0.4f)
    {
        if (_currentSong == name) return;
        _musicBaseVolume = volume;
        MediaPlayer.IsRepeating = true;
        ApplyMusicVolume();
        MediaPlayer.Play(_content.Load<Song>(Folder + name));
        _currentSong = name;
    }

    public static void StopMusic()
    {
        MediaPlayer.Stop();
        _currentSong = null;
    }

    private static void ApplyMusicVolume() => MediaPlayer.Volume = _musicBaseVolume * MasterGain;

    private static void LoadVolume()
    {
        try
        {
            if (File.Exists(SettingsPath) && int.TryParse(File.ReadAllText(SettingsPath), out var saved))
                _volume = Math.Clamp(saved, MinVolume, MaxVolume);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SoundManager: could not load volume: " + ex.Message); }
    }

    private static void SaveVolume()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, _volume.ToString());
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SoundManager: could not save volume: " + ex.Message); }
    }
}
