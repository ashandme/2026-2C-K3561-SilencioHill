using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class EndState : IGameState
{
    private readonly GameStateContext _context;
    private readonly MenuScreen _menu;
    private readonly Texture2D _pixel;
    private readonly string _title;
    private readonly string _subtitle;
    private readonly Color _titleColor;

    public bool DrawsStatesBelow => true;

    public EndState(GameStateContext context, bool won, string subtitle, Func<IGameState> restart, Func<IGameState> mainMenu)
    {
        _context = context;
        _title = won ? "GANASTE" : "SE ACABO EL TIEMPO";
        _subtitle = subtitle;
        _titleColor = won ? Color.LimeGreen : Color.Red;
        _pixel = new Texture2D(context.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
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
        var viewport = _context.GraphicsDevice.Viewport;
        var spriteBatch = _context.SpriteBatch;
        var font = _context.Font;

        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, viewport.Bounds, Color.Black * 0.7f);

        var titleSize = font.MeasureString(_title) * 2f;
        spriteBatch.DrawString(font, _title,
            new Vector2((viewport.Width - titleSize.X) * 0.5f, viewport.Height * 0.2f),
            _titleColor, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);

        var subSize = font.MeasureString(_subtitle);
        spriteBatch.DrawString(font, _subtitle,
            new Vector2((viewport.Width - subSize.X) * 0.5f, viewport.Height * 0.2f + titleSize.Y + 10f),
            Color.White);
        spriteBatch.End();

        _menu.Draw();
    }
}