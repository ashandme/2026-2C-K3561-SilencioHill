using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;
// CLASE PARA MANEJAR LOS MODELOS QUE IMPORTAMOS
internal class Prop
{
    public Model Model { get; }
    public Effect Effect { get; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;
    private readonly Matrix[] _boneTransforms;

    public Prop(Model model, Effect effect, Vector3 position, Vector3? rotation = null, Vector3? scale = null)
    {
        Model = model;
        Effect = effect;
        Position = position;
        Rotation = rotation ?? Vector3.Zero;
        Scale = scale ?? Vector3.One;
        foreach (var mesh in Model.Meshes)
        {
            foreach (var part in mesh.MeshParts)
            {
                part.Effect = Effect;
            }
        }
        _boneTransforms = new Matrix[Model.Bones.Count];
        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
    }
    public Matrix GetWorldMatrix()
    {
        return Matrix.CreateScale(Scale) *
               Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
               Matrix.CreateTranslation(Position);
    }
    public void Draw(Matrix view, Matrix projection, Vector3 diffuseColor)
    {
        var world = GetWorldMatrix();
        // ESTO PUEDE CAMBIAR
        Effect.Parameters["View"]?.SetValue(view);
        Effect.Parameters["Projection"]?.SetValue(projection);
        Effect.Parameters["DiffuseColor"]?.SetValue(diffuseColor);

        foreach (var mesh in Model.Meshes)
        {
            var boneTransform = _boneTransforms[mesh.ParentBone.Index];
            Effect.Parameters["World"]?.SetValue(boneTransform * world);
            mesh.Draw();
        }
    }
}
