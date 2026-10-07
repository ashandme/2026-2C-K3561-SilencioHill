using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP
{
    internal class GameMesh
    {
        private EffectParameterCache? _paramCache;
        private readonly Matrix[] _boneTransforms;

        private static readonly Random _random = new Random();

        public static readonly RasterizerState WireframeRasterizer = new RasterizerState
        {
            FillMode = FillMode.WireFrame,
            CullMode = CullMode.None
        };

        public Model Model { get; }
        public Effect Effect { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;
        public Vector3 Color { get; set; }
        public Texture2D? Texture { get; set; }

        public GameMesh(Model model, Effect effect, Vector3 position, Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
        {
            Model = model;
            Effect = effect;
            Position = position;
            Rotation = rotation ?? Vector3.Zero;
            Scale = scale ?? Vector3.One;
            Color = color ?? RandomColor();
            _boneTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        }

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

        public bool IntersectsRay(Ray ray, out float distance)
        {
            distance = 0f;
            var sphere = TGC.MonoGame.Samples.Collisions.BoundingVolumesExtensions.CreateSphereFrom(Model);
            var world = GetWorldMatrix();
            var center = Vector3.Transform(sphere.Center, world);
            var scaleX = new Vector3(world.M11, world.M12, world.M13).Length();
            var scaleY = new Vector3(world.M21, world.M22, world.M23).Length();
            var scaleZ = new Vector3(world.M31, world.M32, world.M33).Length();
            var scale = Math.Max(Math.Max(scaleX, scaleY), scaleZ);
            var t = ray.Intersects(new BoundingSphere(center, sphere.Radius * scale));

            if (!t.HasValue) return false;
            distance = t.Value;
            return true;
        }

        public void Draw(Matrix view, Matrix projection)
        {
            Draw(GetWorldMatrix(), view, projection);
        }

        public void Draw(Matrix world, Matrix view, Matrix projection)
        {
            _paramCache ??= EffectParameterCache.Get(Effect);
            _paramCache.View?.SetValue(view);
            _paramCache.Projection?.SetValue(projection);
            _paramCache.DiffuseColor?.SetValue(Color);

            if (Texture != null)
            {
                _paramCache.ModelTexture?.SetValue(Texture);
                _paramCache.Texture?.SetValue(Texture);
                _paramCache.DiffuseMap?.SetValue(Texture);
                _paramCache.BaseTexture?.SetValue(Texture);
            }

            SetLightingParameters();
            DrawMeshes(world, view, projection);
        }

        private void SetLightingParameters()
        {
            var eyePos = SceneLighting.EyePosition;
            Effect.Parameters["eyePosition"]?.SetValue(eyePos);
            if (Effect.Parameters["lightCount"] != null)
            {
                _paramCache?.LightCount?.SetValue(1);
                _paramCache?.LightAmbient0?.SetValue(SceneLighting.FlashlightEnabled
                    ? new Vector3(0.02f)
                    : new Vector3(0.05f));
                _paramCache?.LightDiffuse0?.SetValue(SceneLighting.LightColor);
                _paramCache?.LightSpecular0?.SetValue(SceneLighting.LightColor);
                _paramCache?.LightPosition0?.SetValue(SceneLighting.FlashlightEnabled
                    ? SceneLighting.LightPosition
                    : eyePos + new Vector3(0, 50f, 50f));
            }
            else
            {
                Effect.Parameters["lightPosition"]?.SetValue(SceneLighting.FlashlightEnabled
                    ? SceneLighting.LightPosition
                    : eyePos + new Vector3(0, 50f, 50f));
            }
        }

        private void DrawMeshes(Matrix world, Matrix view, Matrix projection)
        {
            foreach (var mesh in Model.Meshes)
            {
                var worldBone = _boneTransforms[mesh.ParentBone.Index] * world;
                foreach (var part in mesh.MeshParts) part.Effect = Effect;
                Effect.Parameters["World"]?.SetValue(worldBone);
                Effect.Parameters["WorldViewProjection"]?.SetValue(worldBone * view * projection);

                try
                {
                    Effect.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(worldBone)));
                }
                catch { }

                mesh.Draw();
            }
        }

        public void DrawWireframeNoState(Matrix view, Matrix projection)
        {
            var world = GetWorldMatrix();
            Effect.Parameters["View"]?.SetValue(view);
            Effect.Parameters["Projection"]?.SetValue(projection);
            Effect.Parameters["DiffuseColor"]?.SetValue(Color);

            try
            {
                var eyePos = Matrix.Invert(view).Translation;
                Effect.Parameters["eyePosition"]?.SetValue(eyePos);
                Effect.Parameters["lightPosition"]?.SetValue(eyePos + new Vector3(0, 50f, 50f));
            }
            catch { }

            DrawMeshes(world, view, projection);
        }

        public void DrawWireframe(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
            var previousRasterizer = graphicsDevice.RasterizerState;
            var previousDepth = graphicsDevice.DepthStencilState;
            graphicsDevice.RasterizerState = WireframeRasterizer;
            DrawWireframeNoState(view, projection);
            graphicsDevice.RasterizerState = previousRasterizer;
            graphicsDevice.DepthStencilState = previousDepth;
        }
    }
}
