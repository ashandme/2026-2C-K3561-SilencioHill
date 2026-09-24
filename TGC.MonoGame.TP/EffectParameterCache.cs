using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace TGC.MonoGame.TP
{
    // Helper that caches EffectParameter lookups to avoid repeated string-based searches
    internal class EffectParameterCache
    {
        // Shared cache per Effect instance
        private static readonly ConcurrentDictionary<Effect, EffectParameterCache> _cache = new();

        public static EffectParameterCache Get(Effect effect)
        {
            if (effect == null) return null;
            return _cache.GetOrAdd(effect, e => new EffectParameterCache(e));
        }

        public readonly EffectParameter View;
        public readonly EffectParameter Projection;
        public readonly EffectParameter World;
        public readonly EffectParameter WorldViewProjection;
        public readonly EffectParameter InverseTransposeWorld;

        public readonly EffectParameter ModelTexture;
        public readonly EffectParameter Texture;
        public readonly EffectParameter DiffuseMap;
        public readonly EffectParameter BaseTexture;

        public readonly EffectParameter DiffuseColor;

        public readonly EffectParameter EyePosition;

        // Multi-light parameters
        public readonly EffectParameter LightCount;
        public readonly EffectParameter LightAmbient0;
        public readonly EffectParameter LightDiffuse0;
        public readonly EffectParameter LightSpecular0;
        public readonly EffectParameter LightPosition0;

        private EffectParameterCache(Effect effect)
        {
            if (effect == null) return;

            View = effect.Parameters["View"];
            Projection = effect.Parameters["Projection"];
            World = effect.Parameters["World"];
            WorldViewProjection = effect.Parameters["WorldViewProjection"];
            InverseTransposeWorld = effect.Parameters["InverseTransposeWorld"];

            ModelTexture = effect.Parameters["ModelTexture"];
            Texture = effect.Parameters["Texture"];
            DiffuseMap = effect.Parameters["DiffuseMap"];
            BaseTexture = effect.Parameters["baseTexture"];

            DiffuseColor = effect.Parameters["DiffuseColor"];

            EyePosition = effect.Parameters["eyePosition"];

            LightCount = effect.Parameters["lightCount"];
            LightAmbient0 = effect.Parameters["lightAmbient[0]"];
            LightDiffuse0 = effect.Parameters["lightDiffuse[0]"];
            LightSpecular0 = effect.Parameters["lightSpecular[0]"];
            LightPosition0 = effect.Parameters["lightPosition[0]"];
        }
    }
}
