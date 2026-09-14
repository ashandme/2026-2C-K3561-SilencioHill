using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    // MapJson inherits Map and provides JSON loading functionality
    internal class MapJson : Map
    {
        public void LoadFromJson(string filePath, ContentManager content)
        {
            // Clear previous props so reload replaces the content
            _props.Clear();

            var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"No se encontró el archivo de configuración en: {fullPath}\n");
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
                var rotationRadians = item.RotationVector * (MathF.PI / 180f);

                _props.Add(new Prop(model, effect, item.PositionVector,
                    rotationRadians,
                    item.ScaleVector, color));
            }
        }
    }
}