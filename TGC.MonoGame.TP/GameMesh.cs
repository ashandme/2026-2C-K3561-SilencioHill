using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP{
    internal class GameMesh
    {
        private EffectParameterCache? _paramCache;
        private readonly Matrix[] _boneTransforms;
        private readonly BoundingSphere _localBoundingSphere;
        private const float FrustumCullingSphereScale = 1.25f;
        private static readonly Dictionary<Model, BoundingSphere> BoundingSphereCache = new();
        private static readonly Dictionary<Effect, int> FogFrameByEffect = new();

        private static readonly Random _random = new Random();

        public static readonly RasterizerState WireframeRasterizer = new RasterizerState
        {
            FillMode = FillMode.WireFrame,
            CullMode = CullMode.None
        };

        public Model Model { get; }
        public PropMaterial Material { get; }
        public Effect Effect
        {
            get => Material.Effect;
            set => Material.Effect = value;
        }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;
        public Vector3 Color
        {
            get => Material.Color;
            set => Material.Color = value;
        }
        public Texture2D? Texture
        {
            get => Material.Texture;
            set => Material.Texture = value;
        }
        public Vector2 TextureScale
        {
            get => Material.TextureScale;
            set => Material.TextureScale = value;
        }
        public TextureAddressMode TextureAddressMode
        {
            get => Material.TextureAddressMode;
            set => Material.TextureAddressMode = value;
        }

        private static readonly SamplerState WrapSamplerState = new() { AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap };
        private static readonly SamplerState ClampSamplerState = new() { AddressU = TextureAddressMode.Clamp, AddressV = TextureAddressMode.Clamp };
        private static readonly SamplerState MirrorSamplerState = new() { AddressU = TextureAddressMode.Mirror, AddressV = TextureAddressMode.Mirror };

        public GameMesh(Model model, Effect effect, Vector3 position, Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
        {
            Model = model;
            Material = new PropMaterial(effect, color: color ?? Vector3.One);
            Position = position;
            Rotation = rotation ?? Vector3.Zero;
            Scale = scale ?? Vector3.One;
            Color = color ?? RandomColor();
            _boneTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
            if (!BoundingSphereCache.TryGetValue(Model, out _localBoundingSphere))
            {
                _localBoundingSphere = TGC.MonoGame.Samples.Collisions.BoundingVolumesExtensions.CreateSphereFrom(Model);
                BoundingSphereCache[Model] = _localBoundingSphere;
            }
        }

        public bool IsVisible(BoundingFrustum frustum)
        {
            var world = GetWorldMatrix();
            var localSphere = _localBoundingSphere;
            localSphere.Transform(ref world, out var worldSphere);
            worldSphere.Radius *= FrustumCullingSphereScale;
            return frustum.Intersects(worldSphere);
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
            Material.Apply();
            _paramCache ??= EffectParameterCache.Get(Effect);
            _paramCache.View?.SetValue(view);
            _paramCache.Projection?.SetValue(projection);
            _paramCache.DiffuseColor?.SetValue(Color);
            _paramCache.TextureScale?.SetValue(TextureScale);

            if (Texture != null)
            {
                _paramCache.ModelTexture?.SetValue(Texture);
                _paramCache.Texture?.SetValue(Texture);
                _paramCache.DiffuseMap?.SetValue(Texture);
                _paramCache.BaseTexture?.SetValue(Texture);
            }

            SetLightingParameters();
            SetTextureSamplerState();
            DrawMeshes(world, view, projection);
        }

        public void DrawShadow(Matrix viewProjection, Effect shadowEffect)
        {
            foreach (var mesh in Model.Meshes)
            {
                var worldBone = _boneTransforms[mesh.ParentBone.Index] * GetWorldMatrix();
                shadowEffect.Parameters["WorldViewProjection"]?.SetValue(worldBone * viewProjection);
                foreach (var part in mesh.MeshParts) part.Effect = shadowEffect;
                mesh.Draw();
            }
        }

        private void SetTextureSamplerState()
        {
            var samplerState = TextureAddressMode switch
            {
                TextureAddressMode.Clamp => ClampSamplerState,
                TextureAddressMode.Mirror => MirrorSamplerState,
                _ => WrapSamplerState
            };

            Effect.GraphicsDevice.SamplerStates[0] = samplerState;
            // Shadow map (s1) must not be filtered or wrapped.
            Effect.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
        }

        private void SetLightingParameters()
        {
            var eyePos = SceneLighting.EyePosition;
            Effect.Parameters["eyePosition"]?.SetValue(eyePos);
            Effect.Parameters["lightViewProjection"]?.SetValue(SceneLighting.LightViewProjection);
            Effect.Parameters["shadowsEnabled"]?.SetValue(SceneLighting.ShadowsEnabled ? 1f : 0f);
            Effect.Parameters["shadowMapSize"]?.SetValue(SceneLighting.ShadowMapSize);
            Effect.Parameters["shadowMap"]?.SetValue(SceneLighting.ShadowMap);
            if (!FogFrameByEffect.TryGetValue(Effect, out var fogFrame) || fogFrame != SceneLighting.RenderFrame)
            {
                Effect.Parameters["fogEnabled"]?.SetValue(SceneLighting.FogEnabled ? 1f : 0f);
                Effect.Parameters["fogColor"]?.SetValue(SceneLighting.FogColor);
                Effect.Parameters["fogStart"]?.SetValue(SceneLighting.FogStartDistance);
                Effect.Parameters["fogEnd"]?.SetValue(SceneLighting.FogEndDistance);
                Effect.Parameters["fogDensity"]?.SetValue(SceneLighting.FogDensity);
                FogFrameByEffect[Effect] = SceneLighting.RenderFrame;
            }
            _paramCache?.LightDirection?.SetValue(SceneLighting.LightDirection);
            _paramCache?.LightIsDirectional?.SetValue(SceneLighting.IsDirectional ? 1f : 0f);
            _paramCache?.LightIsSpotlight?.SetValue(SceneLighting.IsSpotlight ? 1f : 0f);
            _paramCache?.SpotInnerCosine?.SetValue(SceneLighting.SpotInnerCosine);
            _paramCache?.SpotOuterCosine?.SetValue(SceneLighting.SpotOuterCosine);
            _paramCache?.AttenuationConstant?.SetValue(SceneLighting.Attenuation.X);
            _paramCache?.AttenuationLinear?.SetValue(SceneLighting.Attenuation.Y);
            _paramCache?.AttenuationQuadratic?.SetValue(SceneLighting.Attenuation.Z);
            _paramCache?.UpwardLightScale?.SetValue(SceneLighting.UpwardLightScale);
            var lightColor = SceneLighting.LightColor;
            Effect.Parameters["ambientColor"]?.SetValue(new Vector3(SceneLighting.AmbientIntensity) * lightColor);
            Effect.Parameters["diffuseColor"]?.SetValue(lightColor * SceneLighting.DiffuseIntensity);
            Effect.Parameters["specularColor"]?.SetValue(lightColor * SceneLighting.DiffuseIntensity);
            Effect.Parameters["KAmbient"]?.SetValue(1f);
            Effect.Parameters["KDiffuse"]?.SetValue(1f);
            Effect.Parameters["KSpecular"]?.SetValue(0.35f);
            Effect.Parameters["shininess"]?.SetValue(16f);
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
                Effect.Parameters["lightDirection"]?.SetValue(SceneLighting.LightDirection);
                Effect.Parameters["lightIsDirectional"]?.SetValue(SceneLighting.IsDirectional ? 1f : 0f);
                Effect.Parameters["lightIsSpotlight"]?.SetValue(SceneLighting.IsSpotlight ? 1f : 0f);
                Effect.Parameters["spotInnerCosine"]?.SetValue(SceneLighting.SpotInnerCosine);
                Effect.Parameters["spotOuterCosine"]?.SetValue(SceneLighting.SpotOuterCosine);
                Effect.Parameters["attenuationConstant"]?.SetValue(SceneLighting.Attenuation.X);
                Effect.Parameters["attenuationLinear"]?.SetValue(SceneLighting.Attenuation.Y);
                Effect.Parameters["attenuationQuadratic"]?.SetValue(SceneLighting.Attenuation.Z);
                Effect.Parameters["upwardLightScale"]?.SetValue(SceneLighting.UpwardLightScale);
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
