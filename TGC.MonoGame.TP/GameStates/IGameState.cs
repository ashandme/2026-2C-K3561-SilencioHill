using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.GameStates;

internal interface IGameState
{
    bool DrawsStatesBelow { get; }
    void Enter();
    void Exit();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}
