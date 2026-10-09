using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.PropUtils;

public class CollisionShapeData
{
    public string Type { get; set; } = string.Empty;
    public float[]? Center { get; set; }
    public float[]? Min { get; set; }
    public float[]? Max { get; set; }
    public float[]? Point { get; set; }
    public float[]? Normal { get; set; }
    public float[]? Direction { get; set; }
    public float Radius { get; set; }
    public float Distance { get; set; }
    public float HalfAngleDegrees { get; set; }

    public Vector3 CenterVector => ToVector(Center, Vector3.Zero);
    public Vector3 MinVector => ToVector(Min, Vector3.Zero);
    public Vector3 MaxVector => ToVector(Max, Vector3.Zero);
    public Vector3 PointVector => ToVector(Point, Vector3.Zero);
    public Vector3 NormalVector => ToVector(Normal, Vector3.Up);
    public Vector3 DirectionVector => ToVector(Direction, Vector3.Forward);

    private static Vector3 ToVector(float[]? values, Vector3 fallback)
    {
        return values is { Length: >= 3 }
            ? new Vector3(values[0], values[1], values[2])
            : fallback;
    }
}
