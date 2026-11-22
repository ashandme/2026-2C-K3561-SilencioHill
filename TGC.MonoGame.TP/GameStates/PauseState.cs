using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class PauseState : IGameState
{
    private readonly GameStateContext _context;
    private readonly MenuScreen _menu;
    private readonly Texture2D _pixel;
    private bool _optionsOpen;

    public bool DrawsStatesBelow => true;

    public PauseState(GameStateContext context, Func<IGameState> restart, Func<IGameState> mainMenu)
    {
        _context = context;
        _menu = new MenuScreen(context, new[]
        {
            ("Resume", () => context.States.Pop()),
            ("Opciones", OpenOptions),
            ("Restart", () => context.States.Replace(restart())),
            ("Main Menu", () =>
            {
                context.States.Clear();
                context.States.Push(mainMenu());
            }),
            ("Quit", context.Quit)
        });
        _pixel = new Texture2D(context.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Enter()
    {
        _optionsOpen = false;
        _context.SetMouseVisible(true);
    }

    private void OpenOptions()
    {
        _optionsOpen = true;
        _context.States.Push(new OptionsState(_context));
    }
    public void Exit() => _context.SetMouseVisible(false);

    public void Update(GameTime gameTime)
    {
        if (_context.Input.IsKeyPressed(Keys.Escape))
        {
            _context.States.Pop();
            return;
        }

        _menu.Update();
    }

    public void Draw(GameTime gameTime)
    {
        var viewport = _context.GraphicsDevice.Viewport;
        _context.SpriteBatch.Begin();
        _context.SpriteBatch.Draw(_pixel, viewport.Bounds, Color.Black * 0.65f);
        _context.SpriteBatch.End();
        if (!_optionsOpen) _menu.Draw();
    }
}
