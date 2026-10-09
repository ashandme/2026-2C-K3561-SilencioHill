using System.Collections.Generic;

namespace TGC.MonoGame.TP.Entities.Enemy
{
    // Estructura del JSON de ruta de patrulla: lista de puntos [x, y, z]
    public class RouteData
    {
        public List<float[]> Waypoints { get; set; } = new();
    }
}