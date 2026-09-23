using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    // Conviene definir una clase específica para cargar mapas desde JSON
    internal class MapJson : Map
    {
        public int LoadFromJson(string filePath, ContentManager content)
        {
            _props.Clear();

            var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"No se encontró el archivo de configuración en: {fullPath}\n");
            }

            var jsonText = File.ReadAllText(fullPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mapData = JsonSerializer.Deserialize<MapData>(jsonText, options);

            if (mapData?.Props == null) return 0;

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

                // Load optional texture specified in JSON and cache it
                Texture2D? texture = null;
                if (!string.IsNullOrEmpty(item.TexturePath))
                {
                    if (!_loadedTextures.TryGetValue(item.TexturePath, out var cachedTex))
                    {
                        try
                        {
                            cachedTex = content.Load<Texture2D>(item.TexturePath);
                        }
                        catch (Exception)
                        {
                            cachedTex = null;
                        }

                        if (cachedTex != null)
                            _loadedTextures[item.TexturePath] = cachedTex;
                    }

                    texture = cachedTex;
                }

                var color = item.ColorVector ?? new Vector3(_random.NextSingle(), _random.NextSingle(), _random.NextSingle());
                var rotationRadians = DegreesToRadians(item.RotationVector);

                var p = new Prop(model, effect, item.PositionVector,
                    rotationRadians,
                    item.ScaleVector, color);
                if (texture != null) p.Texture = texture;
                _props.Add(p);
            }

            return _props.Count;
        }

        private static Vector3 DegreesToRadians(Vector3 degrees) =>
            degrees * (MathF.PI / 180f);
    }
}