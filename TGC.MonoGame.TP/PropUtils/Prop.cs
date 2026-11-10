using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP.PropUtils;

internal class Prop
{
    public GameMesh Mesh { get; }
    public List<CollisionShape> Colliders { get; } = new();

    public Model Model => Mesh.Model;
    public Effect Effect => Mesh.Effect;
    public Vector3 Position
    {
        get => Mesh.Position;
        set => Mesh.Position = value;
    }
    public Vector3 Rotation
    {
        get => Mesh.Rotation;
        set => Mesh.Rotation = value;
    }
    public Vector3 Scale
    {
        get => Mesh.Scale;
        set => Mesh.Scale = value;
    }
    public Vector3 Color
    {
        get => Mesh.Color;
        set => Mesh.Color = value;
    }
    public bool AlwaysVisible { get; set; }
    public Vector2 TextureScale
    {
        get => Mesh.TextureScale;
        set => Mesh.TextureScale = value;
    }
    public TextureAddressMode TextureAddressMode
    {
        get => Mesh.TextureAddressMode;
        set => Mesh.TextureAddressMode = value;
    }
    public BlinnPhongMaterialData? BlinnPhong
    {
        get => Mesh.Material.BlinnPhong;
        set => Mesh.Material.BlinnPhong = value;
    }
    public Texture2D? Texture
    {
        get => Mesh.Texture;
        set => Mesh.Texture = value;
    }

    public Prop(Model model, Effect effect, Vector3 position, Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
    {
        Mesh = new GameMesh(model, effect, position, rotation, scale, color);
    }

    public Matrix GetWorldMatrix() => Mesh.GetWorldMatrix();

    public bool IsVisible(BoundingFrustum frustum) => Mesh.IsVisible(frustum);

    public bool IntersectsRay(Ray ray, out float distance)
    {
        return Mesh.IntersectsRay(ray, out distance);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        Mesh.Draw(view, projection);
    }

    public void DrawShadow(Matrix viewProjection, Effect shadowEffect)
    {
        Mesh.DrawShadow(viewProjection, shadowEffect);
    }

    public void DrawWireframeNoState(Matrix view, Matrix projection)
    {
        Mesh.DrawWireframeNoState(view, projection);
    }

    public void DrawWireframe(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
    {
        Mesh.DrawWireframe(graphicsDevice, view, projection);
    }

    public static RasterizerState WireframeRasterizer => GameMesh.WireframeRasterizer;
}