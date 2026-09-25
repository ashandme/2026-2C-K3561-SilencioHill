using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP
{
    // Represents a single level (either outside or inside). Encapsulates its own maps, props and enemy.
    internal class Level
    {
        // the level map should be an array of maps
        private readonly Map[] _maps = new Map[0];
        // TODO: REFACTOR make a single class for Entities (props, enemies, etc.) and have a single list of entities instead of separate maps for props and enemies.
        public Enemy? Enemy { get; private set; }

        // A simple identifier
        public string Name { get; }

        public Level(string name, Map[] maps)
        {
            Name = name ?? "unnamed";
            _maps = maps ?? new Map[0];
        }

        public void LoadLevelContent(ContentManager content)
        {
            for (int i = 0; i < _maps.Length; i++)
            {
                try
                {
                    _maps[i].LoadContent(content);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Level[{Name}].LoadContent: map[{i}] load failed: " + ex.Message);
                }
            }

            // Spawn an enemy for this level if desired. For simplicity we attempt to create one from a known path.
            try
            {
                var enemyModel = content.Load<Model>(TGCGame.ContentFolder3D + "kenney_retro-urban-kit/detail-dumpster-closed");
                Enemy = new Enemy(enemyModel, content.Load<Effect>(TGCGame.ContentFolderEffects + "BasicShader"), "Content/enemyRoute.json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Level[{Name}].LoadContent: enemy creation failed: " + ex.Message);
                Enemy = null;
            }
        }

        public void UnloadContent()
        {
            // Clear props and allow GC to collect models/effects if no other references exist.
            try
            {
                for (int i = 0; i < _maps.Length; i++)
                {
                    _maps[i].UnloadContent();
                }
            }
            catch { }
            Enemy = null;
        }

        public void Draw(Matrix view, Matrix projection)
        {
            for (int i = 0; i < _maps.Length; i++)
            {
                _maps[i].Draw(view, projection);
            }
            
            Enemy.Draw(view, projection);
        }

        public IReadOnlyList<Prop> GetProps()
        {
            List<Prop> allProps = new List<Prop>();
            for (int i = 0; i < _maps.Length; i++)
            {
                allProps.AddRange(_maps[i].GetProps());
            }
            return allProps;
        }
    }
}
