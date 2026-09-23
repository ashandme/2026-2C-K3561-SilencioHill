using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;
internal class Prop
{
    public Model Model { get; }
    public Effect Effect { get; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;
    public Vector3 Color { get; set; }
    private readonly Matrix[] _boneTransforms;

    private static readonly Random _random = new Random();

    // Reusable RasterizerState for wireframe (crea uno por problemas de performance si se crea uno por cada prop)!!!
    public static readonly RasterizerState WireframeRasterizer = new RasterizerState
    {
        FillMode = FillMode.WireFrame,
        CullMode = CullMode.None
    };

    public Prop(Model model, Effect effect, Vector3 position, Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
    {
        Model = model;
        Effect = effect;
        Position = position;
        Rotation = rotation ?? Vector3.Zero;
        Scale = scale ?? Vector3.One;
        Color = color ?? RandomColor();
        // NOTE: Do not assign mesh.MeshParts[].Effect here. Model is a shared resource
        // and assigning effects in the constructor will overwrite effects for other
        // instances that reuse the same Model. Effects (and textures) should be set
        // just before drawing so the shared shader instance can be reused.
        _boneTransforms = new Matrix[Model.Bones.Count];
        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
    }

    // Optional per-prop texture (set by caller). If null, shader should use its default.
    public Texture2D? Texture { get; set; }

    private static Vector3 RandomColor()
    {
        return new Vector3(
            (float)_random.NextDouble(),
            (float)_random.NextDouble(),
            (float)_random.NextDouble());
    }

    public Matrix GetWorldMatrix()
    {
        return Matrix.CreateScale(Scale) *
               Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
               Matrix.CreateTranslation(Position);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        var world = GetWorldMatrix();
        // Set effect parameters for this prop. Avoid cloning the effect per-prop; reuse
        // the shared effect and update its parameters before drawing.
        if (Effect != null)
        {
            Effect.Parameters["View"]?.SetValue(view);
            Effect.Parameters["Projection"]?.SetValue(projection);
            Effect.Parameters["DiffuseColor"]?.SetValue(Color);

            // If this prop has a texture, try common parameter names. ShaderHelper
            // already attempts names when preparing effects; here we set texture at
            // draw-time so multiple props can reuse the same effect instance.
            if (Texture != null)
            {
                Effect.Parameters["ModelTexture"]?.SetValue(Texture);
                Effect.Parameters["Texture"]?.SetValue(Texture);
                Effect.Parameters["DiffuseMap"]?.SetValue(Texture);
            }
        }

        foreach (var mesh in Model.Meshes)
        {
            var boneTransform = _boneTransforms[mesh.ParentBone.Index];

            // Before drawing, assign the shared effect instance to each mesh part so
            // the draw call uses the current parameters (world/view/proj/texture).
            if (Effect != null)
            {
                foreach (var part in mesh.MeshParts)
                {
                    part.Effect = Effect;
                }
                Effect.Parameters["World"]?.SetValue(boneTransform * world);
            }

            mesh.Draw();
        }
    }
    public void DrawWireframeNoState(Matrix view, Matrix projection)
    {
        var world = GetWorldMatrix();
        Effect.Parameters["View"]?.SetValue(view);
        Effect.Parameters["Projection"]?.SetValue(projection);
        Effect.Parameters["DiffuseColor"]?.SetValue(Color);

        foreach (var mesh in Model.Meshes)
        {
            var boneTransform = _boneTransforms[mesh.ParentBone.Index];
            Effect.Parameters["World"]?.SetValue(boneTransform * world);
            mesh.Draw();
        }
    }
    public void DrawWireframe(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
    {
        // This method maintained for compatibility but not ideal when called per-prop.
        var prevRaster = graphicsDevice.RasterizerState;
        var prevDepth = graphicsDevice.DepthStencilState;
        graphicsDevice.RasterizerState = WireframeRasterizer;
        DrawWireframeNoState(view, projection);
        graphicsDevice.RasterizerState = prevRaster;
        graphicsDevice.DepthStencilState = prevDepth;
    }
}