using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.Collisions;

internal enum CollisionShapeType
{
    Sphere,
    Aabb,
    Plane
}

internal readonly struct CollisionShape
{
    public CollisionShapeType Type { get; }
    public BoundingSphere Sphere { get; }
    public BoundingBox Box { get; }
    public Plane Plane { get; }

    private CollisionShape(CollisionShapeType type, BoundingSphere sphere, BoundingBox box, Plane plane)
    {
        Type = type;
        Sphere = sphere;
        Box = box;
        Plane = plane;
    }

    public static CollisionShape FromSphere(BoundingSphere sphere) =>
        new(CollisionShapeType.Sphere, sphere, default, default);

    public static CollisionShape FromAabb(BoundingBox box) =>
        new(CollisionShapeType.Aabb, default, box, default);

    public static CollisionShape FromPlane(Plane plane) =>
        new(CollisionShapeType.Plane, default, default, plane);
}

internal sealed class Collision
{
    private readonly List<CollisionShape> _shapes = new();

    public IReadOnlyList<CollisionShape> Shapes => _shapes;

    public Collision() { }

    public Collision(CollisionShape shape)
    {
        _shapes.Add(shape);
    }

    public Collision(IEnumerable<CollisionShape> shapes)
    {
        _shapes.AddRange(shapes);
    }

    public Collision AddSphere(Vector3 center, float radius)
    {
        _shapes.Add(CollisionShape.FromSphere(new BoundingSphere(center, radius)));
        return this;
    }

    public Collision AddAabb(Vector3 min, Vector3 max)
    {
        _shapes.Add(CollisionShape.FromAabb(new BoundingBox(min, max)));
        return this;
    }

    public Collision AddPlane(Vector3 point, Vector3 normal)
    {
        if (normal.LengthSquared() < float.Epsilon)
            throw new ArgumentException("A collision plane requires a non-zero normal.", nameof(normal));

        _shapes.Add(CollisionShape.FromPlane(new Plane(point, normal)));
        return this;
    }

    public Collision Add(CollisionShape shape)
    {
        _shapes.Add(shape);
        return this;
    }
}
