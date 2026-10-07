using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP.Collisions;

internal sealed class CollisionHandler
{
    private readonly List<Collision> _collisions = new();

    public float PlayerRadius { get; set; } = 4f;

    public void SetProps(IEnumerable<Prop> props)
    {
        _collisions.Clear();
        foreach (var prop in props)
        {
            if (prop.Collision.Shapes.Count > 0)
                _collisions.Add(prop.Collision);
        }
    }

    public Vector3 ResolveMovement(Vector3 currentPosition, Vector3 desiredPosition)
    {
        var position = currentPosition;

        // Resolve one axis at a time. This is cheap and naturally allows sliding
        // along walls without requiring a physics engine.
        var xPosition = new Vector3(desiredPosition.X, position.Y, position.Z);
        if (!IsBlocked(xPosition))
            position.X = xPosition.X;

        var zPosition = new Vector3(position.X, desiredPosition.Y, desiredPosition.Z);
        if (!IsBlocked(zPosition))
            position.Z = zPosition.Z;

        return position;
    }

    private bool IsBlocked(Vector3 playerPosition)
    {
        foreach (var collision in _collisions)
        {
            foreach (var shape in collision.Shapes)
            {
                if (Intersects(shape, playerPosition, PlayerRadius))
                    return true;
            }
        }

        return false;
    }

    private static bool Intersects(CollisionShape shape, Vector3 center, float radius)
    {
        return shape.Type switch
        {
            CollisionShapeType.Sphere =>
                Vector3.DistanceSquared(center, shape.Sphere.Center) <=
                MathF.Pow(shape.Sphere.Radius + radius, 2f),

            CollisionShapeType.Aabb =>
                DistanceSquaredToBox(center, shape.Box) <= radius * radius,

            CollisionShapeType.Plane =>
                MathF.Abs(shape.Plane.DotCoordinate(center)) <= radius,

            _ => false
        };
    }

    private static float DistanceSquaredToBox(Vector3 point, BoundingBox box)
    {
        var dx = MathF.Max(box.Min.X - point.X, 0f) + MathF.Max(point.X - box.Max.X, 0f);
        var dy = MathF.Max(box.Min.Y - point.Y, 0f) + MathF.Max(point.Y - box.Max.Y, 0f);
        var dz = MathF.Max(box.Min.Z - point.Z, 0f) + MathF.Max(point.Z - box.Max.Z, 0f);
        return dx * dx + dy * dy + dz * dz;
    }
}
