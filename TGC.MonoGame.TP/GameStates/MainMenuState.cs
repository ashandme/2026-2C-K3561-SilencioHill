using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class MainMenuState : IGameState
{
    private readonly GameStateContext _context;
    private readonly MenuScreen _menu;
    private readonly MenuBackground _background;
    private readonly Texture2D _pixel;

    public bool DrawsStatesBelow => false;

    public MainMenuState(GameStateContext context, Func<IGameState> createPlaying)
    {
        _context = context;
        _background = new MenuBackground(context);
        _pixel = new Texture2D(context.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _menu = new MenuScreen(context, new[]
        {
            ("Play", () => context.States.Replace(createPlaying())),
            ("Debug", () => context.States.Replace(new PlayingState(context, debugEnabled: true))),
            ("Quit", context.Quit)
        });
    }

    public void Enter() => _context.SetMouseVisible(true);
    public void Exit() => _background.Unload();

    public void Update(GameTime gameTime)
    {
        _background.Update(gameTime);
        _menu.Update();
    }

    public void Draw(GameTime gameTime)
    {
        _background.Draw();

        var viewport = _context.GraphicsDevice.Viewport;
        var batch = _context.SpriteBatch;
        const string title = "SILENCIO HILL";
        var size = _context.Font.MeasureString(title) * 2.5f;

        batch.Begin();
        batch.Draw(_pixel, viewport.Bounds, Color.Black * 0.35f);
        batch.DrawString(_context.Font, title,
            new Vector2((viewport.Width - size.X) * 0.5f, viewport.Height * 0.18f),
            Color.DarkRed, 0f, Vector2.Zero, 2.5f, SpriteEffects.None, 0f);
        batch.End();

        _menu.Draw();
    }
}