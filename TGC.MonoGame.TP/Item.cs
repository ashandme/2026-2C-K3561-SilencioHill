using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    internal abstract class Item
    {
        public string Name { get; }
        public Model Model { get; }

        protected Item(string name, Model model = null)
        {
            Name = name;
            Model = model;
        }
        public abstract void Use(Player player);
        public virtual void OnEquip(Player player) { }
        public virtual void OnUnequip(Player player) { }

        public virtual bool AttachToCamera => false;
        // Camera-local offset: X=right, Y=up, Z=forward (camera local space)
        public virtual Vector3 CameraLocalOffset => Vector3.Zero;

        public virtual void DrawModel(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            if (Model == null || effect == null) return;

            effect.Parameters["View"]?.SetValue(view);
            effect.Parameters["Projection"]?.SetValue(projection);

            var boneTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (var mesh in Model.Meshes)
            {
                var meshWorld = boneTransforms[mesh.ParentBone.Index] * world;
                effect.Parameters["World"]?.SetValue(meshWorld);

                foreach (var part in mesh.MeshParts)
                {
                    part.Effect = effect;
                }

                mesh.Draw();
            }
        }
    }

    internal class CandleItem : Item
    {
        public bool IsLit { get; private set; }

        public CandleItem(Model model)
            : base("Candle", model)
        {
            IsLit = true;
        }

        public override void Use(Player player)
        {
            IsLit = !IsLit;
        }

        public override Vector3 CameraLocalOffset => new Vector3(0.4f, -0.5f, 1.0f);
        public override bool AttachToCamera => false;
    }

    internal class FlashlightItem : Item
    {
        public bool IsOn { get; private set; }

        public FlashlightItem(Model model = null) : base("Flashlight", model) { }

        public override void Use(Player player) => IsOn = !IsOn;

        // Flashlight should be attached to camera and always point forward
        public override bool AttachToCamera => true;
        public override Vector3 CameraLocalOffset => new Vector3(0.0f, 0.0f, -0.0f);
        }
}