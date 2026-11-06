using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Entities;
using TGC.MonoGame.TP.LevelUtils;

namespace TGC.MonoGame.TP.GameStates;

// Fondo 3D del menú principal: un interior con el fantasma patrullando
internal sealed class MenuBackground
{
    private const float ProjectionNearPlane = Camera.DefaultNearPlaneDistance;
    private const float ProjectionFarPlane = 2000f;
    private const float CameraOscillationSpeed = 0.2f;
    private const float CameraOscillationDistance = 15f;
    private const float FlickerMinimumInterval = 0.03f;
    private const float FlickerIntervalRange = 0.14f;
    private const float FlickerMinimumIntensity = 0.55f;
    private const float FlickerIntensityRange = 0.95f;

    private static readonly Vector3 CameraBase = new(146f, 30f, 157f);
    private static readonly Vector3 LookTarget = new(145f, 20f, 700f);
    private static readonly RasterizerState NoCull = new() { CullMode = CullMode.None };

    private readonly GraphicsDevice _graphics;
    private readonly Level _level;
    private readonly Player _dummyPlayer;   // solo para que el fantasma tenga a quién "mirar"
    private readonly Matrix _projection;
    private readonly Random _flickerRandom = new();
    private float _time;
    private float _flickerTimer;
    private float _lightIntensity = 30f;

    public MenuBackground(GameStateContext context)
    {
        _graphics = context.GraphicsDevice;
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            _graphics.Viewport.AspectRatio,
            ProjectionNearPlane,
            ProjectionFarPlane);

        _level = new Level("menu-inside",
            [new MapJson { filePath = "Content/insidemap.json" }],
            "Content/menuRoute.json");
        _level.LoadLevelContent(context.Content, _graphics);
        _level.Enemy?.SetScale(Vector3.One * 0.25f);

        // Jugador falso lejísimos: el fantasma se queda siempre en Roaming
        var center = new Point(_graphics.Viewport.Width / 2, _graphics.Viewport.Height / 2);
        _dummyPlayer = new Player(_graphics.Viewport.AspectRatio, new Vector3(0f, 16f, 100000f), center);
    }

    public void Update(GameTime gameTime)
    {
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += elapsedSeconds;
        UpdateLightFlicker(elapsedSeconds);
        _level.Enemy?.Update(gameTime, _dummyPlayer);
    }

    public void Draw()
    {
        var eye = GetCameraPosition();
        var view = Matrix.CreateLookAt(eye, LookTarget, Vector3.Up);

        ConfigureSceneLighting(eye);
        ConfigureRenderState();
        _graphics.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, _level.BackgroundColor, 1f, 0);

        _level.Draw(view, _projection);
    }

    private void UpdateLightFlicker(float elapsedSeconds)
    {
        _flickerTimer -= elapsedSeconds;
        if (_flickerTimer > 0f) return;

        _flickerTimer = FlickerMinimumInterval +
                        (float)_flickerRandom.NextDouble() * FlickerIntervalRange;
        _lightIntensity = FlickerMinimumIntensity +
                          (float)_flickerRandom.NextDouble() * FlickerIntensityRange;
    }

    private Vector3 GetCameraPosition() =>
        CameraBase + new Vector3(MathF.Sin(_time * CameraOscillationSpeed) * CameraOscillationDistance, 0f, 0f);

    private void ConfigureSceneLighting(Vector3 eyePosition)
    {
        SceneLighting.EyePosition = eyePosition;
        SceneLighting.FlashlightEnabled = true;
        SceneLighting.LightPosition = eyePosition;
        SceneLighting.LightColor = new Vector3(_lightIntensity);
    }

    private void ConfigureRenderState()
    {
        _graphics.DepthStencilState = DepthStencilState.Default;
        _graphics.RasterizerState = NoCull;
        _graphics.BlendState = BlendState.Opaque;
    }

    public void Unload() => _level.UnloadContent();
}