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

                    // If this effect looks like the Blinn-Phong shader (or exposes
                    // ambientColor), prepare it with sensible defaults and optional
                    // texture so props using it will render correctly.
                    bool looksLikeBlinn = (item.EffectPath?.IndexOf("blinn", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                                          || effect.Parameters["ambientColor"] != null;

                    if (looksLikeBlinn)
                    {
                        // Prepare afterwards once texture is known (texture may be null)
                        // For now store the raw effect; we'll replace with prepared one below
                    }

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

                // If this effect is a BlinnPhong variant, create a prepared instance
                // that has defaults and the optional texture applied.
                if (effect != null && (effect.Parameters["ambientColor"] != null ||
                                       (item.EffectPath?.IndexOf("blinn", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0))
                {
                    effect = ShaderHelper.PrepareBlinnPhong(effect, texture);
                    // update cache so subsequent props reuse the prepared effect
                    _loadedEffects[item.EffectPath] = effect;
                }

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