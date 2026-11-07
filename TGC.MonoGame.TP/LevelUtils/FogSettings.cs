using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.LevelUtils;

sealed class FogSettings
{
    public Color Color { get; set; } = Color.White;
    public float StartDistance { get; set; } = 100f;
    public float EndDistance { get; set; } = 1000f;
    public float Density { get; set; } = 0.01f;
    public bool Enabled { get; set; } = true;
}
