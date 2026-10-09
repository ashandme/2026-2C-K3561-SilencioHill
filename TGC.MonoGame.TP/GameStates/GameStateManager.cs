using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.GameStates;

internal sealed class GameStateManager
{
    private readonly List<IGameState> _stack = new();

    public IGameState Current => _stack[^1];

    public void Push(IGameState state)
    {
        if (_stack.Count > 0)
            _stack[^1].Exit();

        _stack.Add(state);
        state.Enter();
    }

    public void Pop()
    {
        if (_stack.Count == 0) return;

        _stack[^1].Exit();
        _stack.RemoveAt(_stack.Count - 1);

        if (_stack.Count > 0)
            _stack[^1].Enter();
    }

    public void Replace(IGameState state)
    {
        if (_stack.Count > 0)
            Pop();

        Push(state);
    }

    public void Clear()
    {
        while (_stack.Count > 0)
        {
            _stack[^1].Exit();
            _stack.RemoveAt(_stack.Count - 1);
        }
    }

    public void Update(GameTime gameTime)
    {
        if (_stack.Count > 0)
            _stack[^1].Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        if (_stack.Count == 0) return;

        var first = _stack.Count - 1;
        while (first > 0 && _stack[first].DrawsStatesBelow)
            first--;

        for (var i = first; i < _stack.Count; i++)
            _stack[i].Draw(gameTime);
    }
}
