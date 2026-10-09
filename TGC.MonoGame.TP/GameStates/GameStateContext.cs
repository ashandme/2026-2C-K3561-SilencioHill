using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class GameStateContext
{
    public GraphicsDevice GraphicsDevice { get; }
    public ContentManager Content { get; }
    public InputManager Input { get; }
    public SpriteBatch SpriteBatch { get; }
    public SpriteFont Font { get; }
    public GameStateManager States { get; }
    public Action Quit { get; }
    public Action<bool> SetMouseVisible { get; }

    public GameStateContext(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        InputManager input,
        SpriteBatch spriteBatch,
        SpriteFont font,
        GameStateManager states,
        Action quit,
        Action<bool> setMouseVisible)
    {
        GraphicsDevice = graphicsDevice;
        Content = content;
        Input = input;
        SpriteBatch = spriteBatch;
        Font = font;
        States = states;
        Quit = quit;
        SetMouseVisible = setMouseVisible;
    }
}
