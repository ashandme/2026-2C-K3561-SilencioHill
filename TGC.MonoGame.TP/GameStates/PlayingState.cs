using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TGC.MonoGame.Samples.Geometries;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Entities;
using TGC.MonoGame.TP.Entities.Enemy;
using TGC.MonoGame.TP.LevelUtils;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class PlayingState : IGameState
{
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
        _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _graphics.Viewport.AspectRatio, 1, 2000);
        _effect = context.Content.Load<Effect>("Effects/BlinnPhong");

        _levelManager = new LevelManager();
        _levelManager.AddLevel("outside", new Level("outside", [new Map(), new MapJson { filePath = "Content/props.json" }]));
        _levelManager.AddLevel("inside", new Level("inside", [new MapJson { filePath = "Content/insideprops.json" }]));

        var center = new Point(_graphics.Viewport.Width / 2, _graphics.Viewport.Height / 2);
        _player = new Player(_graphics.Viewport.AspectRatio, new Vector3(0, 10, 50), center);
        _spectator = new FreeCamera(_graphics.Viewport.AspectRatio, new Vector3(0, 50, 150), center);
        _player.SetInput(_input);
        _player.SetCollisionHandler(_collisionHandler);

        _levelManager.LoadLevel("outside", context.Content);
        RefreshCollisions();
        _enemy = _levelManager.Current?.Enemy;

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
        if (current is FlashlightItem light && light.IsOn && light.AttachToCamera)
        {
            SceneLighting.FlashlightEnabled = true;
            SceneLighting.LightPosition = _player.Position + _player.FrontDirection * 4f;
            _effect.Parameters["lightPosition"].SetValue(SceneLighting.LightPosition);
            _effect.Parameters["eyePosition"].SetValue(_player.Position);
        }
        else SceneLighting.FlashlightEnabled = false;

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
            if (_input.IsInteractPressed()) _interactionManager.Interact(_player, _levelManager.Current);
        }
    }

    public void Draw(GameTime gameTime)
    {
        _graphics.SetRenderTarget(_sceneRenderTarget);
        _graphics.DepthStencilState = DepthStencilState.Default;
        _graphics.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, _levelManager.Current?.BackgroundColor ?? new Color(0.0f, 0f, 0.0f), 1f, 0);
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

    internal Matrix ActiveView => ActiveCamera.View;
    internal Player DebugPlayer => _player;
    internal Enemy DebugEnemy => _enemy;
    internal bool IsPlayerMode => _playerMode;

    internal void DrawCaughtOverlay(GameTime gameTime)
    {
        _player.ApplyCameraTexture(_graphics, _fullScreenQuad, _sceneRenderTarget,
            (float)gameTime.TotalGameTime.TotalSeconds);
    }

    internal void ToggleCamera() => _playerMode = !_playerMode;
    internal void ChangeLevel()
    {
        try
        {
            var next = _levelManager.CurrentName == "outside" ? "inside" : "outside";
            _levelManager.LoadLevel(next, _context.Content);
            _enemy = _levelManager.Current?.Enemy;
            RefreshCollisions();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    internal void ReloadLevel()
    {
        try
        {
            _levelManager.ReloadCurrent(_context.Content);
            _enemy = _levelManager.Current?.Enemy;
            RefreshCollisions();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    internal void RefreshCollisions()
    {
        var colliders = new List<CollisionShape>();
        foreach (var prop in _levelManager.Current?.GetProps() ?? Array.Empty<PropUtils.Prop>()) colliders.AddRange(prop.Colliders);
        _collisionHandler.Rebuild(colliders);
    }
}
