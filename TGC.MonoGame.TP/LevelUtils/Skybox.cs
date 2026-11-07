using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.LevelUtils;

sealed class Skybox : IDisposable
{
    private readonly GraphicsDevice _graphics;
    private readonly TextureCube _cubeTexture;
    private readonly Effect _effect;
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private bool _disposed;

    public Skybox(SkyboxSettings settings, GraphicsDevice graphics, ContentManager content)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        // Skybox.fx only has a cube-map technique ("Skybox").
        if (settings.Mode != SkyboxMode.Cube)
            throw new NotSupportedException($"Skybox mode '{settings.Mode}' is not implemented; use SkyboxMode.Cube.");
        if (string.IsNullOrWhiteSpace(settings.TextureAsset))
            throw new ArgumentException("A skybox texture asset is required.", nameof(settings));

        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        var crossTexture = content.Load<Texture2D>(settings.TextureAsset);
        _cubeTexture = CreateCubeFromCross(graphics, crossTexture);
        _effect = content.Load<Effect>(settings.EffectAsset);
        _effect.CurrentTechnique = _effect.Techniques["Skybox"];
        _effect.Parameters["SkyBoxTexture"]?.SetValue(_cubeTexture);

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
            _graphics.SamplerStates[0] = SamplerState.LinearClamp;  // clamp avoids seams at face edges
            _graphics.RasterizerState = RasterizerState.CullNone;
            // The cube is centred on the camera (translation stripped from the view), so the
            // camera sits at the origin and each vertex position is the sampling direction.
            // Scaled to stay between the near plane and the far plane (2000; corner = size * sqrt(3)).
            _effect.Parameters["World"]?.SetValue(Matrix.CreateScale(CubeSize));
            _effect.Parameters["View"]?.SetValue(viewWithoutTranslation);
            _effect.Parameters["Projection"]?.SetValue(projection);
            _effect.Parameters["CameraPosition"]?.SetValue(Vector3.Zero);
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
        _cubeTexture.Dispose();
    }

    private const float CubeSize = 1000f;

    // Builds a TextureCube from a horizontal-cross image (4 x 3 faces):
    //        [+Y]
    //   [-X] [+Z] [+X] [-Z]
    //        [-Y]
    private static TextureCube CreateCubeFromCross(GraphicsDevice graphics, Texture2D cross)
    {
        if (cross.Width * 3 != cross.Height * 4)
            throw new InvalidOperationException(
                $"Skybox texture must be a 4x3 horizontal cross; got {cross.Width}x{cross.Height}.");

        var faceSize = cross.Width / 4;
        var pixels = new Color[cross.Width * cross.Height];
        cross.GetData(pixels);

        var cube = new TextureCube(graphics, faceSize, false, SurfaceFormat.Color);
        var faces = new (CubeMapFace Face, int Column, int Row)[]
        {
            (CubeMapFace.PositiveY, 1, 0),
            (CubeMapFace.NegativeX, 0, 1),
            (CubeMapFace.PositiveZ, 1, 1),
            (CubeMapFace.PositiveX, 2, 1),
            (CubeMapFace.NegativeZ, 3, 1),
            (CubeMapFace.NegativeY, 1, 2),
        };

        var facePixels = new Color[faceSize * faceSize];
        foreach (var (face, column, row) in faces)
        {
            var originX = column * faceSize;
            var originY = row * faceSize;
            for (var y = 0; y < faceSize; y++)
                Array.Copy(pixels, (originY + y) * cross.Width + originX, facePixels, y * faceSize, faceSize);
            cube.SetData(face, facePixels);
        }

        return cube;
    }
}
