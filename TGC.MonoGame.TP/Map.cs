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

        public void LoadContent(ContentManager content)
        {
            // Cargas los shaders y modelos UNA sola vez
            var shader = content.Load<Effect>(TGCGame.ContentFolderEffects + "BasicShader");
            var treeModel   = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/tree-pine-small");
            var grass   = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/grass");
            var truckflat   = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/truck-flat");
            var bench       = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-bench");
            var lightSingle = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-light-single");
            var dumpster    = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-dumpster-closed");
            var wallFlat    = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-a-flat");
            var wallWindow  = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/wall-a-window");
            // Instancias los props
            /*_props.Add(new Prop(treeModel, shader, new Vector3(0, 0, 140)));
            _props.Add(new Prop(treeModel, shader, new Vector3(125, 0, 15), scale: new Vector3(1.5f)));
            _props.Add(new Prop(truckflat, shader, new Vector3(-100, 0, 0), rotation: new Vector3(0, MathHelper.ToRadians(90), 0)));
            _props.Add(new Prop(bench, shader, new Vector3(20, 0, 20)));
            _props.Add(new Prop(lightSingle, shader, new Vector3(30, 0, 15)));
            _props.Add(new Prop(dumpster, shader, new Vector3(-10, 0, 5)));
            _props.Add(new Prop(wallFlat, shader, new Vector3(50, 0, 50)));
            _props.Add(new Prop(wallWindow, shader, new Vector3(60, 0, 50)));*/
            
            float grassSize = 100f;
            
            for (int x = 0; x < 8; x++)
            {
                for (int z = 0; z < 8; z++)
                {
                    _props.Add(new Prop(grass, shader,
                        new Vector3(x * grassSize, 0, z * grassSize)));
                }
            }

        }
        public void LoadFromJson(string filePath, ContentManager content)
        {
            _props.Clear();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"no existe o no tiene permisos: {filePath}");

            var jsonText = File.ReadAllText(filePath);
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

                // Si no tiene color en el JSON, generamos uno aleatorio
                var color = item.Color ?? new Vector3(_random.NextSingle(), _random.NextSingle(), _random.NextSingle());

                // Se pasan directo Position, Rotation y Scale sin ParseVector3
                _props.Add(new Prop(model, effect, item.Position, item.Rotation, item.Scale, color));
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
    }
}
