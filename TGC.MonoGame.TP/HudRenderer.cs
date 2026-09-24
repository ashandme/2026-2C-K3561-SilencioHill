using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP
{
    internal class HudRenderer
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly int _margin;
        private readonly Color _leftColor;
        private readonly Color _rightColor;
        private readonly string _defaultRightText;

        private string _status = "";
        private double _statusTimer = 0.0;
        // Optional per-frame right-side overlay (e.g., item states). Set by game each frame.
        public string RightOverlay { get; set; } = null;
        public HudRenderer(SpriteBatch spriteBatch, SpriteFont font, GraphicsDevice graphicsDevice, int margin = 10,
            Color? leftColor = null, Color? rightColor = null, string defaultRightText = "READY")
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _margin = margin;
            _leftColor = leftColor ?? Color.White;
            _rightColor = rightColor ?? Color.Lime;
            _defaultRightText = defaultRightText;
        }

        public void SetStatus(string text, double durationSeconds)
        {
            _status = text ?? "";
            _statusTimer = Math.Max(0.0, durationSeconds);
        }

        public void Update(GameTime gameTime)
        {
            if (_statusTimer <= 0) return;
            _statusTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_statusTimer <= 0) _status = "";
        }

        public void Draw(string leftText)
        {
            var prevDepth = _graphicsDevice.DepthStencilState;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Left text (general info)
            if (!string.IsNullOrEmpty(leftText))
            {
                var lines = leftText.Split(new[] { '\n' }, StringSplitOptions.None);
                var x = _margin;
                var y = _margin;
                foreach (var line in lines)
                {
                    _spriteBatch.DrawString(_font, line, new Vector2(x, y), _leftColor);
                    y += _font.LineSpacing;
                }
            }

            // Right HUD: status + item indicators appended below status
            var rightText = string.IsNullOrEmpty(_status) ? _defaultRightText : _status;
            var viewportWidth = _graphicsDevice.Viewport.Width;
            var yRight = _margin;

            if (!string.IsNullOrEmpty(rightText))
            {
                var lines = rightText.Split(new[] { '\n' }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    var size = _font.MeasureString(line);
                    var x = MathF.Max(_margin, viewportWidth - _margin - size.X);
                    _spriteBatch.DrawString(_font, line, new Vector2(x, yRight), _rightColor);
                    yRight += _font.LineSpacing;
                }
            }

            // Additional per-item status lines are provided via RightOverlay
            if (!string.IsNullOrEmpty(RightOverlay))
            {
                var lines = RightOverlay.Split(new[] { '\n' }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    var size = _font.MeasureString(line);
                    var x = MathF.Max(_margin, viewportWidth - _margin - size.X);
                    _spriteBatch.DrawString(_font, line, new Vector2(x, yRight), _rightColor);
                    yRight += _font.LineSpacing;
                }
            }

            _spriteBatch.End();

            _graphicsDevice.DepthStencilState = prevDepth;
        }
    }
}