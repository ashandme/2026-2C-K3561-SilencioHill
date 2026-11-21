using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.LevelUtils;

// Per-level Blinn-Phong material coefficients. Applied to SceneLighting when the level loads
// and pushed to every Blinn-Phong effect each draw.
sealed class BlinnPhongSettings
{
    public float KAmbient { get; set; } = 1f;
    public float KDiffuse { get; set; } = 1f;
    public float KSpecular { get; set; } = 0.35f;
    public float Shininess { get; set; } = 16f;

    public Vector3? GlobalLightDirection { get; set; }
    public Vector3 GlobalLightColor { get; set; } = new Vector3(0.35f, 0.42f, 0.4f);

    public void Apply()
    {
        SceneLighting.MoonEnabled = GlobalLightDirection.HasValue;
        if (GlobalLightDirection.HasValue)
        {
            SceneLighting.MoonDirection = Vector3.Normalize(-GlobalLightDirection.Value);
            SceneLighting.MoonColor = GlobalLightColor;
        }
        SceneLighting.KAmbient = KAmbient;
        SceneLighting.KDiffuse = KDiffuse;
        SceneLighting.KSpecular = KSpecular;
        SceneLighting.Shininess = Shininess;
    }
}
