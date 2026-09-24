using System;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP
{
    // Responsible for detecting interactive props under the player's crosshair and executing interactions
    internal class InteractionManager
    {
        private readonly Map _outsideMap;
        private readonly Map _insideMap;

        public InteractionManager(Map outsideMap, Map insideMap)
        {
            _outsideMap = outsideMap;
            _insideMap = insideMap;
        }

        // Find nearest interactive prop under the given ray (searches active map depending on showInsideOnly)
        public InteractiveProp? FindInteractiveProp(Ray ray, bool useInside)
        {
            var map = useInside ? _insideMap : _outsideMap;
            InteractiveProp? best = null;
            float bestDist = float.MaxValue;

            foreach (var prop in map.GetProps())
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
            if (consumed)
            {
                var map = useInside ? _insideMap : _outsideMap;
                map.RemoveProp(prop);
            }
            return consumed;
        }
    }
}
