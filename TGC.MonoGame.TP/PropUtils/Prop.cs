using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP.PropUtils;

internal class Prop
{
    // Cache of effect parameters for the shared Effect instance (optional)
    private EffectParameterCache? _paramCache;
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

    // Ray intersection helper: transforms model bounding sphere to world and tests ray intersection.
    public bool IntersectsRay(Ray ray, out float distance)
    {
        distance = 0f;
        if (Model == null) return false;

        // Create a bounding sphere from the model (in model space)
        var sphere = TGC.MonoGame.Samples.Collisions.BoundingVolumesExtensions.CreateSphereFrom(Model);

        var world = GetWorldMatrix();
        // Transform center by world matrix
        var center = Vector3.Transform(sphere.Center, world);

        // Approximate world scale by taking column vector lengths
        var scaleX = new Vector3(world.M11, world.M12, world.M13).Length();
        var scaleY = new Vector3(world.M21, world.M22, world.M23).Length();
        var scaleZ = new Vector3(world.M31, world.M32, world.M33).Length();
        var scale = Math.Max(Math.Max(scaleX, scaleY), scaleZ);
        var radius = sphere.Radius * scale;

        var worldSphere = new BoundingSphere(center, radius);

        var t = ray.Intersects(worldSphere);
        if (t.HasValue)
        {
            distance = t.Value;
            return true;
        }

        return false;
    }

    public void Draw(Matrix view, Matrix projection)
    {
        var world = GetWorldMatrix();
        // Initialize parameter cache lazily
        if (_paramCache == null && Effect != null)
        {
            _paramCache = EffectParameterCache.Get(Effect);
        }
        // Set effect parameters for this prop. Avoid cloning the effect per-prop; reuse
        // the shared effect and update its parameters before drawing.
        if (Effect != null)
        {
            // Use cached parameters when available
            if (_paramCache != null)
            {
                _paramCache.View?.SetValue(view);
                _paramCache.Projection?.SetValue(projection);
                _paramCache.DiffuseColor?.SetValue(Color);
            }
            else
            {
                Effect.Parameters["View"]?.SetValue(view);
                Effect.Parameters["Projection"]?.SetValue(projection);
                Effect.Parameters["DiffuseColor"]?.SetValue(Color);
            }

            // If this prop has a texture, try common parameter names. ShaderHelper
            // already attempts names when preparing effects; here we set texture at
            // draw-time so multiple props can reuse the same effect instance.
            if (Texture != null)
            {
                if (_paramCache != null)
                {
                    _paramCache.ModelTexture?.SetValue(Texture);
                    _paramCache.Texture?.SetValue(Texture);
                    _paramCache.DiffuseMap?.SetValue(Texture);
                    _paramCache.BaseTexture?.SetValue(Texture);
                }
                else
                {
                    Effect.Parameters["ModelTexture"]?.SetValue(Texture);
                    Effect.Parameters["Texture"]?.SetValue(Texture);
                    Effect.Parameters["DiffuseMap"]?.SetValue(Texture);
                    Effect.Parameters["baseTexture"]?.SetValue(Texture);
                }
            }

            // Set dynamic per-frame parameters used by Blinn-Phong shader if present
            try
            {
                // Use precomputed eye position to avoid per-prop matrix inversion
                var eyePos = SceneLighting.EyePosition;
                Effect.Parameters["eyePosition"]?.SetValue(eyePos);

                // Prefer scene-level lighting. The shader now supports multiple lights, so set light arrays.
                var lightCountParam = Effect.Parameters["lightCount"];
                if (lightCountParam != null)
                {
                    try
                    {
                        if (SceneLighting.FlashlightEnabled)
                        {
                            // Fill first light slot with flashlight info
                            _paramCache?.LightCount?.SetValue(1);
                            _paramCache?.LightAmbient0?.SetValue(new Vector3(0.02f, 0.02f, 0.02f));
                            _paramCache?.LightDiffuse0?.SetValue(SceneLighting.LightColor);
                            _paramCache?.LightSpecular0?.SetValue(SceneLighting.LightColor);
                            _paramCache?.LightPosition0?.SetValue(SceneLighting.LightPosition);
                        }
                        else
                        {
                            // No scene lights: set a default single directional/point above camera
                            var lightPos = eyePos + new Vector3(0, 50f, 50f);
                            _paramCache?.LightCount?.SetValue(1);
                            _paramCache?.LightAmbient0?.SetValue(new Vector3(0.05f, 0.05f, 0.05f));
                            _paramCache?.LightDiffuse0?.SetValue(new Vector3(1f, 1f, 1f));
                            _paramCache?.LightSpecular0?.SetValue(new Vector3(1f, 1f, 1f));
                            _paramCache?.LightPosition0?.SetValue(lightPos);
                        }
                    }
                    catch { }
                }
                else
                {
                    // Fallback for older shader signatures
                    if (SceneLighting.FlashlightEnabled)
                    {
                        Effect.Parameters["lightPosition"]?.SetValue(SceneLighting.LightPosition);
                    }
                    else
                    {
                        var lightPos = eyePos + new Vector3(0, 50f, 50f);
                        Effect.Parameters["lightPosition"]?.SetValue(lightPos);
                    }
                }
            }
            catch
            {
                // In case view matrix inversion fails, skip setting eye/light
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

                var worldBone = boneTransform * world;
                Effect.Parameters["World"]?.SetValue(worldBone);

                // Also set WorldViewProjection and inverse-transpose world (normals)
                var wvp = worldBone * view * projection;
                Effect.Parameters["WorldViewProjection"]?.SetValue(wvp);

                // Inverse-transpose world for normal transformation
                try
                {
                    var invTrans = Matrix.Transpose(Matrix.Invert(worldBone));
                    Effect.Parameters["InverseTransposeWorld"]?.SetValue(invTrans);
                }
                catch
                {
                    // ignore if the matrix is not invertible
                }
            }

            mesh.Draw();
        }
    }
    public void DrawWireframeNoState(Matrix view, Matrix projection)
    {
        var world = GetWorldMatrix();
        if (Effect != null)
        {
            Effect.Parameters["View"]?.SetValue(view);
            Effect.Parameters["Projection"]?.SetValue(projection);
            Effect.Parameters["DiffuseColor"]?.SetValue(Color);

            try
            {
                var eyePos = Matrix.Invert(view).Translation;
                Effect.Parameters["eyePosition"]?.SetValue(new Vector3(eyePos.X, eyePos.Y, eyePos.Z));
                var lightPos = eyePos + new Vector3(0, 50f, 50f);
                Effect.Parameters["lightPosition"]?.SetValue(new Vector3(lightPos.X, lightPos.Y, lightPos.Z));
            }
            catch { }
        }

        foreach (var mesh in Model.Meshes)
        {
            var boneTransform = _boneTransforms[mesh.ParentBone.Index];
            var worldBone = boneTransform * world;
            Effect.Parameters["World"]?.SetValue(worldBone);
            Effect.Parameters["WorldViewProjection"]?.SetValue(worldBone * view * projection);
            try
            {
                var invTrans = Matrix.Transpose(Matrix.Invert(worldBone));
                Effect.Parameters["InverseTransposeWorld"]?.SetValue(invTrans);
            }
            catch { }
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