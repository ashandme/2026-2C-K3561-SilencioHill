using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP
{
    // Simple global lighting state used by shaders. Updated each frame by the game.
    internal static class SceneLighting
    {
        public static bool FlashlightEnabled { get; set; } = false;
        public static Vector3 LightPosition { get; set; } = Vector3.Zero;
        public static Vector3 LightColor { get; set; } = new Vector3(1f, 1f, 1f);
        // Camera (eye) position computed once per frame by the game
        public static Vector3 EyePosition { get; set; } = Vector3.Zero;
    }
}
