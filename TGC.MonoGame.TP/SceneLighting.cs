using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP
{
    // Simple global lighting state used by shaders. Updated each frame by the game.
    internal static class SceneLighting
    {
        public static bool FlashlightEnabled { get; set; } = false;
        public static Vector3 LightPosition { get; set; } = Vector3.Zero;
        public static Vector3 LightDirection { get; set; } = Vector3.Forward;
        public static bool IsDirectional { get; set; }
        public static bool IsSpotlight { get; set; }
        public static float SpotInnerCosine { get; set; } = 0.94f;
        public static float SpotOuterCosine { get; set; } = 0.82f;
        public static Vector3 Attenuation { get; set; } = new Vector3(1f, 0.01f, 0.001f);
        public static float UpwardLightScale { get; set; } = 1f;
        public static Vector3 LightColor { get; set; } = new Vector3(1f, 1f, 1f);
        public static float AmbientIntensity { get; set; } = 0.08f;
        public static float DiffuseIntensity { get; set; } = 1f;
        // Camera (eye) position computed once per frame by the game
        public static Vector3 EyePosition { get; set; } = Vector3.Zero;
    }
}
