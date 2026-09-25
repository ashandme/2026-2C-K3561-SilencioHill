using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP
{
    internal class Map
    {
        // Expose props for external systems (e.g., interaction raycasts)
        public IReadOnlyList<Prop> GetProps() => _props.AsReadOnly();

        // Remove a prop from the map (e.g., when picked up)
        public bool RemoveProp(Prop prop)
        {
            if (prop == null) return false;
            return _props.Remove(prop);
        }

        protected readonly List<Prop> _props = new();
        protected readonly Dictionary<string, Model> _loadedModels = new();
        protected readonly Dictionary<string, Effect> _loadedEffects = new();
        protected readonly Dictionary<string, Texture2D> _loadedTextures = new();
        protected readonly Random _random = new();

        // Add these fields inside the Map class (near other texture/asset fields)
        internal Microsoft.Xna.Framework.Graphics.Texture2D treeA;
        internal Microsoft.Xna.Framework.Graphics.Texture2D treeB;
        internal virtual void LoadContent(ContentManager content)
        {
            // Cargas los shaders y modelos UNA sola vez
            var shader = content.Load<Effect>(TGCGame.ContentFolderEffects + "BasicShader");
            var basicTextureShader = content.Load<Effect>(TGCGame.ContentFolderEffects + "BasicTexture");

            var treeModel = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/tree-pine-small");
            var treeModelLarge = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/tree-pine-large");
            var grass = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/grass");
            var truckflat = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/truck-flat");
            var bench = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-bench");
            var lightSingle = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-light-single");
            var dumpster = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-dumpster-closed");
            var wallFlat = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-a-flat");
            var wallWindow = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-a-window");
            var wallFence = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-fence");

            // Load grass texture and tree textures
            Texture2D grassTexture = content.Load<Texture2D>(TGCGame.ContentFolderTextures + "grass");
            Texture2D treeATexture = content.Load<Texture2D>(TGCGame.ContentFolderTextures + "treeA");
            Texture2D treeBTexture = content.Load<Texture2D>(TGCGame.ContentFolderTextures + "treeB");
            Texture2D wallTexture = content.Load<Texture2D>(TGCGame.ContentFolderTextures + "wall");
            var baseEffect = basicTextureShader ?? shader;

            Effect? wallEffect = ShaderHelper.PrepareEffectWithTexture(basicTextureShader ?? shader, wallTexture);
            Effect? grassEffect = ShaderHelper.PrepareEffectWithTexture(basicTextureShader ?? shader, grassTexture);
            Effect? treeAEffect = ShaderHelper.PrepareEffectWithTexture(basicTextureShader ?? shader, treeATexture);
            Effect? treeBEffect = ShaderHelper.PrepareEffectWithTexture(basicTextureShader ?? shader, treeBTexture);

            int gridSize = 16;
            float grassSize = 100f;

            MapBuilder.BuildGrassGrid(_props, grass, grassEffect, grassTexture, gridSize, grassSize);
            MapBuilder.BuildFences(_props, wallFence, wallEffect, gridSize, grassSize);

            int treeCount = 30;
            const float worldMax = 1500f;
            const float edgeBandMin = 0f;
            const float edgeBandMax = 20f;
            const float edgeBandHighMin = 1480f;
            const float edgeBandHighMax = 1500f;

            MapBuilder.PlaceRandomTrees(
                _props,
                _random,
                treeModel,
                treeModelLarge,
                treeCount,
                worldMax,
                edgeBandMin,
                edgeBandMax,
                edgeBandHighMin,
                edgeBandHighMax,
                treeAEffect,
                treeBEffect);

            // Add a battery interactive pickup to the world
            try
            {
                Model batteryModel = null;
                try
                {
                    batteryModel = content.Load<Model>(TGCGame.ContentFolder3D + "Assets/battery");
                }
                catch (Exception)
                {
                    batteryModel = null;
                }

                Texture2D metalTexture = null;
                try
                {
                    metalTexture = content.Load<Texture2D>(TGCGame.ContentFolderTextures + "metal");
                }
                catch (Exception)
                {
                    metalTexture = null;
                }

                if (batteryModel != null)
                {
                    // Use BasicTexture shader if available, otherwise fallback to base shader
                    var batteryEffect = ShaderHelper.PrepareEffectWithTexture(basicTextureShader ?? shader, metalTexture);

                    // Place the battery somewhere in the scene (adjust coordinates as needed)
                    var batteryPos = new Vector3(30f, 0f, 30f);
                    var battery = new BatteryProp(batteryModel, batteryEffect, batteryPos, "battery_01", addSeconds: 60f);
                    _props.Add(battery);
                }
            }
            catch { }
        }

        public void Draw(Matrix view, Matrix projection)
        {
            // iterar sobre cada prop y dibujarlo
            foreach (var prop in _props)
            {
                prop.Draw(view, projection);
            }
        }

        public void DrawWireframe(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
            //graphicsDevice.BlendState = BlendState.AlphaBlend;
            //graphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None };
            // Save and set once per map draw (avoid per-prop state churn)
            var prevRaster = graphicsDevice.RasterizerState;
            var prevDepth = graphicsDevice.DepthStencilState;
            graphicsDevice.RasterizerState = Prop.WireframeRasterizer;

            foreach (var prop in _props)
            {
                // Use the no-state draw which sets only shader params and issues draw calls
                prop.DrawWireframeNoState(view, projection);
            }

            // Restore previous states once
            graphicsDevice.RasterizerState = prevRaster;
            graphicsDevice.DepthStencilState = prevDepth;
        }
        public void UnloadContent()
        {
            // Clear props and allow GC to collect models/effects if no other references exist.
            try
            {
                _props.Clear();
                _loadedModels.Clear();
                _loadedEffects.Clear();
                _loadedTextures.Clear();
            }
            catch { }
        }
    }
}
