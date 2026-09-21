using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace TGC.MonoGame.TP
{
    // Para manejar entradas en el juego de forma conveniente :D
    internal class InputManager
    {
        private KeyboardState _previous;
        private KeyboardState _current;

        public InputManager()
        {
            _previous = Keyboard.GetState();
            _current = _previous;
        }

        public void Update(GameTime gameTime)
        {
            _previous = _current;
            _current = Keyboard.GetState();
        }

        public bool IsKeyDown(Keys key) => _current.IsKeyDown(key);

        public bool IsKeyPressed(Keys key) => _current.IsKeyDown(key) && _previous.IsKeyUp(key);

        public bool IsKeyReleased(Keys key) => _current.IsKeyUp(key) && _previous.IsKeyDown(key);

        public void Reset()
        {
            _previous = Keyboard.GetState();
            _current = _previous;
        }
    }
}