using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    internal class Map
    {
        private readonly List<Prop> _props = new();

        public void LoadContent(ContentManager content)
        {
            // Cargas los shaders y modelos UNA sola vez
            var shader = content.Load<Effect>(TGCGame.ContentFolderEffects + "BasicShader");
            var treeModel = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/tree-pine-small");
            var truckflat = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/truck-flat");
            // Instancias los props
            _props.Add(new Prop(treeModel, shader, new Vector3(0, 0, 140)));
            _props.Add(new Prop(treeModel, shader, new Vector3(125, 0, 15), scale: new Vector3(1.5f)));
            _props.Add(new Prop(truckflat, shader, new Vector3(-100, 0, 0), rotation: new Vector3(0, MathHelper.ToRadians(90), 0)));
        }

        public void Draw(Matrix view, Matrix projection)
        {
            // iterar sobre cada prop y dibujarlo
            foreach (var prop in _props)
            {
                // Si cada prop tiene color propio o compartís uno genérico:
                prop.Draw(view, projection, Color.DarkSlateGray.ToVector3());
            }
        }
    }
}
