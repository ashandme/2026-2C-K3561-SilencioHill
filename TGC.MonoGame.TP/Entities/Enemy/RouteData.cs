using System.Collections.Generic;

namespace TGC.MonoGame.TP.Entities.Enemy
{
    // Estructura del JSON de ruta de patrulla: lista de puntos [x, y, z]
    public class RouteData
    {
        // Punto de aparicion inicial [x, y, z]. Si falta, aparece en el primer waypoint.
        public float[]? Spawn { get; set; }

        // Ruta base (planta baja).
        public List<float[]> Waypoints { get; set; } = new();

        // Rutas extra por piso: se usan cuando el jugador esta a una altura >= MinPlayerY.
        public List<FloorRoute> Floors { get; set; } = new();
    }

    public class FloorRoute
    {
        public float MinPlayerY { get; set; }
        public List<float[]> Waypoints { get; set; } = new();
    }
}
