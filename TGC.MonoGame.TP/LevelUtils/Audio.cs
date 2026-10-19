using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace TGC.MonoGame.TP;

// Reproductor de sonidos global: Audio.Play("Door"), Audio.PlayMusic("IndoorAmbience")
internal static class Audio
{
    private const string Folder = "SFX/";
    private static readonly Dictionary<string, SoundEffect> Effects = new();
    private static ContentManager _content;
    private static string _currentSong;

    public static void Init(ContentManager content) => _content = content;

    public static void Play(string name, float volume = 1f)
    {
        if (!Effects.TryGetValue(name, out var effect))
        {
            effect = _content.Load<SoundEffect>(Folder + name);
            Effects[name] = effect;
        }
        effect.Play(volume, 0f, 0f);
    }

    // Música/ambiente en loop; si ya está sonando la misma, no la reinicia
    public static void PlayMusic(string name, float volume = 0.4f)
    {
        if (_currentSong == name) return;
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = volume;
        MediaPlayer.Play(_content.Load<Song>(Folder + name));
        _currentSong = name;
    }

    public static void StopMusic()
    {
        MediaPlayer.Stop();
        _currentSong = null;
    }
}