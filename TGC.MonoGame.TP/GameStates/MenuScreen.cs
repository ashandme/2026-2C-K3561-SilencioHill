using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class MenuScreen
{
    private readonly InputManager _input;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly List<(string Label, Action Action)> _items;
    private readonly Texture2D _pixel;
    private int _selected;

    public MenuScreen(GameStateContext context, IEnumerable<(string Label, Action Action)> items)
    {
        _input = context.Input;
        _spriteBatch = context.SpriteBatch;
        _font = context.Font;
        _items = new List<(string Label, Action Action)>(items);
        _pixel = new Texture2D(context.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Update()
    {
        if (_items.Count == 0) return;

        if (_input.IsKeyPressed(Keys.Up))
            _selected = (_selected + _items.Count - 1) % _items.Count;
        else if (_input.IsKeyPressed(Keys.Down))
            _selected = (_selected + 1) % _items.Count;
        else if (_input.IsKeyPressed(Keys.Enter))
            _items[_selected].Action();
    }

    public void Draw()
    {
        if (_items.Count == 0) return;

        var viewport = _spriteBatch.GraphicsDevice.Viewport;
        var totalHeight = _items.Count * 42f;
        var y = (viewport.Height - totalHeight) * 0.5f;

        _spriteBatch.Begin();
        for (var i = 0; i < _items.Count; i++)
        {
            var text = _items[i].Label;
            var size = _font.MeasureString(text);
            var position = new Vector2((viewport.Width - size.X) * 0.5f, y + i * 42f);
            var color = i == _selected ? Color.Yellow : Color.White;
            _spriteBatch.DrawString(_font, text, position, color);
        }
        _spriteBatch.End();
    }
}
