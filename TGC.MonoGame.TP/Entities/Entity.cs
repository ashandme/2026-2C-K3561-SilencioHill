using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP.Entities;

internal class Entity
{
    private readonly List<CollisionShape> _colliders = new();
    private readonly List<CollisionShape> _sensors = new();

    protected GameMesh Mesh { get; }
    public IReadOnlyList<CollisionShape> Colliders => _colliders;
    public IReadOnlyList<CollisionShape> Sensors => _sensors;

    public Vector3 Position { get; private set; }
    public Vector3 Rotation { get; private set; }
    public Vector3 Scale { get; private set; } = Vector3.One;
    public bool Visible { get; set; } = true;

    public Vector3 ForwardDirection => Vector3.Transform(
        Vector3.Forward,
        Matrix.CreateRotationY(Rotation.Y));

    protected float ModelYawOffset { get; set; }

    public Entity(GameMesh mesh, Vector3 position, Vector3 rotation, Vector3? scale = null)
    {
        Mesh = mesh;
        Position = position;
        Rotation = rotation;
        Scale = scale ?? Vector3.One;
        SyncTransform();
    }

    protected void AddCollider(CollisionShape shape)
    {
        _colliders.Add(shape);
        shape.UpdateTransform(Position, ForwardDirection);
    }

    protected void AddSensor(CollisionShape shape)
    {
        _sensors.Add(shape);
        shape.UpdateTransform(Position, ForwardDirection);
    }

    public void Move(Vector3 delta)
    {
        SetPosition(Position + delta);
    }

    public void Rotate(Vector3 delta)
    {
        SetRotation(Rotation + delta);
    }

    public void FaceDirection(Vector3 direction)
    {
        var flatDirection = new Vector3(direction.X, 0f, direction.Z);
        if (flatDirection.LengthSquared() < 0.0001f) return;

        flatDirection.Normalize();
        SetRotation(new Vector3(
            Rotation.X,
            MathF.Atan2(flatDirection.X, flatDirection.Z),
            Rotation.Z));
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
        SyncTransform();
    }

    public void SetRotation(Vector3 rotation)
    {
        Rotation = rotation;
        SyncTransform();
    }

    public void SetScale(Vector3 scale)
    {
        Scale = scale;
        SyncTransform();
    }

    public bool IsColliding(CollisionShape shape, BoundingSphere other)
    {
        return shape.Intersects(other);
    }

    public bool IsColliding(BoundingSphere other)
    {
        foreach (var collider in _colliders)
        {
            if (collider.Intersects(other)) return true;
        }

        return false;
    }

    public bool IsSensorTriggered(CollisionShape sensor, BoundingSphere other)
    {
        return sensor.Intersects(other);
    }

    protected virtual void SyncTransform()
    {
        Mesh.Position = Position;
        Mesh.Rotation = new Vector3(Rotation.X, Rotation.Y + ModelYawOffset, Rotation.Z);
        Mesh.Scale = Scale;

        var forward = ForwardDirection;
        foreach (var collider in _colliders)
            collider.UpdateTransform(Position, forward);
        foreach (var sensor in _sensors)
            sensor.UpdateTransform(Position, forward);
    }

    public virtual void Update(GameTime gameTime) { }

    public virtual void Draw(Matrix view, Matrix projection)
    {
        if (Visible)
            Mesh.Draw(GetWorldMatrix(), view, projection);
    }

    private Matrix GetWorldMatrix()
    {
        return Matrix.CreateScale(Scale) *
               Matrix.CreateFromYawPitchRoll(Rotation.Y + ModelYawOffset, Rotation.X, Rotation.Z) *
               Matrix.CreateTranslation(Position);
    }
}
