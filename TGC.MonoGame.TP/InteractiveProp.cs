using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.PropUtils;
namespace TGC.MonoGame.TP.LevelUtils
{
    // Interactive prop: a prop that can be interacted by the player (e.g., pickups, batteries)
    internal class InteractiveProp : Prop
    {
        public string Id { get; }
        public virtual int Priority => 0;

        public InteractiveProp(Model model, Effect effect, Vector3 position, string id, Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
            : base(model, effect, position, rotation, scale, color)
        {
            Id = id;
        }

        // Called when the player interacts with this prop (points + presses interact key)
        // Returns true if interaction succeeded/consumed
        public virtual bool OnInteract(Player player)
        {
            return false;
        }
    }

    // Example battery pickup that adds time to player's flashlight
    internal class BatteryProp : InteractiveProp
    {
        private readonly float _addSeconds;
        private bool _pickedUp;

        public BatteryProp(Model model, Effect effect, Vector3 position, string id, float addSeconds = 30f)
            : base(model, effect, position, id)
        {
            _addSeconds = addSeconds;
            _pickedUp = false;
            this.Rotation = new Vector3(0f, 0f, 0.2f);
            this.Scale *= 0.2f;
        }

        public override bool OnInteract(Player player)
        {
            if (_pickedUp) return false;

            // Find flashlight in player's inventory and add time
            foreach (var it in player.Inventory)
            {
                if (it is FlashlightItem f)
                {
                    // Use public API to add time
                    f.AddTime(_addSeconds);
                    _pickedUp = true;
                    return true;
                }
            }

            return false;
        }
    }
    internal class HidingSpotProp : InteractiveProp
    {
        // Punto donde se para el jugador al esconderse (afuera del mesh, no en el centro del prop)
        public Vector3 HideStandPoint { get; set; }
        public HidingSpotProp(Model model, Effect effect, Vector3 position, string id)
            : base(model, effect, position, id) 
        {
            HideStandPoint = position; // por defecto, ajustalo al crearlo en Map.cs
        }

        public override int Priority => 5; // entre pila (0) y puerta (10), ajustable

        public override bool OnInteract(Player player)
            {
            player.ToggleHiding(HideStandPoint);
            return false; // reutilizable: no se saca del mapa como la pila
            }
    }
}
