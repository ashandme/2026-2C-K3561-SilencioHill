using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class DebugState : IGameState
{
    private readonly GameStateContext _context;
    private readonly PlayingState _playing;

    public bool DrawsStatesBelow => true;

    public DebugState(GameStateContext context, PlayingState playing)
    {
        _context = context;
        _playing = playing;
    }

    public void Enter() => _context.SetMouseVisible(false);

    public void Exit() => _context.SetMouseVisible(true);

    public void Update(GameTime gameTime)
    {
        if (_context.Input.IsKeyPressed(Keys.Escape))
        {
            _context.States.Push(new PauseState(
                _context,
                () => new PlayingState(_context, debugEnabled: true),
                () => new MainMenuState(_context, () => new PlayingState(_context))));
            return;
        }

        if (_context.Input.IsKeyPressed(Keys.F1))
            _playing.ChangeLevel();
        else if (_context.Input.IsKeyPressed(Keys.F2))
            _playing.ReloadLevel();
        else if (_context.Input.IsKeyPressed(Keys.F3))
            _playing.ToggleCamera();

        _playing.UpdateGameplay(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        var cameraPosition = Matrix.Invert(_playing.ActiveView).Translation;
        var text = $"Camera: X={cameraPosition.X:F2} Y={cameraPosition.Y:F2} Z={cameraPosition.Z:F2}\n" +
                   $"F1: Switch Interior/Exterior | F2: Reload | F3: {(_playing.IsPlayerMode ? "Player" : "Spectator")}\n";
        if (_playing.DebugEnemy != null)
        {
            var distance = (_playing.DebugEnemy.Position - _playing.DebugPlayer.Position).Length();
            text += $"Enemy: {_playing.DebugEnemy.State} | Dist: {distance:F1} | " +
                    $"Angle: {_playing.DebugEnemy.DebugAngleToPlayerDegrees(_playing.DebugPlayer):F1}";
        }

        _context.SpriteBatch.Begin();
        _context.SpriteBatch.DrawString(_context.Font, text, new Vector2(20f, 20f), Color.White);
        _context.SpriteBatch.End();
    }
}
