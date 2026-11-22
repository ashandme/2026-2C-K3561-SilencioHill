using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class MainMenuState : IGameState
{
    private readonly GameStateContext _context;
    private readonly MenuScreen _menu;
    private readonly MenuBackground _background;
    private readonly Texture2D _pixel;
    private readonly Func<IGameState> _createPlaying;
    private bool _resolutionChanged;   // the screen size changed in options: rebuild this screen on return
    private bool _optionsOpen;   // options screen on top: keep the background loaded and hide our menu

    public bool DrawsStatesBelow => false;

    public MainMenuState(GameStateContext context, Func<IGameState> createPlaying)
    {
        _context = context;
        _createPlaying = createPlaying;
        _background = new MenuBackground(context);
        _pixel = new Texture2D(context.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _menu = new MenuScreen(context, new[]
        {
            ("Play", () => context.States.Replace(createPlaying())),
            ("Debug", () => context.States.Replace(new PlayingState(context, debugEnabled: true))),
            ("Opciones", OpenOptions),
            ("Quit", context.Quit)
        });
    }

    public void Enter()
    {
        _optionsOpen = false;
        _context.SetMouseVisible(true);
        SoundManager.PlayMusic("Menu");
        // Background and layout were built for the previous resolution: start over with a fresh menu.
        if (_resolutionChanged) _context.States.Replace(new MainMenuState(_context, _createPlaying));
    }

    public void Exit()
    {
        if (!_optionsOpen) _background.Unload();
    }

    private void OpenOptions()
    {
        _optionsOpen = true;
        _context.States.Push(new OptionsState(_context, allowResolution: true, onResolutionChanged: () => _resolutionChanged = true));
    }

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
        var titleScale = 2.5f * UiScale.Of(viewport);
        var size = _context.Font.MeasureString(title) * titleScale;

        batch.Begin();
        batch.Draw(_pixel, viewport.Bounds, Color.Black * 0.35f);
        batch.DrawString(_context.Font, title,
            new Vector2((viewport.Width - size.X) * 0.5f, viewport.Height * 0.18f),
            Color.DarkRed, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
        batch.End();

        if (!_optionsOpen) _menu.Draw();
    }
}