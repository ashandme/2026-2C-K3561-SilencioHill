using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    internal class Map
    {
        protected readonly List<Prop> _props = new();
        protected readonly Dictionary<string, Model> _loadedModels = new();
        protected readonly Dictionary<string, Effect> _loadedEffects = new();
        protected readonly Random _random = new();

        internal void LoadContent(ContentManager content)
        {
            // Cargas los shaders y modelos UNA sola vez
            var shader = content.Load<Effect>(TGCGame.ContentFolderEffects + "BasicShader");
            var treeModel   = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/tree-pine-small");
            var treeModelLarge = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/tree-pine-large");
            var grass   = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/grass");
            var truckflat   = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/truck-flat");
            var bench       = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-bench");
            var lightSingle = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-light-single");
            var dumpster    = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-dumpster-closed");
            var wallFlat    = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-a-flat");
            var wallWindow  = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-a-window");
            var wallFence   = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-fence");

            // Grid de 16x16
            int gridSize = 16;
            float grassSize = 100f;

            // Populate grass tiles
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    _props.Add(new Prop(grass, shader,
                        new Vector3(x * grassSize, 0, z * grassSize)));
                }
            }
            float half = grassSize / 2f;
            float max = (gridSize - 1) * grassSize;

            for (int x = 0; x < gridSize; x++)
            {
                var posTop = new Vector3(x * grassSize, 0, -half);
                var posBottom = new Vector3(x * grassSize, 0, max + half);

                _props.Add(new Prop(wallFence, shader, posTop));
                _props.Add(new Prop(wallFence, shader, posBottom));
            }

            var rot90 = new Vector3(0, MathHelper.ToRadians(90), 0);
            for (int z = 0; z < gridSize; z++)
            {
                var posLeft = new Vector3(-half, 0, z * grassSize);
                var posRight = new Vector3(max + half, 0, z * grassSize);

                _props.Add(new Prop(wallFence, shader, posLeft, rotation: rot90));
                _props.Add(new Prop(wallFence, shader, posRight, rotation: rot90));
            }

            // Añadir árboles pine (small y large) de forma aleatoria
            int treeCount = 30;
            const float worldMax = 1500f;
            const float edgeBandMin = 0f;
            const float edgeBandMax = 20f;
            const float edgeBandHighMin = 1480f;
            const float edgeBandHighMax = 1500f;

            for (int i = 0; i < treeCount; i++)
            {
                var useLarge = _random.Next(0, 2) == 0;
                var model = useLarge ? treeModelLarge : treeModel;

                bool xIsConstrained = _random.Next(0, 2) == 0;

                float posX;
                float posZ;

                if (xIsConstrained)
                {
                    bool xUseHighEdge = _random.Next(0, 2) == 0;
                    posX = xUseHighEdge
                        ? edgeBandHighMin + _random.NextSingle() * (edgeBandHighMax - edgeBandHighMin) // 1480 .. 1500
                        : edgeBandMin + _random.NextSingle() * (edgeBandMax - edgeBandMin);            // 0 .. 20

                    posZ = _random.NextSingle() * worldMax; // 0 .. 1500
                }
                else
                {
                    bool zUseHighEdge = _random.Next(0, 2) == 0;
                    posZ = zUseHighEdge
                        ? edgeBandHighMin + _random.NextSingle() * (edgeBandHighMax - edgeBandHighMin) // 1480 .. 1500
                        : edgeBandMin + _random.NextSingle() * (edgeBandMax - edgeBandMin);            // 0 .. 20

                    posX = _random.NextSingle() * worldMax; // 0 .. 1500
                }

                var position = new Vector3(posX, 0f, posZ);

                float rotY = _random.NextSingle() * MathHelper.TwoPi;
                var rotation = new Vector3(0f, rotY, 0f);

                float r = _random.NextSingle() * 0.1f;
                float g = 0.5f + _random.NextSingle() * 0.5f; // 0.5 .. 1.0
                float b = _random.NextSingle() * 0.1f;
                var color = new Vector3(r, g, b);

                var scale = Vector3.One;

                _props.Add(new Prop(model, shader, position, rotation, scale, color));
            }
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
    }
}
