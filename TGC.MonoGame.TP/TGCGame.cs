using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.Samples.Cameras;

namespace TGC.MonoGame.TP;

/// <summary>
///     Esta es la clase principal del juego.
///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
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

    private FreeCamera cam;
    private Map _tilemap;
    private MapJson _propmap;
    private MapJson _insidepropmap;
    private Effect _effect;
    //private Model _model;
    private Matrix _projection;
    private SpriteBatch _spriteBatch;
    //private Matrix _view;
    private Matrix _world;

    // New: font for drawing camera position
    private SpriteFont _font;
    // PARA MOSTRAR SOLO LOS PROPS DE INTERIOR (X) O TODO (default)
    private bool _showInsideOnly = false;

    // New: previous keyboard state to detect key presses (edge)
    private KeyboardState _previousKeyboardState;

    // Añadir campos para estado de recarga (en la clase)
    private string _reloadStatus = "";
    private double _reloadStatusTimer = 0;

    /// <summary>
    ///     Constructor del juego.
    /// </summary>
    public TGCGame()
    {
        // Maneja la configuracion y la administracion del dispositivo grafico.
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;

        // Para que el juego sea pantalla completa se puede usar Graphics IsFullScreen.
        // Carpeta raiz donde va a estar toda la Media.
        Content.RootDirectory = "Content";
        // Hace que el mouse sea visible.
        IsMouseVisible = true;
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
    ///     Escribir aqui el codigo de inicializacion: el procesamiento que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void Initialize()
    {
        // La logica de inicializacion que no depende del contenido se recomienda poner en este metodo.

        // Apago el backface culling.
        // Esto se hace por un problema en el diseno del modelo del logo de la materia.
        // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
        GraphicsDevice.RasterizerState = rasterizerState;
        // Seria hasta aca.

        // Configuramos nuestras matrices de la escena.
        _world = Matrix.Identity;
        //_view = Matrix.CreateLookAt(Vector3.UnitZ * 150, Vector3.Zero, Vector3.Up);
        _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, 2000);
        _tilemap = new Map();
        _propmap = new MapJson();
        _insidepropmap = new MapJson(); // <-- inicializar para evitar null reference
        var screenCenter = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
        cam = new FreeCamera(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 10, 50), screenCenter);
        //cam.FarPlane = 15100.0f;

        // Inicializar estado previo del teclado para detectar pulsaciones.
        _previousKeyboardState = Keyboard.GetState();

        base.Initialize();
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo, despues de Initialize.
    ///     Escribir aqui el codigo de inicializacion: cargar modelos, texturas, estructuras de optimizacion, el procesamiento
    ///     que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void LoadContent()
    {
        // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Cargo el modelo del logo.
        //_model = Content.Load<Model>(ContentFolder3D + "tgc-logo/tgc-logo");

        // Cargo un efecto basico propio declarado en el Content pipeline.
        // En el juego no pueden usar BasicEffect de MG, deben usar siempre efectos propios.
        _effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader");

        // Asigno el efecto que cargue a cada parte del mesh.
        // Un modelo puede tener mas de 1 mesh internamente.
        /*foreach (var mesh in _model.Meshes)
        {
            // Un mesh puede tener mas de 1 mesh part (cada 1 puede tener su propio efecto).
            foreach (var meshPart in mesh.MeshParts)
            {
                meshPart.Effect = _effect;
            }
        }*/
        _tilemap.LoadContent(Content);
        _propmap.LoadFromJson("Content/props.json", Content);
        _insidepropmap.LoadFromJson("Content/insideprops.json", Content);
        // Load a SpriteFont to draw the camera position.
        // Ensure a SpriteFont named "DefaultFont.spritefont" exists under Content/SpriteFonts.
        _font = Content.Load<SpriteFont>(ContentFolderSpriteFonts + "CascadiaCode/CascadiaCodePL");

        base.LoadContent();
    }

    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // Aca deveriamos poner toda la logica de actualizacion del juego.

        // Capturar Input teclado
        var ks = Keyboard.GetState();
        if (ks.IsKeyDown(Keys.Escape))
        {
            //Salgo del juego.
            Exit();
        }
        // F1: SWITCH DIBUJAR EXTERIOR/INTERIOR
        if (ks.IsKeyDown(Keys.F1) && !_previousKeyboardState.IsKeyDown(Keys.F1))
        {
            _showInsideOnly = !_showInsideOnly;
        }

        // F2: PARA RECARGAR LOS JSON (EN VS NO FUNCA)
        if (ks.IsKeyDown(Keys.F2) && !_previousKeyboardState.IsKeyDown(Keys.F2))
        {
            try
            {
                _propmap.LoadFromJson("Content/props.json", Content);
                _insidepropmap.LoadFromJson("Content/insideprops.json", Content);
                _reloadStatus = "Reload OK";
                _reloadStatusTimer = 2.0; // mostrar 2 segundos
            }
            catch (Exception ex)
            {
                _reloadStatus = "Reload ERROR: " + ex.Message;
                _reloadStatusTimer = 5.0;
            }
        }
        if (_reloadStatusTimer > 0)
        {
            _reloadStatusTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_reloadStatusTimer <= 0) _reloadStatus = "";
        }

        cam.Update(gameTime);
        // Actualizar el estado previo del teclado al final del Update.
        _previousKeyboardState = ks;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Se llama cada vez que hay que refrescar la pantalla.
    ///     Escribir aqui el codigo referido al renderizado.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // Limpiar color y depth buffer
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Color(0.1f,0.0f,0.3f), 1.0f, 0);

        // Pasar parametros al shader
        _effect.Parameters["View"].SetValue(cam.View);
        _effect.Parameters["Projection"].SetValue(_projection);
        _effect.Parameters["DiffuseColor"].SetValue(Color.DarkBlue.ToVector3());

        // Compute camera world position by inverting the view matrix.
        var camPos = Matrix.Invert(cam.View).Translation;
        var camText = string.Format("Camera: X={0:F2} Y={1:F2} Z={2:F2}", camPos.X, camPos.Y, camPos.Z);

        // CAMBIA INTERIOR/EXTERIOR
        if (!_showInsideOnly)
        {
            _tilemap.Draw(cam.View, _projection);
            _propmap.Draw(cam.View, _projection);
        }
        else
        {
            _insidepropmap.DrawWireframe(GraphicsDevice, cam.View, _projection);
        }
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _spriteBatch.DrawString(_font, camText, new Vector2(10f, 10f), Color.White);
        if (!string.IsNullOrEmpty(_reloadStatus))
        {
            _spriteBatch.DrawString(_font, _reloadStatus, new Vector2(10f, 30f), Color.Yellow);
        }
        _spriteBatch.End();

        // Restaurar el Depth/Stencil para que el Z-buffer funcione correctamente tras usar SpriteBatch
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
    }

    /// <summary>
    ///     Libero los recursos que se cargaron en el juego.
    /// </summary>
    protected override void UnloadContent()
    {
        // Libero los recursos.
        Content.Unload();

        base.UnloadContent();
    }
}