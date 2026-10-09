using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class CaughtState : IGameState
{
    private readonly GameStateContext _context;
    private readonly PlayingState _playing;
    private readonly MenuScreen _menu;

    public bool DrawsStatesBelow => true;

    public CaughtState(GameStateContext context, PlayingState playing, Func<IGameState> restart, Func<IGameState> mainMenu)
    {
        _context = context;
        _playing = playing;
        _menu = new MenuScreen(context, new[]
        {
            ("Restart", () => context.States.Replace(restart())),
            ("Main Menu", () =>
            {
                context.States.Clear();
                context.States.Push(mainMenu());
            }),
            ("Quit", context.Quit)
        });
    }

    public void Enter() => _context.SetMouseVisible(true);
    public void Exit() { }

    public void Update(GameTime gameTime) => _menu.Update();

    public void Draw(GameTime gameTime)
    {
        _playing.DrawCaughtOverlay(gameTime);
        _menu.Draw();
    }
}
