using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.Collisions;

internal readonly struct CollisionResolution
{
    public Vector3 Position { get; }
    public CollisionShape? BlockingShape { get; }
    public bool BlockedX { get; }
    public bool BlockedY { get; }
    public bool BlockedZ { get; }
    public bool IsBlocked => BlockedX || BlockedY || BlockedZ;

    public CollisionResolution(Vector3 position, CollisionShape? blockingShape, bool blockedX, bool blockedY, bool blockedZ)
    {
        Position = position;
        BlockingShape = blockingShape;
        BlockedX = blockedX;
        BlockedY = blockedY;
        BlockedZ = blockedZ;
    }
}

internal sealed class CollisionHandler
{
    private readonly List<CollisionShape> _colliders = new();

    public void Rebuild(IReadOnlyList<CollisionShape> colliders)
    {
        _colliders.Clear();
        _colliders.AddRange(colliders);
    }

    public CollisionResolution ResolveSphereMovement(
        Vector3 currentPosition,
        Vector3 desiredPosition,
        float radius,
        bool resolveVertical = true)
    {
        var position = currentPosition;
        CollisionShape? blockingShape = null;
        var blockedX = false;
        var blockedY = false;
        var blockedZ = false;

        var movement = desiredPosition - currentPosition;
        var steps = Math.Max(1, (int)MathF.Ceiling(movement.Length() / MathF.Max(radius * 0.5f, 0.5f)));
        var step = movement / steps;

        for (var i = 0; i < steps; i++)
        {
            TryMoveAxis(ref position, step.X, Axis.X, radius, ref blockedX, ref blockingShape);
            if (resolveVertical)
                TryMoveAxis(ref position, step.Y, Axis.Y, radius, ref blockedY, ref blockingShape);
            TryMoveAxis(ref position, step.Z, Axis.Z, radius, ref blockedZ, ref blockingShape);
        }

        return new CollisionResolution(position, blockingShape, blockedX, blockedY, blockedZ);
    }

    private void TryMoveAxis(
        ref Vector3 position,
        float amount,
        Axis axis,
        float radius,
        ref bool blocked,
        ref CollisionShape? blockingShape)
    {
        if (MathF.Abs(amount) < 0.0001f) return;

        var candidate = position;
        switch (axis)
        {
            case Axis.X: candidate.X += amount; break;
            case Axis.Y: candidate.Y += amount; break;
            case Axis.Z: candidate.Z += amount; break;
        }

        var shape = FindBlockingShape(candidate, radius);
        if (shape == null)
        {
            position = candidate;
            return;
        }

        blocked = true;
        blockingShape ??= shape;
    }

    private CollisionShape? FindBlockingShape(Vector3 center, float radius)
    {
        var sphere = new BoundingSphere(center, radius);
        foreach (var collider in _colliders)
        {
            if (collider.Intersects(sphere))
                return collider;
        }

        return null;
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }
}
