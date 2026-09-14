using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BepuPhysics.Trees;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    internal class Map
    {
        private readonly List<Prop> _props = new();
        private readonly Dictionary<string, Model> _loadedModels = new();
        private readonly Dictionary<string, Effect> _loadedEffects = new();     
        private readonly Random _random = new();

        // PSEUDOCODE / PLAN:
        // 1. Load shared shader and models exactly once (unchanged).
        // 2. Create a grass grid and fence perimeter (unchanged).
        // 3. For tree placement (N trees):
        //    - Randomly choose a tree model (small or large).
        //    - Randomly decide WHICH axis will be forced to the "edge band" (X or Z).
        //      * The constrained axis will be placed either in [0..20] or [1480..1500] (randomly chosen).
        //      * The other axis will vary freely in the full range [0..1500].
        //    - Assign a random Y rotation, a slightly varied greenish color, and a default scale.
        //    - Add the Prop with computed position/rotation/scale/color.
        // 4. This guarantees an empty central area because at least one coordinate is near an edge.
        //
        // The implementation below replaces the previous logic where both coordinates could be restricted
        // simultaneously. Now exactly one coordinate is constrained to the edge band per tree.

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

            // Instancias los props (ejemplos comentados originales mantenidos)
            /*_props.Add(new Prop(treeModel, shader, new Vector3(0, 0, 140)));
            _props.Add(new Prop(treeModel, shader, new Vector3(125, 0, 15), scale: new Vector3(1.5f)));
            _props.Add(new Prop(truckflat, shader, new Vector3(-100, 0, 0), rotation: new Vector3(0, MathHelper.ToRadians(90), 0)));
            _props.Add(new Prop(truckflat, shader, new Vector3(-100, 0, 0), rotation: new Vector3(0, MathHelper.ToRadians(90), 0)));
            _props.Add(new Prop(bench, shader, new Vector3(20, 0, 20)));
            _props.Add(new Prop(lightSingle, shader, new Vector3(30, 0, 15)));
            _props.Add(new Prop(dumpster, shader, new Vector3(-10, 0, 5)));
            _props.Add(new Prop(wallFlat, shader, new Vector3(50, 0, 50)));
            _props.Add(new Prop(wallWindow, shader, new Vector3(60, 0, 50)));*/

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
        public void LoadFromJson(string filePath, ContentManager content)
        {
            //_props.Clear();
            var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"No se encontró el archivo de configuración en: {fullPath}\n"
                );
            }
            var jsonText = File.ReadAllText(fullPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mapData = JsonSerializer.Deserialize<MapData>(jsonText, options);

            if (mapData?.Props == null) return;

            foreach (var item in mapData.Props)
            {
                if (!_loadedModels.TryGetValue(item.ModelPath, out var model))
                {
                    model = content.Load<Model>(item.ModelPath);
                    _loadedModels[item.ModelPath] = model;
                }

                if (!_loadedEffects.TryGetValue(item.EffectPath, out var effect))
                {
                    effect = content.Load<Effect>(item.EffectPath);
                    _loadedEffects[item.EffectPath] = effect;
                }

                var color = item.ColorVector ?? new Vector3(_random.NextSingle(), _random.NextSingle(), _random.NextSingle());

                // Convert rotation from degrees (in JSON) to radians for internal use
                var rotationRadians = item.RotationVector * (MathF.PI / 180f);

                _props.Add(new Prop(model, effect, item.PositionVector,
                    rotationRadians,
                    item.ScaleVector, color));
            }
        }

        public void Draw(Matrix view, Matrix projection)
        {
            // iterar sobre cada prop y dibujarlo
            foreach (var prop in _props)
            {
                // Si cada prop tiene color propio o compartís uno genérico:
                prop.Draw(view, projection);
            }
        }

        // Nuevo: dibujar todo el map en modo wireframe
        public void DrawWireframe(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
            foreach (var prop in _props)
            {
                prop.DrawWireframe(graphicsDevice, view, projection);
            }
        }
    }
}
