using System;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Resolution-independent UI sizing. Layout code expresses positions as fractions of the viewport and
// sizes (fonts, margins, bars) in "reference pixels" at 1280x720, multiplied by Of(viewport).
internal static class UiScale
{
    private const float ReferenceWidth = 1280f;
    private const float ReferenceHeight = 720f;
    private const float MinScale = 0.5f;

    public static float Of(Viewport viewport) =>
        MathF.Max(MinScale, MathF.Min(viewport.Width / ReferenceWidth, viewport.Height / ReferenceHeight));

    public static float Of(GraphicsDevice device) => Of(device.Viewport);
}
