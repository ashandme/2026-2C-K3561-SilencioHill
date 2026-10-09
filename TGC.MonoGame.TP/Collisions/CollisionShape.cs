using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using System;

namespace TGC.MonoGame.TP.Collisions;

internal abstract class CollisionShape
{
    public abstract bool Intersects(BoundingSphere sphere);
    public abstract bool Intersects(CollisionShape shape);
    public virtual void UpdateTransform(Vector3 position, Vector3 forward) { }
}

internal sealed class SphereCollisionShape : CollisionShape
{
    public BoundingSphere Sphere { get; private set; }
    public Vector3 LocalCenter { get; }

    public SphereCollisionShape(Vector3 center, float radius)
    {
        LocalCenter = center;
        Sphere = new BoundingSphere(center, radius);
    }

    public void SetCenter(Vector3 center)
    {
        Sphere = new BoundingSphere(center, Sphere.Radius);
    }

    public override void UpdateTransform(Vector3 position, Vector3 forward)
    {
        SetCenter(position + LocalCenter);
    }

    public override bool Intersects(BoundingSphere sphere)
    {
        var radius = Sphere.Radius + sphere.Radius;
        return Vector3.DistanceSquared(Sphere.Center, sphere.Center) <= radius * radius;
    }

    public override bool Intersects(CollisionShape shape) => shape switch
    {
        SphereCollisionShape sphere => Intersects(sphere.Sphere),
        AabbCollisionShape box => box.Intersects(Sphere),
        PlaneCollisionShape plane => plane.Intersects(Sphere),
        _ => false
    };
}

internal sealed class AabbCollisionShape : CollisionShape
{
    public BoundingBox Box { get; }

    public AabbCollisionShape(Vector3 min, Vector3 max)
    {
        Box = new BoundingBox(min, max);
    }

    public override bool Intersects(BoundingSphere sphere)
    {
        var closest = Vector3.Clamp(sphere.Center, Box.Min, Box.Max);
        return Vector3.DistanceSquared(closest, sphere.Center) <= sphere.Radius * sphere.Radius;
    }

    public override bool Intersects(CollisionShape shape) => shape switch
    {
        SphereCollisionShape sphere => Intersects(sphere.Sphere),
        AabbCollisionShape box => Box.Intersects(box.Box),
        PlaneCollisionShape plane => plane.Intersects(new BoundingSphere(
            (Box.Min + Box.Max) * 0.5f,
            (Box.Max - Box.Min).Length() * 0.5f)),
        _ => false
    };
}

internal sealed class PlaneCollisionShape : CollisionShape
{
    public Plane Plane { get; }

    public PlaneCollisionShape(Vector3 point, Vector3 normal)
    {
        if (normal.LengthSquared() < float.Epsilon)
            throw new ArgumentException("A collision plane requires a non-zero normal.", nameof(normal));

        Plane = new Plane(point, Vector3.Normalize(normal));
    }

    public override bool Intersects(BoundingSphere sphere)
    {
        return MathF.Abs(Plane.DotCoordinate(sphere.Center)) <= sphere.Radius;
    }

    public override bool Intersects(CollisionShape shape) => shape switch
    {
        SphereCollisionShape sphere => Intersects(sphere.Sphere),
        AabbCollisionShape box => box.Intersects(this),
        PlaneCollisionShape plane => Vector3.DistanceSquared(Plane.Normal, plane.Plane.Normal) < 0.0001f &&
                                     MathF.Abs(Plane.D - plane.Plane.D) <= 0.001f,
        _ => false
    };
}

internal sealed class ConeCollisionShape : CollisionShape
{
    public Vector3 Origin { get; private set; }
    public Vector3 Direction { get; private set; }
    public float Distance { get; }
    public float HalfAngleDegrees { get; }
    private readonly bool _invertDirection;

    public ConeCollisionShape(Vector3 origin, Vector3 direction, float distance, float halfAngleDegrees, bool invertDirection = false)
    {
        Origin = origin;
        Direction = direction;
        Distance = distance;
        HalfAngleDegrees = halfAngleDegrees;
        _invertDirection = invertDirection;
    }

    public void Update(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
    }

    public override void UpdateTransform(Vector3 position, Vector3 forward)
    {
        Update(position, _invertDirection ? -forward : forward);
    }

    public override bool Intersects(BoundingSphere sphere)
    {
        var toTarget = sphere.Center - Origin;
        var maxDistance = Distance + sphere.Radius;
        if (toTarget.LengthSquared() > maxDistance * maxDistance)
            return false;

        var flatDirection = new Vector3(Direction.X, 0f, Direction.Z);
        var flatTarget = new Vector3(toTarget.X, 0f, toTarget.Z);
        if (flatDirection.LengthSquared() < 0.0001f || flatTarget.LengthSquared() < 0.0001f)
            return true;

        flatDirection.Normalize();
        flatTarget.Normalize();
        var angle = MathHelper.ToDegrees(MathF.Acos(
            MathHelper.Clamp(Vector3.Dot(flatDirection, flatTarget), -1f, 1f)));
        return angle <= HalfAngleDegrees;
    }

    public override bool Intersects(CollisionShape shape) => shape switch
    {
        SphereCollisionShape sphere => Intersects(sphere.Sphere),
        _ => false
    };
}
