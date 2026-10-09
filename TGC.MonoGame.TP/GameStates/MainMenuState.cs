using Microsoft.Xna.Framework;
using System;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class MainMenuState : IGameState
{
    private readonly GameStateContext _context;
    private readonly MenuScreen _menu;

    public bool DrawsStatesBelow => false;

    public MainMenuState(GameStateContext context, Func<IGameState> createPlaying)
    {
        _context = context;
        _menu = new MenuScreen(context, new[]
        {
            ("Play", () => context.States.Replace(createPlaying())),
            ("Debug", () => context.States.Replace(new PlayingState(context, debugEnabled: true))),
            ("Quit", context.Quit)
        });
    }

    public void Enter() => _context.SetMouseVisible(true);
    public void Exit() { }
    public void Update(GameTime gameTime) => _menu.Update();
    public void Draw(GameTime gameTime) => _menu.Draw();
}
