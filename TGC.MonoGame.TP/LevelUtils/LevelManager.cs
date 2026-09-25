using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP.LevelUtils
{
    internal class LevelManager
    {
        private readonly Dictionary<string, Level> _levels = new();
        private Level? _current;
        public string? CurrentName { get; private set; }

        public void AddLevel(string name, Level level)
        {
            if (string.IsNullOrEmpty(name) || level == null) return;
            _levels[name] = level;
        }

        public void LoadLevel(string name, ContentManager content)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!_levels.TryGetValue(name, out var lvl))
            {
                lvl = new Level(name, new Map[0]); // Create an empty level if not found
                _levels[name] = lvl;
            }

            // Unload previous
            if (_current != null && _current != lvl)
            {
                try
                {
                    _current.UnloadContent();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("LevelManager: unload failed: " + ex.Message);
                }
            }

            // Load the new one
            try
            {
                lvl.LoadLevelContent(content);
                _current = lvl;
                CurrentName = name;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LevelManager: load failed: " + ex.Message);
                throw;
            }
        }

        public void UnloadCurrent()
        {
            if (_current == null) return;
            try
            {
                _current.UnloadContent();
            }
            catch { }
            _current = null;
            CurrentName = null;
        }

        public Level? Current => _current;

        public IReadOnlyList<Prop> GetActiveProps()
        {
            if (_current == null) return new List<Prop>().AsReadOnly();
            return _current.GetProps();
        }

        public void ReloadCurrent(ContentManager content)
        {
            if (_current == null || string.IsNullOrEmpty(CurrentName)) return;
            try
            {
                // unload then reload
                _current.UnloadContent();
                _current.LoadLevelContent(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LevelManager.ReloadCurrent failed: " + ex.Message);
                throw;
            }
        }
    }
}
