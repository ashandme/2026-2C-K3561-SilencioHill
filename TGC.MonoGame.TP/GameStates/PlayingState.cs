using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.Samples.Geometries;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Entities;
using TGC.MonoGame.TP.Entities.Enemy;
using TGC.MonoGame.TP.LevelUtils;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class PlayingState : IGameState
{
    private const float InsidePlayerHeightOffset = 50f;
    private const float InsideCameraHeightOffset = 40f;
    private readonly GameStateContext _context;
    private readonly GraphicsDevice _graphics;
    private readonly InputManager _input;
    private readonly Matrix _projection;
    private readonly Effect _effect;
    private readonly LevelManager _levelManager;
    private readonly Player _player;
    private readonly FreeCamera _spectator;
    private readonly CollisionHandler _collisionHandler = new();
    private readonly FullScreenQuad _fullScreenQuad;
    private readonly RenderTarget2D _sceneRenderTarget;
    private readonly HudRenderer _hud;
    private readonly InteractionManager _interactionManager;
    private readonly Texture2D _crosshairPixel;
    private Enemy _enemy;
    private bool _playerMode = true;
    private bool _caughtOverlayShown;
    private readonly bool _debugEnabled;

    public bool DrawsStatesBelow => false;
    private Camera ActiveCamera => _playerMode ? _player : _spectator;

    public PlayingState(GameStateContext context, bool debugEnabled = false)
    {
        _context = context;
        _debugEnabled = debugEnabled;
        _graphics = context.GraphicsDevice;
        _input = context.Input;
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            _graphics.Viewport.AspectRatio,
            Camera.DefaultNearPlaneDistance,
            2000f);
        _effect = context.Content.Load<Effect>("Effects/BlinnPhong");

        _levelManager = new LevelManager();
        _levelManager.AddLevel("outside", new Level("outside", [new Map(), new MapJson { filePath = "Content/outsidemap.json" }], skyboxSettings: new SkyboxSettings
        {
            TextureAsset = "Textures/gradient_sky-night"
        }));
        _levelManager.AddLevel("inside", new Level("inside", [new MapJson { filePath = "Content/insidemap.json" }]));
        var center = new Point(_graphics.Viewport.Width / 2, _graphics.Viewport.Height / 2);
        _player = new Player(_graphics.Viewport.AspectRatio, new Vector3(0, 10, 50), center);
        _spectator = new FreeCamera(_graphics.Viewport.AspectRatio, new Vector3(0, 50, 150), center);
        _player.SetInput(_input);
        _player.SetCollisionHandler(_collisionHandler);

        _levelManager.LoadLevel("outside", context.Content, _graphics);
        RefreshCollisions();
        ConfigureEnemyForCurrentLevel();
        PlayLevelAmbience();

        var candle = context.Content.Load<Model>("Models/Assets/Candle");
        var flashlight = context.Content.Load<Model>("Models/Assets/Linterna");
        Texture2D texture = null;
        Effect textureEffect = null;
        try { texture = context.Content.Load<Texture2D>("Textures/FlashlightTexture"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        try { textureEffect = context.Content.Load<Effect>("Effects/BasicTexture"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        _player.PickupItem(new FlashlightItem(flashlight, texture, textureEffect));
        _player.PickupItem(new CandleItem(candle, texture, textureEffect));
        _player.LoadHorrorOverlay(context.Content.Load<Texture2D>("Textures/overlay"), context.Content.Load<Effect>("Effects/TextureMerge"));

        _fullScreenQuad = new FullScreenQuad(_graphics);
        _sceneRenderTarget = new RenderTarget2D(_graphics, _graphics.Viewport.Width, _graphics.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);
        _hud = new HudRenderer(context.SpriteBatch, context.Font, _graphics);
        _interactionManager = new InteractionManager();
        _crosshairPixel = new Texture2D(_graphics, 1, 1);
        _crosshairPixel.SetData(new[] { Color.White });
    }

    public void Enter()
    {
        _context.SetMouseVisible(false);
        if (_debugEnabled && _context.States.Current is PlayingState)
        {
            _context.States.Push(new DebugState(_context, this));
        }
    }
    public void Exit() => _context.SetMouseVisible(true);

    public void Update(GameTime gameTime)
    {
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _context.States.Push(new PauseState(_context,
                () => new PlayingState(_context, _debugEnabled),
                () => new MainMenuState(_context, () => new PlayingState(_context))));
            return;
        }

        UpdateGameplay(gameTime);
    }

    internal void UpdateGameplay(GameTime gameTime)
    {
        if (_playerMode) { _player.Update(gameTime); _player.UpdateItems(gameTime); }
        else _spectator.Update(gameTime);

        var current = _player.CurrentItem;
        SceneLighting.FlashlightEnabled = false;
        SceneLighting.IsDirectional = false;
        SceneLighting.IsSpotlight = false;
        SceneLighting.SpotInnerCosine = 0.94f;
        SceneLighting.SpotOuterCosine = 0.82f;
        SceneLighting.Attenuation = new Vector3(1f, 0.002f, 0.00002f);
        SceneLighting.LightColor = Vector3.Zero;
        bool isInside = _levelManager.CurrentName?.Equals("inside", StringComparison.OrdinalIgnoreCase) == true;
        SceneLighting.AmbientIntensity = isInside ? 0.24f : 0.08f;
        SceneLighting.DiffuseIntensity = isInside ? 0.58f : 1f;
        SceneLighting.UpwardLightScale = isInside ? 0.35f : 1f;
        SceneLighting.LightPosition = _player.CameraPosition + new Vector3(0f, 50f, 50f);
        SceneLighting.LightDirection = _player.FrontDirection;

        if (current is FlashlightItem light && light.IsOn && light.AttachToCamera)
        {
            SceneLighting.FlashlightEnabled = true;
            SceneLighting.IsSpotlight = true;
            SceneLighting.LightColor = Vector3.One;
            SceneLighting.SpotInnerCosine = 0.98f;
            SceneLighting.SpotOuterCosine = 0.88f;
            SceneLighting.LightPosition = _player.CameraPosition;
            // This is the direction the flashlight rays travel from the camera.
            SceneLighting.LightDirection = _player.FrontDirection;
        }
        else if (current is CandleItem candle && candle.IsLit)
        {
            SceneLighting.FlashlightEnabled = true;
            SceneLighting.LightPosition = _player.CameraPosition
                + _player.RightDirection * candle.CameraLocalOffset.X
                + _player.UpDirection * candle.CameraLocalOffset.Y
                + _player.FrontDirection * candle.CameraLocalOffset.Z;
            SceneLighting.LightColor = new Vector3(1f, 0.65f, 0.12f);
        }

        _enemy?.Update(gameTime, _player);
        if (_player.State == PlayerState.Caught && !_caughtOverlayShown)
        {
            _caughtOverlayShown = true;
            _context.States.Push(new CaughtState(_context, this,
                () => new PlayingState(_context),
                () => new MainMenuState(_context, () => new PlayingState(_context))));
            return;
        }
        _hud.Update(gameTime);
        _interactionManager.UpdateCandidates(_player.Position, _levelManager.Current);
        if (_playerMode)
        {
            if (_input.IsCyclePressed()) _interactionManager.CycleSelection();
            if (!_player.IsStairsAnimating && _input.IsInteractPressed())
            {
                if (_interactionManager.Selected is DoorProp door)
                    UseDoor(door);
                else
                    _interactionManager.Interact(_player, _levelManager.Current);
            }
        }
    }

    private void PlayLevelAmbience() => Audio.PlayMusic(_levelManager.CurrentName == "inside" ? "IndoorAmbience" : "NighttimeForestAmbience");
    private void UseDoor(DoorProp door)
    {
        if (string.IsNullOrWhiteSpace(door.TargetLevel)) return;

        try
        {
            _levelManager.LoadLevel(door.TargetLevel, _context.Content, _graphics);
            Audio.Play("Door");
            if (door.TeleportPosition != Vector3.Zero)
            {
                var destination = door.TeleportPosition;
                if (door.TargetLevel.Equals("inside", StringComparison.OrdinalIgnoreCase))
                    destination.Y += InsidePlayerHeightOffset;
                _player.Position = destination;
            }
            ConfigureEnemyForCurrentLevel();
            RefreshCollisions();
            PlayLevelAmbience();
        }
        catch (Exception ex)
        {
            _hud.SetStatus("Door ERROR: " + ex.Message, 5.0);
        }
    }

    public void Draw(GameTime gameTime)
    {
        _graphics.SetRenderTarget(_sceneRenderTarget);
        _graphics.DepthStencilState = DepthStencilState.Default;
        _graphics.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, _levelManager.Current?.BackgroundColor ?? new Color(0.0f, 0f, 0.0f), 1f, 0);
        UpdatePlayerCameraView();
        var view = ActiveCamera.View;
        SceneLighting.EyePosition = Matrix.Invert(view).Translation;
        _levelManager.Current?.Draw(view, _projection);
        if (_playerMode) _player.DrawHeldItem(_effect, _projection, _graphics);
        _graphics.SetRenderTarget(null);
        _graphics.DepthStencilState = DepthStencilState.None;
        _context.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
        _context.SpriteBatch.Draw(_sceneRenderTarget, _graphics.Viewport.Bounds, Color.White);
        _context.SpriteBatch.End();
        _graphics.DepthStencilState = DepthStencilState.Default;
        _hud.UpdateState(_player, _interactionManager);
        _hud.Draw();
        if (_playerMode)
            _player.DrawCenterDot(_context.SpriteBatch, _crosshairPixel);
    }

    private void UpdatePlayerCameraView()
    {
        if (!_playerMode) return;

        _player.CameraOffset = _levelManager.CurrentName?.Equals("inside", StringComparison.OrdinalIgnoreCase) == true
            ? Vector3.Up * InsideCameraHeightOffset
            : Vector3.Zero;

        var cameraPosition = _player.CameraPosition;

        _player.View = Matrix.CreateLookAt(
            cameraPosition,
            cameraPosition + _player.FrontDirection,
            _player.UpDirection);
    }

    internal Matrix ActiveView => ActiveCamera.View;
    internal Player DebugPlayer => _player;
    internal Enemy DebugEnemy => _enemy;
    internal bool IsPlayerMode => _playerMode;

    internal void DrawCaughtOverlay(GameTime gameTime)
    {
        _graphics.DepthStencilState = DepthStencilState.None;
        _player.ApplyCameraTexture(_graphics, _fullScreenQuad, _sceneRenderTarget,
            (float)gameTime.TotalGameTime.TotalSeconds);
    }

    internal void ToggleCamera() => _playerMode = !_playerMode;
    internal void ChangeLevel()
    {
        try
        {
            var next = _levelManager.CurrentName == "outside" ? "inside" : "outside";
            _levelManager.LoadLevel(next, _context.Content, _graphics);
            AdjustPlayerHeightForLevel(next);
            ConfigureEnemyForCurrentLevel();
            RefreshCollisions();
            PlayLevelAmbience();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    private void AdjustPlayerHeightForLevel(string levelName)
    {
        if (!levelName.Equals("inside", StringComparison.OrdinalIgnoreCase))
            _player.SetGroundHeight(0f);

        var offset = levelName.Equals("inside", StringComparison.OrdinalIgnoreCase)
            ? InsidePlayerHeightOffset
            : -InsidePlayerHeightOffset;
        _player.Position += Vector3.Up * offset;
    }

    internal void ReloadLevel()
    {
        try
        {
            _levelManager.ReloadCurrent(_context.Content, _graphics);
            ConfigureEnemyForCurrentLevel();
            RefreshCollisions();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    private void ConfigureEnemyForCurrentLevel()
    {
        _enemy = _levelManager.Current?.Enemy;
        if (_enemy != null && _levelManager.CurrentName?.Equals("inside", StringComparison.OrdinalIgnoreCase) == true)
            _enemy.SetScale(_enemy.Scale * 3f);
    }

    internal void RefreshCollisions()
    {
        var colliders = new List<CollisionShape>();
        var currentLevel = _levelManager.Current;
        if (currentLevel != null)
        {
            foreach (var prop in currentLevel.EnumerateProps())
                colliders.AddRange(prop.Colliders);
        }
        _collisionHandler.Rebuild(colliders);
    }
}
