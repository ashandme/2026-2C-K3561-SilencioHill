using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP;

namespace TGC.MonoGame.TP.PropUtils;

internal sealed class PropMaterial
{
    public Effect Effect { get; set; }
    public Texture2D? Texture { get; set; }
    public Vector2 TextureScale { get; set; } = Vector2.One;
    public TextureAddressMode TextureAddressMode { get; set; } = TextureAddressMode.Wrap;
    public Vector3 Color { get; set; } = Vector3.One;
    public Vector3 AmbientColor { get; set; } = new(0.2f, 0.2f, 0.2f);
    public Vector3 DiffuseColor { get; set; } = new(0.7f, 0.7f, 0.7f);
    public Vector3 SpecularColor { get; set; } = Vector3.One;
    public float AmbientCoefficient { get; set; } = 0.7f;
    public float DiffuseCoefficient { get; set; } = 0.8f;
    public float SpecularCoefficient { get; set; } = 0.7f;
    public float Shininess { get; set; } = 16f;

    public PropMaterial(Effect effect, Texture2D? texture = null, Vector3? color = null)
    {
        Effect = effect;
        Texture = texture;
        Color = color ?? Vector3.One;
    }

    public void Apply()
    {
        ShaderHelper.ApplyBlinnPhongParameters(this);
    }
}
