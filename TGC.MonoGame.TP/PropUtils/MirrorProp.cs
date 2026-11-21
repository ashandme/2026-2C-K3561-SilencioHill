using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.PropUtils;

// A prop with a reflective surface. The mirror plane is derived from the model itself: the
// reflective part is the smallest mesh part (e.g. the glass quad of a bathroom mirror) and its
// thinnest axis is the plane normal. PlanarReflection renders the scene mirrored about that plane
// and DrawSurface overlays it on the part.
internal sealed class MirrorProp : Prop
{
    // Overlay offset along the plane normal (world units) so the surface does not z-fight the model quad.
    private const float SurfaceOffset = 0.2f;

    private VertexPositionNormalTexture[]? _quad;
    private Vector3 _localCenter;
    private Vector3 _localNormal;

    public MirrorProp(Model model, Effect effect, Vector3 position, Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
        : base(model, effect, position, rotation, scale, color)
    {
    }

    // Mirror plane in world space with its normal pointing to the viewer side.
    public Plane GetPlaneFacing(Vector3 eye)
    {
        EnsureGeometry();
        var world = GetWorldMatrix();
        var center = Vector3.Transform(_localCenter, world);
        var normal = Vector3.Normalize(Vector3.TransformNormal(_localNormal, world));
        if (Vector3.Dot(normal, eye - center) < 0f)
            normal = -normal;
        return new Plane(normal, -Vector3.Dot(normal, center));
    }

    public float DistanceTo(Vector3 eye)
    {
        EnsureGeometry();
        return Vector3.Distance(eye, Vector3.Transform(_localCenter, GetWorldMatrix()));
    }

    public void DrawSurface(Effect effect, Texture2D reflection, Plane plane,
        Matrix reflectedView, Matrix reflectedProjection, Matrix view, Matrix projection)
    {
        EnsureGeometry();
        var world = GetWorldMatrix() * Matrix.CreateTranslation(plane.Normal * SurfaceOffset);
        effect.Parameters["World"]?.SetValue(world);
        effect.Parameters["WorldViewProjection"]?.SetValue(world * view * projection);
        effect.Parameters["ReflectionView"]?.SetValue(reflectedView);
        effect.Parameters["Projection"]?.SetValue(reflectedProjection);
        effect.Parameters["ReflectionTexture"]?.SetValue(reflection);

        var device = effect.GraphicsDevice;
        var previousRasterizer = device.RasterizerState;
        device.RasterizerState = RasterizerState.CullNone;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, _quad!, 0, 2);
        }
        device.RasterizerState = previousRasterizer;
    }

    private void EnsureGeometry()
    {
        if (_quad != null) return;

        var transforms = new Matrix[Model.Bones.Count];
        Model.CopyAbsoluteBoneTransformsTo(transforms);

        ModelMeshPart? surface = null;
        var surfaceTransform = Matrix.Identity;
        foreach (var mesh in Model.Meshes)
        {
            foreach (var part in mesh.MeshParts)
            {
                if (part.NumVertices < 3) continue;
                if (surface == null || part.NumVertices < surface.NumVertices)
                {
                    surface = part;
                    surfaceTransform = transforms[mesh.ParentBone.Index];
                }
            }
        }
        if (surface == null) throw new InvalidOperationException("Mirror model has no mesh parts.");

        var stride = surface.VertexBuffer.VertexDeclaration.VertexStride;
        var positions = new Vector3[surface.NumVertices];
        surface.VertexBuffer.GetData(surface.VertexOffset * stride, positions, 0, positions.Length, stride);

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in positions)
        {
            var t = Vector3.Transform(p, surfaceTransform);
            min = Vector3.Min(min, t);
            max = Vector3.Max(max, t);
        }

        _localCenter = (min + max) * 0.5f;
        var size = max - min;
        // The plane normal is the axis along which the surface is thinnest.
        var axis = size.X <= size.Y && size.X <= size.Z ? 0 : size.Y <= size.Z ? 1 : 2;
        _localNormal = axis == 0 ? Vector3.UnitX : axis == 1 ? Vector3.UnitY : Vector3.UnitZ;

        // The two in-plane axes (a, b) in x/y/z order, with the plane coordinate taken from the center.
        Vector3 Point(float a, float b) => axis switch
        {
            0 => new Vector3(_localCenter.X, a, b),
            1 => new Vector3(a, _localCenter.Y, b),
            _ => new Vector3(a, b, _localCenter.Z)
        };
        var a0 = axis == 0 ? min.Y : min.X;
        var a1 = axis == 0 ? max.Y : max.X;
        var b0 = axis == 2 ? min.Y : min.Z;
        var b1 = axis == 2 ? max.Y : max.Z;

        var p00 = new VertexPositionNormalTexture(Point(a0, b0), _localNormal, new Vector2(0, 1));
        var p10 = new VertexPositionNormalTexture(Point(a1, b0), _localNormal, new Vector2(1, 1));
        var p01 = new VertexPositionNormalTexture(Point(a0, b1), _localNormal, new Vector2(0, 0));
        var p11 = new VertexPositionNormalTexture(Point(a1, b1), _localNormal, new Vector2(1, 0));
        _quad = new[] { p00, p01, p10, p10, p01, p11 };
    }
}
