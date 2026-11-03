using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.LevelUtils;

sealed class Skybox : IDisposable
{
    private readonly GraphicsDevice _graphics;
    private readonly Texture2D _texture;
    private readonly Effect _effect;
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private bool _disposed;

    public Skybox(SkyboxSettings settings, GraphicsDevice graphics, ContentManager content)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        if (settings.Mode != SkyboxMode.Gradient)
            throw new NotSupportedException($"Skybox mode '{settings.Mode}' is not implemented.");
        if (string.IsNullOrWhiteSpace(settings.TextureAsset))
            throw new ArgumentException("A skybox texture asset is required.", nameof(settings));

        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _texture = content.Load<Texture2D>(settings.TextureAsset);
        _effect = content.Load<Effect>(settings.EffectAsset);

        _effect.Parameters["SkyTexture"]?.SetValue(_texture);
        _effect.Parameters["Tint"]?.SetValue(settings.Tint.ToVector4());
        _effect.Parameters["Brightness"]?.SetValue(settings.Brightness);
        _effect.Parameters["Mode"]?.SetValue((int)settings.Mode);

        var vertices = new VertexPosition[8]
        {
            new(new Vector3(-1, -1, -1)), new(new Vector3(-1, 1, -1)),
            new(new Vector3(1, 1, -1)), new(new Vector3(1, -1, -1)),
            new(new Vector3(-1, -1, 1)), new(new Vector3(-1, 1, 1)),
            new(new Vector3(1, 1, 1)), new(new Vector3(1, -1, 1))
        };
        var indices = new ushort[]
        {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 4, 5, 0, 5, 1,
            3, 2, 6, 3, 6, 7,
            1, 5, 6, 1, 6, 2,
            0, 3, 7, 0, 7, 4
        };

        _vertexBuffer = new VertexBuffer(graphics, VertexPosition.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);
        _indexBuffer = new IndexBuffer(graphics, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        var viewWithoutTranslation = view;
        viewWithoutTranslation.M41 = 0f;
        viewWithoutTranslation.M42 = 0f;
        viewWithoutTranslation.M43 = 0f;
        viewWithoutTranslation.M44 = 1f;

        var previousDepth = _graphics.DepthStencilState;
        var previousSampler = _graphics.SamplerStates[0];
        var previousRasterizer = _graphics.RasterizerState;

        try
        {
            _graphics.DepthStencilState = DepthStencilState.None;
            _graphics.SamplerStates[0] = SamplerState.LinearClamp;
            _graphics.RasterizerState = RasterizerState.CullNone;
            _effect.Parameters["View"]?.SetValue(viewWithoutTranslation);
            _effect.Parameters["Projection"]?.SetValue(projection);
            _graphics.SetVertexBuffer(_vertexBuffer);
            _graphics.Indices = _indexBuffer;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }
        }
        finally
        {
            _graphics.DepthStencilState = previousDepth;
            _graphics.SamplerStates[0] = previousSampler;
            _graphics.RasterizerState = previousRasterizer;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }
}
