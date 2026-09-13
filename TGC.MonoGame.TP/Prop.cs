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
    public Vector3 Color { get; set; } // <- faltaba esta propiedad
    private readonly Matrix[] _boneTransforms;

    private static readonly Random _random = new Random();

    public Prop(Model model, Effect effect, Vector3 position, Vector3? rotation = null, Vector3? scale = null, Vector3? color = null)
    {
        Model = model;
        Effect = effect;
        Position = position;
        Rotation = rotation ?? Vector3.Zero;
        Scale = scale ?? Vector3.One;
        Color = color ?? RandomColor(); // ahora "color" existe como parametro
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

    private static Vector3 RandomColor()
    {
        return new Vector3(
            (float)_random.NextDouble(),
            (float)_random.NextDouble(),
            (float)_random.NextDouble());
    }

    public Matrix GetWorldMatrix()
    {
        return Matrix.CreateScale(Scale) *
               Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
               Matrix.CreateTranslation(Position);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        var world = GetWorldMatrix();
        Effect.Parameters["View"]?.SetValue(view);
        Effect.Parameters["Projection"]?.SetValue(projection);
        Effect.Parameters["DiffuseColor"]?.SetValue(Color); // usa el color propio de la instancia

        foreach (var mesh in Model.Meshes)
        {
            var boneTransform = _boneTransforms[mesh.ParentBone.Index];
            Effect.Parameters["World"]?.SetValue(boneTransform * world);
            mesh.Draw();
        }
    }
}
