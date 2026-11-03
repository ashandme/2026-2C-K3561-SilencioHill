using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.LevelUtils;

sealed class SkyboxSettings
{
    public SkyboxMode Mode { get; set; } = SkyboxMode.Gradient;
    public string TextureAsset { get; set; } = string.Empty;
    public Color Tint { get; set; } = Color.White;
    public float Brightness { get; set; } = 1f;
    public string EffectAsset { get; set; } = "Effects/Skybox";
}
