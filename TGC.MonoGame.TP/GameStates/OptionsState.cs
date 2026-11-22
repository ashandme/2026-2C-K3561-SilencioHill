using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.GameStates;

// Options screen, pushed on top of the main menu or the pause menu.
// Left/Right adjusts the selected value; Esc or "Volver" goes back.
internal sealed class OptionsState : IGameState
{
    private const int VolumeStep = 5;
    private const float TitleScale = 1.8f;
    private const float TitleY = 0.2f;   // fraction of the viewport height

    private readonly GameStateContext _context;
    private readonly MenuScreen _menu;
    private readonly Texture2D _pixel;

    public bool DrawsStatesBelow => true;

    // allowResolution: the resolution can only change outside a match (render targets are sized at creation).
    // onResolutionChanged runs after the resolution changes so the caller can rebuild its screen.
    public OptionsState(GameStateContext context, bool allowResolution = false, Action? onResolutionChanged = null)
    {
        _context = context;
        _pixel = new Texture2D(context.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        var items = new List<MenuItem>
        {
            new MenuItem(
                () => $"Sonido: < {SoundManager.Volume} >",
                null,
                direction => SoundManager.Volume += direction * VolumeStep)
        };
        items.Add(new MenuItem(
            () => $"Pantalla: < {(DisplaySettings.IsFullScreen ? "Completa" : "Ventana")} >",
            DisplaySettings.ToggleFullScreen,
            _ => DisplaySettings.ToggleFullScreen()));
        if (allowResolution)
        {
            items.Add(new MenuItem(
                () => $"Resolucion: < {DisplaySettings.Current.Label} >",
                null,
                direction =>
                {
                    DisplaySettings.Step(direction);
                    onResolutionChanged?.Invoke();
                }));
        }
        items.Add(new MenuItem(() => "Volver", () => context.States.Pop()));
        _menu = new MenuScreen(context, items);
    }

    public void Enter() => _context.SetMouseVisible(true);
    public void Exit() { }

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
        var scale = UiScale.Of(viewport) * TitleScale;
        const string title = "OPCIONES";
        var size = _context.Font.MeasureString(title) * scale;

        _context.SpriteBatch.Begin();
        _context.SpriteBatch.Draw(_pixel, viewport.Bounds, Color.Black * 0.85f);
        _context.SpriteBatch.DrawString(_context.Font, title,
            new Vector2((viewport.Width - size.X) * 0.5f, viewport.Height * TitleY),
            Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _context.SpriteBatch.End();
        _menu.Draw();
    }
}
