using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.GameStates;

namespace TGC.MonoGame.TP;

public class TGCGame : Game
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderTextures = "Textures/";

    private readonly GraphicsDeviceManager _graphics;
    private InputManager _input;
    private GameStateManager _states;

    public TGCGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GraphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None };
        _input = new InputManager();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);
        var font = Content.Load<SpriteFont>("SpriteFonts/CascadiaCode/CascadiaCodePL");
        _states = new GameStateManager();
        var context = new GameStateContext(
            GraphicsDevice,
            Content,
            _input,
            spriteBatch,
            font,
            _states,
            Exit,
            visible => IsMouseVisible = visible);

        _states.Push(new MainMenuState(context, () => new PlayingState(context)));
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(gameTime);
        _states.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _states.Draw(gameTime);
        base.Draw(gameTime);
    }
}
