using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.PropUtils;

/* Para manejar la deserialización de JSON
 * definimos una clase que represente la estructura del archivo JSON */
public class MapData
{
    public List<PropData> Props { get; set; } = new();
}

public class PropData
{
    public string Name { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public string EffectPath { get; set; } = string.Empty;
    // Optional texture path for the prop (e.g., tree billboard texture)
    public string TexturePath { get; set; } = string.Empty;
    public float[]? Position { get; set; }
    public float[]? Rotation { get; set; }
    public float[]? Scale { get; set; }
    public float[]? Color { get; set; }
    public float[]? HideOffset { get; set; }
    public float[]? TextureScale { get; set; }
    public string TextureAddressMode { get; set; } = "Wrap";
    public bool AlwaysVisible { get; set; }
    public BlinnPhongMaterialData? Material { get; set; }
    public List<CollisionShapeData> Collisions { get; set; } = new();
    public string InteractiveType { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = string.Empty;
    public float[]? TeleportPosition { get; set; }
    public float[]? TeleportRotation { get; set; }
    public float[]? TargetPosition { get; set; }
    public float AnimationDuration { get; set; } = 1f;

    [JsonIgnore]
    public Vector2 TextureScaleVector => TextureScale is { Length: >= 2 }
        ? new Vector2(TextureScale[0], TextureScale[1])
        : Vector2.One;

    public PropData CreateInstance(PropData instance)
    {
        return new PropData
        {
            Name = Name,
            ModelPath = ModelPath,
            EffectPath = EffectPath,
            TexturePath = TexturePath,
            Position = instance.Position,
            Rotation = instance.Rotation,
            Scale = instance.Scale,
            Color = Color,
            HideOffset = HideOffset,
            TextureScale = TextureScale,
            TextureAddressMode = TextureAddressMode,
            AlwaysVisible = AlwaysVisible,
            Material = Material,
            Collisions = Collisions,
            InteractiveType = InteractiveType,
            Id = string.IsNullOrWhiteSpace(instance.Id) ? Id : instance.Id,
            TargetLevel = TargetLevel,
            TeleportPosition = TeleportPosition,
            TeleportRotation = TeleportRotation,
            TargetPosition = TargetPosition,
            AnimationDuration = AnimationDuration
        };
    }

    // Propiedades calculadas (ignoradas por el parser JSON)
    [JsonIgnore]
    public Vector3 PositionVector => Position is { Length: >= 3 }
        ? new Vector3(Position[0], Position[1], Position[2])
        : Vector3.Zero;

    [JsonIgnore]
    public Vector3 RotationVector => Rotation is { Length: >= 3 }
        ? new Vector3(Rotation[0], Rotation[1], Rotation[2])
        : Vector3.Zero;

    [JsonIgnore]
    public Vector3 ScaleVector => Scale is { Length: >= 3 }
        ? new Vector3(Scale[0], Scale[1], Scale[2])
        : Vector3.One;

    [JsonIgnore]
    public Vector3? ColorVector => Color is { Length: >= 3 }
        ? new Vector3(Color[0], Color[1], Color[2])
        : null;

    [JsonIgnore]
    public Vector3 HideOffsetVector => HideOffset is { Length: >= 3 }
        ? new Vector3(HideOffset[0], HideOffset[1], HideOffset[2])
        : Vector3.Zero;

    [JsonIgnore]
    public Vector3 TeleportPositionVector => TeleportPosition is { Length: >= 3 }
        ? new Vector3(TeleportPosition[0], TeleportPosition[1], TeleportPosition[2])
        : Vector3.Zero;

    [JsonIgnore]
    public Vector3 TeleportRotationVector => TeleportRotation is { Length: >= 3 }
        ? new Vector3(TeleportRotation[0], TeleportRotation[1], TeleportRotation[2])
        : Vector3.Zero;

    [JsonIgnore]
    public Vector3 TargetPositionVector => TargetPosition is { Length: >= 3 }
        ? new Vector3(TargetPosition[0], TargetPosition[1], TargetPosition[2])
        : Vector3.Zero;
}