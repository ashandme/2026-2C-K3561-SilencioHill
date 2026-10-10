using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TGC.MonoGame.TP.Entities;
using TGC.MonoGame.TP.LevelUtils;

namespace TGC.MonoGame.TP.GameStates;

// Fondo 3D del menú principal: un interior con el fantasma patrullando
internal sealed class MenuBackground
{
    // TUNEAR: desde dónde y hacia dónde mira la cámara
    private static readonly Vector3 CameraBase = new(146f, 30f, 157f);
    private static readonly Vector3 LookTarget = new(145f, 20f, 700f);
    private static readonly RasterizerState NoCull = new() { CullMode = CullMode.None };

    private readonly GraphicsDevice _graphics;
    private readonly Level _level;
    private readonly Player _dummyPlayer;   // solo para que el fantasma tenga a quién "mirar"
    private readonly Matrix _projection;
    private float _time;

    public MenuBackground(GameStateContext context)
    {
        _graphics = context.GraphicsDevice;
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4, _graphics.Viewport.AspectRatio, 1, 2000);

        _level = new Level("menu-inside",
            [new MapJson { filePath = "Content/insideprops.json" }],
            "Content/menuRoute.json");
        _level.LoadLevelContent(context.Content);
        _level.Enemy?.SetScale(Vector3.One * 0.25f);

        // Jugador falso lejísimos: el fantasma se queda siempre en Roaming
        var center = new Point(_graphics.Viewport.Width / 2, _graphics.Viewport.Height / 2);
        _dummyPlayer = new Player(_graphics.Viewport.AspectRatio, new Vector3(0f, 16f, 100000f), center);
    }

    public void Update(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _level.Enemy?.Update(gameTime, _dummyPlayer);
    }

    public void Draw()
    {
        var eye = CameraBase + new Vector3(MathF.Sin(_time * 0.2f) * 15f, 0f, 0f);
        var view = Matrix.CreateLookAt(eye, LookTarget, Vector3.Up);

        SceneLighting.EyePosition = eye;
        SceneLighting.FlashlightEnabled = true;
        SceneLighting.LightPosition = eye + new Vector3(0f, 30f, 40f);

        _graphics.DepthStencilState = DepthStencilState.Default;
        _graphics.RasterizerState = NoCull;
        _graphics.BlendState = BlendState.Opaque;
        _graphics.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, _level.BackgroundColor, 1f, 0);

        _level.Draw(view, _projection);
    }

    public void Unload() => _level.UnloadContent();
}