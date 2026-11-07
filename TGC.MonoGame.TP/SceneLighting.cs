using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        public static Vector3 FogColor { get; set; } = Vector3.Zero;
        public static float FogStartDistance { get; set; }
        public static float FogEndDistance { get; set; } = 1f;
        public static float FogDensity { get; set; }
        public static bool FogEnabled { get; set; }
        public static int RenderFrame { get; set; }
        public static bool ShadowsEnabled { get; set; }
        public static Matrix LightViewProjection { get; set; } = Matrix.Identity;
        public static Texture2D ShadowMap { get; set; }
        public static float ShadowFar { get; set; } = 800f;
        // Set each frame by the active light item (flashlight / candle) to request a shadow pass.
        public static bool CastsShadows { get; set; }
        // Field of view of the shadow frustum, in degrees, pointed along LightDirection.
        public static float ShadowFieldOfView { get; set; } = 70f;
        public static Vector2 ShadowMapSize { get; set; } = new Vector2(1024f, 1024f);
    }
}
