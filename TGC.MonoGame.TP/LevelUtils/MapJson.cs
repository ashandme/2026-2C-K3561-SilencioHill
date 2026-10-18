using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP.LevelUtils
{
    // Conviene definir una clase específica para cargar mapas desde JSON
    internal class MapJson : Map
    {
        public string filePath { get; set; } = string.Empty;
        internal override void LoadContent(ContentManager content)
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

                var p = item.InteractiveType.Equals("door", StringComparison.OrdinalIgnoreCase)
                    ? new DoorProp(model, effect, item.PositionVector, string.IsNullOrWhiteSpace(item.Id) ? "door" : item.Id)
                    : new Prop(model, effect, item.PositionVector,
                    rotationRadians,
                    item.ScaleVector, color);
                if (p is DoorProp door)
                {
                    door.Rotation = rotationRadians;
                    door.Scale = item.ScaleVector;
                    door.Color = color;
                    door.TargetLevel = item.TargetLevel;
                    door.TeleportPosition = item.TeleportPositionVector;
                    door.TeleportRotation = DegreesToRadians(item.TeleportRotationVector);
                }
                if (texture != null) p.Texture = texture;
                AddCollisions(p, item);
                _props.Add(p);
            }
        }

        private static Vector3 DegreesToRadians(Vector3 degrees) =>
            degrees * (MathF.PI / 180f);

        private static void AddCollisions(Prop prop, PropData data)
        {
            var scale = prop.Scale;
            var position = prop.Position;
            var radiusScale = MathF.Max(MathF.Abs(scale.X), MathF.Max(MathF.Abs(scale.Y), MathF.Abs(scale.Z)));

            foreach (var collision in data.Collisions)
            {
                switch (collision.Type.Trim().ToLowerInvariant())
                {
                    case "sphere":
                        prop.Colliders.Add(new SphereCollisionShape(
                            position + collision.CenterVector * scale,
                            collision.Radius * radiusScale));
                        break;

                    case "aabb":
                        prop.Colliders.Add(CreateRotatedAabb(collision, position, prop.Rotation, scale));
                        break;

                    case "plane":
                        prop.Colliders.Add(new PlaneCollisionShape(
                            position + collision.PointVector * scale,
                            collision.NormalVector));
                        break;

                    case "cone":
                        prop.Colliders.Add(new ConeCollisionShape(
                            position + collision.PointVector * scale,
                            collision.DirectionVector,
                            collision.Distance * radiusScale,
                            collision.HalfAngleDegrees));
                        break;
                }
            }
        }

        private static CollisionShape CreateRotatedAabb(
            CollisionShapeData data,
            Vector3 position,
            Vector3 rotation,
            Vector3 scale)
        {
            var min = data.MinVector;
            var max = data.MaxVector;
            var localCenter = (min + max) * 0.5f;
            var localExtents = (max - min) * 0.5f;
            var orientation = Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
            var center = position + Vector3.Transform(localCenter * scale, orientation);
            var extents = new Vector3(
                MathF.Abs(localExtents.X * scale.X),
                MathF.Abs(localExtents.Y * scale.Y),
                MathF.Abs(localExtents.Z * scale.Z));

            return new OrientedBoxCollisionShape(center, extents, orientation);
        }
    }
}