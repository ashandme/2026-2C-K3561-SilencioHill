using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

/* Para manejar la deserialización de JSON
 * definimos una clase que represente la estructura del archivo JSON */
public class MapData
{
    public List<PropData> Props { get; set; } = new();
}

public class PropData
{
    public string ModelPath { get; set; } = string.Empty;
    public string EffectPath { get; set; } = string.Empty;
    public float[]? Position { get; set; }
    public float[]? Rotation { get; set; }
    public float[]? Scale { get; set; }
    public float[]? Color { get; set; }

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
}