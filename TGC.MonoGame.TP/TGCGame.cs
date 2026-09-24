using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
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

    // Replace FreeCamera with Player so player inventories can be used
    private Player _player;
    private Enemy _enemy;
    private bool _playerMode = true; // true = player, false = spectator (default was spectator before)
    private FreeCamera _spectator;

    private Camera ActiveCamera => _playerMode ? (Camera)_player : (Camera)_player;

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
        _player = new Player(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 10, 50), screenCenter);
        _spectator = new FreeCamera(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 50, 150), screenCenter);

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

        // CARGAR ITEMS
        var candleModel = Content.Load<Model>(ContentFolder3D + "Assets/Candle");
        var linternaModel = Content.Load<Model>(ContentFolder3D + "Assets/Linterna");
        // Load flashlight texture and the BasicTexture effect used to draw it
        // TODO: REFACTORIZAR PARA QUE EL ITEM FLASHLIGHT TENGA SU PROPIO EFFECT Y TEXTURE, Y NO DEPENDA DEL JUEGO
        Texture2D flashlightTexture = null;
        Effect basicTextureEffect = null;
        try
        {
            flashlightTexture = Content.Load<Texture2D>(ContentFolderTextures + "FlashlightTexture");
        }
        catch (Exception)
        {
            // ignore: texture optional
        }

        try
        {
            basicTextureEffect = Content.Load<Effect>(ContentFolderEffects + "BasicTexture");
        }
        catch (Exception)
        {
            // ignore: effect optional
        }

        var startingCandle = new CandleItem(candleModel);
        var startingLinterna = new FlashlightItem(linternaModel, flashlightTexture, basicTextureEffect);
        _player.PickupItem(startingLinterna);
        _player.PickupItem(startingCandle);

        // CARGAR ENEMIGO
        var enemyModel = Content.Load<Model>(ContentFolder3D + "kenney_retro-urban-kit/detail-dumpster-closed"); // ajusta el path al modelo que tengas
        _enemy = new Enemy(enemyModel, _effect, "Content/enemyRoute.json");   
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

        // Update the active camera: player or spectator
        if (_playerMode)
        {
            _player.Update(gameTime);
            // Delegate item draining to the player
            _player.UpdateItems(gameTime);
        }
        else
        {
            _spectator.Update(gameTime);
        }
        // Update scene lighting based on player's flashlight state
        try
        {
            var current = _player.CurrentItem;
            if (current is FlashlightItem flashlight && flashlight.IsOn && flashlight.AttachToCamera)
            {
                SceneLighting.FlashlightEnabled = true;
                SceneLighting.LightPosition = _player.Position + _player.FrontDirection * 4f;
            }
            else
            {
                SceneLighting.FlashlightEnabled = false;
            }
        }
        catch
        {
            SceneLighting.FlashlightEnabled = false;
        }
        // Item draining moved into Player.UpdateItems
        _enemy.Update(gameTime, _player);
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
        // 1) Clear color + depth and draw opaque scene normally
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, ClearColor, 1.0f, 0);

        // Use active camera for world rendering (player or spectator)
        var activeView = _playerMode ? _player.View : _spectator.View;
        _effect.Parameters["View"].SetValue(activeView);
        _effect.Parameters["Projection"].SetValue(_projection);
        _effect.Parameters["DiffuseColor"].SetValue(Color.DarkBlue.ToVector3());
        // Compute camera (eye) position once and expose to SceneLighting to avoid per-prop inversion
        var camPos = Matrix.Invert(activeView).Translation;
        SceneLighting.EyePosition = camPos;
        var camText = string.Format("Camera: X={0:F2} Y={1:F2} Z={2:F2}\nF1: Switch Interior/Exterior | F3: {3}", 
            camPos.X, camPos.Y, camPos.Z, _playerMode ? "Player" : "Spectator");
        var distanceToEnemy = (_enemy.Position - _player.Position).Length();
        camText += $"\nEnemy: {_enemy.State} | Dist: {distanceToEnemy:F1} | Angle: {_enemy.DebugAngleToPlayerDegrees(_player):F1}";

        if (!_showInsideOnly)
        {
            _tilemap.Draw(activeView, _projection);
            _propmap.Draw(activeView, _projection);
            _enemy.Draw(activeView, _projection);
        }
        else
        {
            _insidepropmap.Draw(activeView, _projection);
        }

        _hud.Draw(camText);

        // Build right-side HUD overlay with item statuses
        try
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var it in _player.Inventory)
            {
                if (it is FlashlightItem f)
                {
                    var state = f.IsOn ? "ON" : "OFF";
                    var time = System.TimeSpan.FromSeconds(f.RemainingSeconds).ToString(@"mm\:ss");
                    lines.Add($"Flashlight: {state}  {time}");
                }
                else if (it is CandleItem c)
                {
                    var state = c.IsLit ? "ON" : "OFF";
                    var time = System.TimeSpan.FromSeconds(c.RemainingSeconds).ToString(@"mm\:ss");
                    lines.Add($"Candle: {state}  {time}");
                }
            }

            _hud.RightOverlay = string.Join("\n", lines);
        }
        catch { _hud.RightOverlay = null; }

        // 2) Draw held item on top: clear only depth buffer so the held item is not occluded by scene geometry
        GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);

        // DIBUJAR EL ITEM
        if (_playerMode)
        {
            _player.DrawHeldItem(_effect, _projection, GraphicsDevice);
        }

        base.Draw(gameTime);
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