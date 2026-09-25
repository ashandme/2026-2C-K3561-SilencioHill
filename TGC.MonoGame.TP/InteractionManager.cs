using System;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP
{
    // Responsible for detecting interactive props under the player's crosshair and executing interactions
    internal class InteractionManager
    {
        // Instead of holding maps, accept a provider to obtain active props and a remover for propagation
        private readonly Func<bool, System.Collections.Generic.IReadOnlyList<Prop>> _propsProvider;
        private readonly Action<Prop, bool>? _removeAction;

        public InteractionManager(Func<bool, System.Collections.Generic.IReadOnlyList<Prop>> propsProvider, Action<Prop, bool>? removeAction = null)
        {
            _propsProvider = propsProvider ?? throw new ArgumentNullException(nameof(propsProvider));
            _removeAction = removeAction;
        }

        // Find nearest interactive prop under the given ray (searches active map depending on showInsideOnly)
        public InteractiveProp? FindInteractiveProp(Ray ray, bool useInside)
        {
            var props = _propsProvider(useInside);
            InteractiveProp? best = null;
            float bestDist = float.MaxValue;

            foreach (var prop in props)
            {
                if (prop is InteractiveProp ip)
                {
                    if (ip.IntersectsRay(ray, out var dist))
                    {
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best = ip;
                        }
                    }
                }
            }

            return best;
        }

        // Execute interaction and remove prop from map if consumed
        public bool Interact(InteractiveProp prop, Player player, bool useInside)
        {
            if (prop == null) return false;
            var consumed = prop.OnInteract(player);
            if (consumed && _removeAction != null)
            {
                _removeAction(prop, useInside);
            }
            return consumed;
        }
    }
}
