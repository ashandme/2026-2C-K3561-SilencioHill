using Microsoft.Xna.Framework;
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

            var candidateNames = new[] { "baseTexture", "BasicTexture", "Texture", "DiffuseTexture", "DiffuseMap", "tex" };
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

        // Prepare a Blinn-Phong style effect: clone, set texture (if provided) and sensible
        // default material/light parameters so the shader has usable values out of the box.
        public static Effect PrepareBlinnPhong(Effect baseEffect, Texture2D? texture = null)
        {
            if (baseEffect is null)
                return baseEffect;

            var effect = baseEffect.Clone();

            // Try to set a texture if provided
            if (texture != null)
            {
                // Try common names
                if (!TrySetTextureParameter(effect, "ModelTexture", texture))
                {
                    var candidateNames = new[] { "baseTexture", "BasicTexture", "Texture", "DiffuseTexture", "DiffuseMap", "tex" };
                    foreach (var name in candidateNames)
                    {
                        if (TrySetTextureParameter(effect, name, texture))
                            break;
                    }
                }
            }

            // Set some reasonable defaults if the shader declares those parameters
            TrySetVector3Parameter(effect, "ambientColor", new Vector3(0.1f, 0.1f, 0.1f));
            TrySetVector3Parameter(effect, "diffuseColor", new Vector3(1f, 1f, 1f));
            TrySetVector3Parameter(effect, "specularColor", new Vector3(1f, 1f, 1f));

            TrySetFloatParameter(effect, "KAmbient", 0.2f);
            TrySetFloatParameter(effect, "KDiffuse", 1.0f);
            TrySetFloatParameter(effect, "KSpecular", 0.5f);
            TrySetFloatParameter(effect, "shininess", 16f);

            // Initialize first light slot and set lightCount to 1 for shaders that support multiple lights
            try
            {
                var pCount = effect.Parameters["lightCount"];
                if (pCount != null)
                {
                    pCount.SetValue(1);
                }
            }
            catch
            {
                // Ignore if parameter not present or cannot be set
            }

            TrySetVector3Parameter(effect, "lightAmbient[0]", new Vector3(0.05f, 0.05f, 0.05f));
            TrySetVector3Parameter(effect, "lightDiffuse[0]", new Vector3(1f, 1f, 1f));
            TrySetVector3Parameter(effect, "lightSpecular[0]", new Vector3(1f, 1f, 1f));

            // Do not set eyePosition or lightPosition here — those should be updated each frame.

            return effect;
        }

        private static bool TrySetVector3Parameter(Effect effect, string paramName, Vector3 value)
        {
            try
            {
                var p = effect.Parameters[paramName];
                if (p != null)
                {
                    p.SetValue(value);
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TrySetFloatParameter(Effect effect, string paramName, float value)
        {
            try
            {
                var p = effect.Parameters[paramName];
                if (p != null)
                {
                    p.SetValue(value);
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}
