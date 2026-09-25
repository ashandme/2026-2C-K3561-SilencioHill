using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP
{
    internal abstract class Item
    {
        public string Name { get; }
        public Model Model { get; }

        // Optional lifetime for items that consume over time (seconds)
        public float MaxDurationSeconds { get; protected set; } = 0f;
        public float RemainingSeconds { get; protected set; } = 0f;

        // Optional per-item tuning for how the item sits in the hand
        public Vector3 HandScale { get; set; } = Vector3.One;
        public Vector3 HandRotationOffset { get; set; } = Vector3.Zero;

        protected Item(string name, Model model = null)
        {
            Name = name;
            Model = model;
            MaxDurationSeconds = 0f;
            RemainingSeconds = 0f;
        }

        // Public method to add time to the flashlight battery
        public void AddTime(float seconds)
        {
            if (seconds <= 0f) return;
            RemainingSeconds = Math.Min(MaxDurationSeconds, RemainingSeconds + seconds);
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

            // Compute correction from per-item hand scale/rotation offsets
            var localScale = Matrix.CreateScale(HandScale);
            var localRot = Matrix.CreateFromYawPitchRoll(HandRotationOffset.Y, HandRotationOffset.X, HandRotationOffset.Z);
            var finalWorld = localScale * localRot * world;

            // Allocate boneTransforms once per Item instance could be more efficient.
            // For held items draws are rare, so keep simple allocation here.
            var boneTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (var mesh in Model.Meshes)
            {
                var meshWorld = boneTransforms[mesh.ParentBone.Index] * finalWorld;
                effect.Parameters["World"]?.SetValue(meshWorld);

                foreach (var part in mesh.MeshParts)
                {
                    part.Effect = effect;
                }

                mesh.Draw();
            }
        }

        // Drain remaining time; when reaches zero, the item should switch off in the specific implementation
        public virtual void Drain(float seconds)
        {
            if (RemainingSeconds <= 0f) return;
            RemainingSeconds = Math.Max(0f, RemainingSeconds - seconds);
        }

        // Provide a HUD-friendly status line for this item. Return null if no status to show.
        public virtual string GetHudStatus()
        {
            return null;
        }
    }

    internal class CandleItem : Item
    {
        public bool IsLit { get; private set; }

        public CandleItem(Model model)
            : base("Candle", model)
        {
            IsLit = false;
            HandScale = Vector3.One * 0.007f;
            MaxDurationSeconds = 120f; // default 2 minutes of burn time
            RemainingSeconds = MaxDurationSeconds;
        }

        public override void Use(Player player)
        {
            if (RemainingSeconds <= 0f) return;
            IsLit = !IsLit;
        }
        public override Vector3 CameraLocalOffset => new Vector3(0.4f, -0.8f, 1.0f);
        public override bool AttachToCamera => false;

        public override void Drain(float seconds)
        {
            base.Drain(seconds);
            if (RemainingSeconds <= 0f)
            {
                IsLit = false;
            }
        }

        public override string GetHudStatus()
        {
            var state = IsLit ? "ON" : "OFF";
            var time = TimeSpan.FromSeconds(RemainingSeconds).ToString(@"mm\:ss");
            return $"Candle: {state}  {time}";
        }
    }

    internal class FlashlightItem : Item
    {
        public bool IsOn { get; private set; }
        // Optional texture and effect for drawing the flashlight with a simple texture shader
        public Texture2D Texture { get; set; }
        public Effect TextureEffect { get; set; }

        public FlashlightItem(Model model = null, Texture2D texture = null, Effect textureEffect = null) : base("Flashlight", model)
        {
            HandScale = Vector3.One * 0.002f;
            Texture = texture;
            TextureEffect = textureEffect;
            IsOn = true;
            MaxDurationSeconds = 180f; // default 3 minutes battery
            RemainingSeconds = MaxDurationSeconds;
        }

        public override void Use(Player player)
        {
            if (RemainingSeconds <= 0f) return;
            IsOn = !IsOn;
        }

        // Flashlight should be attached to camera and always point forward
        public override bool AttachToCamera => true;
        public override Vector3 CameraLocalOffset => new Vector3(0.5f, -0.5f, 1.0f);

        public override void DrawModel(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            // If we have a texture and a BasicTexture effect, use it to draw the model textured
            if (Model == null) return;

            if (Texture != null && TextureEffect != null)
            {
                TextureEffect.Parameters["View"]?.SetValue(view);
                TextureEffect.Parameters["Projection"]?.SetValue(projection);

                var localScale = Matrix.CreateScale(HandScale);
                var localRot = Matrix.CreateFromYawPitchRoll(HandRotationOffset.Y, HandRotationOffset.X, HandRotationOffset.Z);
                var finalWorld = localScale * localRot * world;

                var boneTransforms = new Matrix[Model.Bones.Count];
                Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

                foreach (var mesh in Model.Meshes)
                {
                    var meshWorld = boneTransforms[mesh.ParentBone.Index] * finalWorld;
                    TextureEffect.Parameters["World"]?.SetValue(meshWorld);
                    // BasicTexture.fx expects the texture parameter named "ModelTexture"
                    TextureEffect.Parameters["ModelTexture"]?.SetValue(Texture);

                    foreach (var part in mesh.MeshParts)
                    {
                        part.Effect = TextureEffect;
                    }

                    mesh.Draw();
                }

                return;
            }

            base.DrawModel(effect, world, view, projection);
        }
        public override void Drain(float seconds)
        {
            base.Drain(seconds);
            if (RemainingSeconds <= 0f)
            {
                IsOn = false;
            }
        }

        public override string GetHudStatus()
        {
            var state = IsOn ? "ON" : "OFF";
            var time = TimeSpan.FromSeconds(RemainingSeconds).ToString(@"mm\:ss");
            return $"Flashlight: {state}  {time}";
        }
    }
}
