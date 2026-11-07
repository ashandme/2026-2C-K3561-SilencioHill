using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Entities;

namespace TGC.MonoGame.TP.PropUtils;

// Prueba: se "fotografía" interactuando con E, como una pila
internal class EvidenceProp : InteractiveProp
{
    public EvidenceProp(Model model, Effect effect, Vector3 position, string id,
        Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
        : base(model, effect, position, id, rotation, scale, color) { }

    public override int Priority => 3;

    public override bool OnInteract(Player player) => true; // el conteo lo hace PlayingState
}