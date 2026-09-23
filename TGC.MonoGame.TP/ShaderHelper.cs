using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    internal static class ShaderHelper
    {
        // Clone the effect if we need to set a texture so other users of the base effect are not affected.
        public static Effect PrepareEffectWithTexture(Effect baseEffect, Texture2D texture)
        {
            if (baseEffect is null || texture is null)
            {
                return baseEffect;
            }

            var effect = baseEffect.Clone();

            // Prefer the parameter name used by BasicTexture / common models
            if (TrySetTextureParameter(effect, "ModelTexture", texture))
                return effect;

            var candidateNames = new[] { "basicTexture", "BasicTexture", "Texture", "DiffuseTexture", "DiffuseMap", "tex" };
            foreach (var name in candidateNames)
            {
                if (TrySetTextureParameter(effect, name, texture))
                    return effect;
            }

            // No parameter found; return the cloned effect unchanged
            return effect;
        }

        private static bool TrySetTextureParameter(Effect effect, string paramName, Texture2D texture)
        {
            try
            {
                var param = effect.Parameters[paramName];
                if (param != null)
                {
                    param.SetValue(texture);
                    return true;
                }
            }
            catch
            {
                // Ignore and let caller try other names
            }

            return false;
        }
    }
}
