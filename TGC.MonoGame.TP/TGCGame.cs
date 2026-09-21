using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;

namespace TGC.MonoGame.TP;

/// <summary>
///     Esta es la clase principal del juego.
/// </summary>
public class TGCGame : Game
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    private readonly GraphicsDeviceManager _graphics;

    private FreeCamera _spectatorCam;
    private Player _player;
    private bool _playerMode = false; // false = espectador (default), true = player
    private Camera ActiveCamera => _playerMode ? (Camera)_player : _spectatorCam;

    private Map _tilemap;
    private MapJson _propmap;
    private MapJson _insidepropmap;
    private Effect _effect;
    private Matrix _projection;
    private SpriteBatch _spriteBatch;
    private Matrix _world;

    private SpriteFont _font;
    private bool _showInsideOnly = false;

    // HUD helper (manages its own status timer)
    private HudRenderer _hud;

    // Input manager centralizes keyboard handling
    private InputManager _input;

    private const int BackBufferMargin = 100;
    private static readonly Color ClearColor = new Color(0.1f, 0.0f, 0.3f);
    private const double ReloadStatusSecondsOk = 2.0;
    private const double ReloadStatusSecondsError = 5.0;

    public TGCGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - BackBufferMargin;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - BackBufferMargin;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
    ///     Escribir aqui el codigo de inicializacion: el procesamiento que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void Initialize()
    {
        var rasterizerState = new RasterizerState { CullMode = CullMode.None };
        GraphicsDevice.RasterizerState = rasterizerState;

        _world = Matrix.Identity;
        _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, 2000);
        _tilemap = new Map();
        _propmap = new MapJson();
        _insidepropmap = new MapJson();

        var screenCenter = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
        _spectatorCam = new FreeCamera(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 10, 50), screenCenter);
        _player = new Player(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 10, 50), screenCenter);

        _input = new InputManager();

        base.Initialize();
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo, despues de Initialize.
    ///     Escribir aqui el codigo de inicializacion: cargar modelos, texturas, estructuras de optimizacion, el procesamiento
    ///     que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader");

        _tilemap.LoadContent(Content);
        _propmap.LoadFromJson("Content/props.json", Content);
        _insidepropmap.LoadFromJson("Content/insideprops.json", Content);

        _font = Content.Load<SpriteFont>(ContentFolderSpriteFonts + "CascadiaCode/CascadiaCodePL");

        // create HUD helper (uses shared SpriteBatch and font)
        _hud = new HudRenderer(_spriteBatch, _font, GraphicsDevice);

        base.LoadContent();
    }

    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // Update input first
        _input.Update(gameTime);

        if (_input.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // F1: toggle drawing mode (edge-press)
        if (_input.IsKeyPressed(Keys.F1))
        {
            _showInsideOnly = !_showInsideOnly;
        }

        // F2: reload JSON maps and set HUD status
        if (_input.IsKeyPressed(Keys.F2))
        {
            try
            {
                _propmap.LoadFromJson("Content/props.json", Content);
                _insidepropmap.LoadFromJson("Content/insideprops.json", Content);
                _hud.SetStatus("Reload OK", ReloadStatusSecondsOk);
            }
            catch (Exception ex)
            {
                _hud.SetStatus("Reload ERROR: " + ex.Message, ReloadStatusSecondsError);
            }
        }

        // F3: toggle modo espectador / player (edge-press)
        if (_input.IsKeyPressed(Keys.F3))
        {
            _playerMode = !_playerMode;
        }

        ActiveCamera.Update(gameTime);

        // Let HUD manage its own timer
        _hud.Update(gameTime);

        base.Update(gameTime);
    }

    /// <summary>
    ///     Se llama cada vez que hay que refrescar la pantalla.
    ///     Escribir aqui el codigo referido al renderizado.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, ClearColor, 1.0f, 0);

        _effect.Parameters["View"].SetValue(ActiveCamera.View);
        _effect.Parameters["Projection"].SetValue(_projection);
        _effect.Parameters["DiffuseColor"].SetValue(Color.DarkBlue.ToVector3());

        var camPos = Matrix.Invert(ActiveCamera.View).Translation;
        var camText = string.Format("Camera: X={0:F2} Y={1:F2} Z={2:F2}\nF1: Switch Interior/Exterior | F3: {3}",
            camPos.X, camPos.Y, camPos.Z, _playerMode ? "Player" : "Espectador");

        if (!_showInsideOnly)
        {
            _tilemap.Draw(ActiveCamera.View, _projection);
            _propmap.Draw(ActiveCamera.View, _projection);
        }
        else
        {
            _insidepropmap.DrawWireframe(GraphicsDevice, ActiveCamera.View, _projection);
        }

        // HUD draws camera info on left and status (or READY) on right
        _hud.Draw(camText);
    }

    /// <summary>
    ///     Libero los recursos que se cargaron en el juego.
    /// </summary>
    protected override void UnloadContent()
    {
        Content.Unload();
        base.UnloadContent();
    }
}