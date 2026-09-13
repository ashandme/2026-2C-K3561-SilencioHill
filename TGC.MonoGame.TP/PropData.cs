using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

public class MapData
{
    public List<PropData> Props { get; set; } = new();
}

public class PropData
{
    public string ModelPath { get; set; } = string.Empty;
    public string EffectPath { get; set; } = string.Empty;

    // Optional arrays [x, y, z] so the JSON remains clean and readable
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.One;
    public Vector3? Color { get; set; };
}