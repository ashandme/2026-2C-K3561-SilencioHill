using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP.Collisions;

internal sealed class CollisionHandler
{
    private readonly List<CollisionShape> _shapes = new();

    public float PlayerRadius { get; set; } = 4f;

    public void SetProps(IEnumerable<Prop> props)
    {
        _shapes.Clear();
        foreach (var prop in props)
        {
            _shapes.AddRange(prop.Colliders);
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
        var playerSphere = new BoundingSphere(playerPosition, PlayerRadius);
        foreach (var shape in _shapes)
        {
            if (shape.Intersects(playerSphere))
                return true;
        }

        return false;
    }

}
