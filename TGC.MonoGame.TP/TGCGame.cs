using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.LevelUtils;
using TGC.MonoGame.TP.PropUtils;

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

    private Camera ActiveCamera => _playerMode ? (Camera)_player : (Camera)_spectator;

    private LevelManager _levelManager;
    private Effect _effect;
    private Matrix _projection;
    private SpriteBatch _spriteBatch;
    private Matrix _world;

    private SpriteFont _font;

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
        _levelManager = new LevelManager();
        var mapGen = new Map();
        var mapOutside = new MapJson { filePath = "Content/props.json" };
        var mapInside = new MapJson { filePath = "Content/insideprops.json" };
        _levelManager.AddLevel("outside", new Level("outside", [mapGen, mapOutside]));
        _levelManager.AddLevel("inside", new Level("inside", [mapInside]));

        var screenCenter = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
        _player = new Player(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 10, 50), screenCenter);
        _spectator = new FreeCamera(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 50, 150), screenCenter);

        _input = new InputManager();
        _player.SetInput(_input);

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
        _effect = Content.Load<Effect>(ContentFolderEffects + "BlinnPhong");

        // Load the initial level using LevelManager
        _levelManager.LoadLevel("outside", Content);

        // Interaction manager uses LevelManager's GetActiveProps/RemoveActiveProp
        // _interactionManager = new InteractionManager(useInside => _levelManager.GetActiveProps(), (prop, useInside));

        // Keep reference to current enemy via level manager
        _enemy = _levelManager.Current?.Enemy;

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Failed to load FlashlightTexture: " + ex.Message);
            flashlightTexture = null;
        }

        try
        {
            basicTextureEffect = Content.Load<Effect>(ContentFolderEffects + "BasicTexture");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Failed to load BasicTexture effect: " + ex.Message);
            basicTextureEffect = null;
        }

        var startingCandle = new CandleItem(candleModel);
        var startingLinterna = new FlashlightItem(linternaModel, flashlightTexture, basicTextureEffect);
        _player.PickupItem(startingLinterna);
        _player.PickupItem(startingCandle);
        // CARGAR ENEMIGO
        var enemyModel = Content.Load<Model>(ContentFolder3D + "Assets/ghost");// ajusta el path al modelo que tengas
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

        // F1: toggle level between outside and inside
        if (_input.IsKeyPressed(Keys.F1))
        {
            try
            {
                var next = _levelManager.CurrentName == "outside" ? "inside" : "outside";
                _levelManager.LoadLevel(next, Content);
                // update enemy reference
                _enemy = _levelManager.Current?.Enemy;
            }
            catch (Exception ex)
            {
                _hud.SetStatus("Level switch ERROR: " + ex.Message, ReloadStatusSecondsError);
            }
        }

        // F2: reload current level and set HUD status
        if (_input.IsKeyPressed(Keys.F2))
        {
            try
            {
                _levelManager.ReloadCurrent(Content);
                _hud.SetStatus("Reload OK", ReloadStatusSecondsOk);
                // refresh enemy reference
                _enemy = _levelManager.Current?.Enemy;
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
        // Update SceneLighting from player's current item (no exceptions should propagate)
        var current = _player.CurrentItem;
        if (current is FlashlightItem flashlight && flashlight.IsOn && flashlight.AttachToCamera)
        {
            SceneLighting.FlashlightEnabled = true;
            SceneLighting.LightPosition = _player.Position + _player.FrontDirection * 4f;
            _effect.Parameters["lightPosition"].SetValue(SceneLighting.LightPosition);
            _effect.Parameters["eyePosition"].SetValue(_player.Position);

        }
        else
        {
            SceneLighting.FlashlightEnabled = false;
        }
        // Item draining moved into Player.UpdateItems
        _enemy.Update(gameTime, _player);
        // Let HUD manage its own timer
        _hud.Update(gameTime);

        // Interaction: handle interact key in Update using screen-space ray (viewport unproject)
        if (_playerMode && _input.IsInteractPressed())
        {
            var vp = GraphicsDevice.Viewport;
            var cx = vp.Width / 2f;
            var cy = vp.Height / 2f;

            var nearPoint = vp.Unproject(new Vector3(cx, cy, 0f), _projection, _player.View, Matrix.Identity);
            var farPoint = vp.Unproject(new Vector3(cx, cy, 1f), _projection, _player.View, Matrix.Identity);
            var dir = Vector3.Normalize(farPoint - nearPoint);
            var ray = new Ray(nearPoint, dir);

            //var target = _interactionManager.FindInteractiveProp(ray, _levelManager.Current?.IsInside ?? false);
            //if (target != null)
            //{
            //    _interactionManager.Interact(target, _player, _levelManager.Current?.IsInside ?? false);
            //}
        }

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
        var activeView = ActiveCamera.View;
        // _effect.Parameters["View"].SetValue(activeView);
        // _effect.Parameters["Projection"].SetValue(_projection);
        // _effect.Parameters["DiffuseColor"].SetValue(Color.DarkBlue.ToVector3());
        var camPos = Matrix.Invert(activeView).Translation;
        SceneLighting.EyePosition = camPos;

        var camText = string.Format("Camera: X={0:F2} Y={1:F2} Z={2:F2}\nF1: Switch Interior/Exterior | F3: {3}",
            camPos.X, camPos.Y, camPos.Z, _playerMode ? "Player" : "Spectator");
        var distanceToEnemy = (_enemy.Position - _player.Position).Length();
        camText += $"\nEnemy: {_enemy.State} | Dist: {distanceToEnemy:F1} | Angle: {_enemy.DebugAngleToPlayerDegrees(_player):F1}";

        // Draw current level via LevelManager
        if (_levelManager.Current != null)
        {
            _levelManager.Current.Draw(activeView, _projection);
        }
        else
        {
            // Fallback: draw nothing
        }

        // Let HUD build display strings from live entities
        // ActiveCamera is a Cameras.Camera; pass actual camera instance
        _hud.UpdateState(_player, _enemy, ActiveCamera, _playerMode);
        _hud.Draw();

        // Interactions are handled in Update; Draw must not mutate game state.

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