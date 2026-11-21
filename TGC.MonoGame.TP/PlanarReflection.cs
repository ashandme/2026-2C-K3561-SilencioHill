using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP;

// Planar reflection for MirrorProp surfaces. Each frame, the closest visible mirror gets the scene
// rendered from the camera mirrored about its plane into a render target; DrawSurface then paints it.
internal sealed class PlanarReflection
{
    private const float MaxMirrorDistance = 800f;

    private readonly GraphicsDevice _graphics;
    private readonly Effect _effect;
    private readonly RenderTarget2D _target;

    private MirrorProp? _active;
    private Plane _plane;
    private Matrix _reflectedView;
    private Matrix _reflectedProjection;

    public PlanarReflection(GraphicsDevice graphics, Effect effect, int width, int height)
    {
        _graphics = graphics;
        _effect = effect;
        _target = new RenderTarget2D(graphics, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);
    }

    // Renders the reflection for the closest visible mirror (if any). drawScene must draw the whole
    // level with the given view/projection and must not draw mirror surfaces. The caller must set
    // its own render target afterwards.
    public void Render(IEnumerable<Prop>? props, Vector3 eye, Matrix view, Matrix projection,
        Color clearColor, Action<Matrix, Matrix> drawScene)
    {
        _active = null;
        if (props == null) return;

        var frustum = new BoundingFrustum(view * projection);
        var bestDistance = MaxMirrorDistance;
        foreach (var prop in props)
        {
            if (prop is not MirrorProp mirror || !mirror.IsVisible(frustum)) continue;
            var distance = mirror.DistanceTo(eye);
            if (distance > bestDistance) continue;
            bestDistance = distance;
            _active = mirror;
        }
        if (_active == null) return;

        _plane = _active.GetPlaneFacing(eye);
        var reflection = Matrix.CreateReflection(_plane);
        _reflectedView = reflection * view;
        _reflectedProjection = ObliqueProjection(projection, _reflectedView, _plane);

        var previousRasterizer = _graphics.RasterizerState;
        var previousEye = SceneLighting.EyePosition;
        try
        {
            _graphics.SetRenderTarget(_target);
            _graphics.DepthStencilState = DepthStencilState.Default;
            _graphics.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, clearColor, 1f, 0);
            // Mirroring flips the triangle winding.
            _graphics.RasterizerState = RasterizerState.CullClockwise;
            SceneLighting.EyePosition = Vector3.Transform(eye, reflection);
            drawScene(_reflectedView, _reflectedProjection);
        }
        finally
        {
            SceneLighting.EyePosition = previousEye;
            _graphics.RasterizerState = previousRasterizer;
        }
    }

    // Paints the reflection on the mirror chosen by Render. Call after the main scene is drawn.
    public void DrawSurface(Matrix view, Matrix projection)
    {
        _active?.DrawSurface(_effect, _target, _plane, _reflectedView, _reflectedProjection, view, projection);
    }

    // Replaces the near plane of the projection with the mirror plane (Lengyel oblique frustum, D3D
    // depth range) so geometry behind the mirror is not rendered into the reflection.
    // worldPlane normal must point to the side that is kept (the viewer side).
    private static Matrix ObliqueProjection(Matrix projection, Matrix reflectedView, Plane worldPlane)
    {
        var toCamera = Matrix.Transpose(Matrix.Invert(reflectedView));
        var clip = Vector4.Transform(new Vector4(worldPlane.Normal, worldPlane.D), toCamera);

        var corner = Vector4.Transform(
            new Vector4(MathF.Sign(clip.X), MathF.Sign(clip.Y), 1f, 1f), Matrix.Invert(projection));
        var dot = Vector4.Dot(clip, corner);
        if (MathF.Abs(dot) < 1e-6f) return projection;

        var row = clip / dot;
        projection.M13 = row.X;
        projection.M23 = row.Y;
        projection.M33 = row.Z;
        projection.M43 = row.W;
        return projection;
    }
}
