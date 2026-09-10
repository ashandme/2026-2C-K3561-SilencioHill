using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
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
            // 1. Cargas los shaders y modelos UNA sola vez
            var shader = content.Load<Effect>(TGCGame.ContentFolderEffects + "BasicShader");
            var treeModel = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/");
            var rockModel = content.Load<Model>(TGCGame.ContentFolder3D + "roca/roca");

            // 2. Instancias los props reutilizando el mismo modelo (Instanciación lógica)
            _props.Add(new Prop(treeModel, shader, new Vector3(0, 0, 10)));
            _props.Add(new Prop(treeModel, shader, new Vector3(25, 0, 15), scale: new Vector3(1.5f)));
            _props.Add(new Prop(rockModel, shader, new Vector3(-10, 0, 5)));
        }

        public void Draw(Matrix view, Matrix projection)
        {
            // Podés iterar y dibujar todo el entorno
            foreach (var prop in _props)
            {
                // Si cada prop tiene color propio o compartís uno genérico:
                prop.Draw(view, projection, Color.DarkSlateGray.ToVector3());
            }
        }
    }
}
