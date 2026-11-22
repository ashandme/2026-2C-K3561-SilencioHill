using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.GameStates;

// A menu entry. OnSelect runs on Enter; OnAdjust (optional) runs on Left/Right with -1 / +1.
internal sealed class MenuItem
{
    public Func<string> Label { get; }
    public Action? OnSelect { get; }
    public Action<int>? OnAdjust { get; }

    public MenuItem(Func<string> label, Action? onSelect, Action<int>? onAdjust = null)
    {
        Label = label;
        OnSelect = onSelect;
        OnAdjust = onAdjust;
    }
}

internal sealed class MenuScreen
{
    // Sizes in reference pixels (1280x720); multiplied by UiScale at draw time.
    private const float ItemSpacing = 42f;

    private readonly InputManager _input;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly List<MenuItem> _items;
    private int _selected;

    public MenuScreen(GameStateContext context, IEnumerable<(string Label, Action Action)> items)
        : this(context, items.Select(i => new MenuItem(() => i.Label, i.Action)))
    {
    }

    public MenuScreen(GameStateContext context, IEnumerable<MenuItem> items)
    {
        _input = context.Input;
        _spriteBatch = context.SpriteBatch;
        _font = context.Font;
        _items = new List<MenuItem>(items);
    }

    public void Update()
    {
        if (_items.Count == 0) return;

        if (_input.IsKeyPressed(Keys.Up))
            _selected = (_selected + _items.Count - 1) % _items.Count;
        else if (_input.IsKeyPressed(Keys.Down))
            _selected = (_selected + 1) % _items.Count;
        else if (_input.IsKeyPressed(Keys.Left))
            _items[_selected].OnAdjust?.Invoke(-1);
        else if (_input.IsKeyPressed(Keys.Right))
            _items[_selected].OnAdjust?.Invoke(1);
        else if (_input.IsKeyPressed(Keys.Enter))
            _items[_selected].OnSelect?.Invoke();
    }

    public void Draw()
    {
        if (_items.Count == 0) return;

        var viewport = _spriteBatch.GraphicsDevice.Viewport;
        var scale = UiScale.Of(viewport);
        var spacing = ItemSpacing * scale;
        var y = (viewport.Height - _items.Count * spacing) * 0.5f;

        _spriteBatch.Begin();
        for (var i = 0; i < _items.Count; i++)
        {
            var text = _items[i].Label();
            var size = _font.MeasureString(text) * scale;
            var position = new Vector2((viewport.Width - size.X) * 0.5f, y + i * spacing);
            var color = i == _selected ? Color.Yellow : Color.White;
            _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        _spriteBatch.End();
    }
}
