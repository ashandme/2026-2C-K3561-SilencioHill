using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TGC.MonoGame.TP.Entities;
using TGC.MonoGame.TP.LevelUtils;

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
        private readonly Texture2D _pixel;
        private readonly List<(string Name, float Progress)> _itemProgress = new();

        private string _status = "";
        private double _statusTimer = 0.0;
        private string _leftText = null;
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
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
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

        // Update HUD state from gameplay entities. Debug information is rendered by DebugState.
        public void UpdateState(Player player, InteractionManager interactionManager = null)
        {
            try
            {
                var interactionText = "";
                if (interactionManager?.Selected != null)
                {
                    interactionText = $"E: Agarrar '{interactionManager.Selected.Id}'";
                    if (interactionManager.CandidateCount > 1)
                    {
                        interactionText += " | Tab: Alternar";
                    }
                }

                _leftText = interactionText;

                // Build right overlay from player's inventory
                if (player != null)
                {
                    UpdateItemProgress(player.Inventory);
                }
                else
                {
                    _itemProgress.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HudRenderer.UpdateState failed: " + ex.Message);
                _leftText = null;
                RightOverlay = null;
            }
        }

        private void UpdateItemProgress(IEnumerable<Item> inventory)
        {
            _itemProgress.Clear();
            foreach (var item in inventory)
            {
                if (item == null || item.MaxDurationSeconds <= 0f) continue;

                var progress = MathHelper.Clamp(
                    item.RemainingSeconds / item.MaxDurationSeconds,
                    0f,
                    1f);
                _itemProgress.Add((item.Name, progress));
            }
        }

        private void DrawItemProgressBars(ref float y)
        {
            const int barWidth = 150;
            const int barHeight = 10;
            const int spacing = 8;
            var viewportWidth = _graphicsDevice.Viewport.Width;

            foreach (var item in _itemProgress)
            {
                var labelSize = _font.MeasureString(item.Name);
                var labelX = MathF.Max(_margin, viewportWidth - _margin - barWidth);
                var labelY = y;
                var barY = labelY + labelSize.Y + 2f;

                _spriteBatch.DrawString(_font, item.Name, new Vector2(labelX, labelY), _rightColor);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)labelX, (int)barY, barWidth, barHeight),
                    Color.DarkGray);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)labelX, (int)barY, (int)(barWidth * item.Progress), barHeight),
                    item.Progress > 0.25f ? Color.LimeGreen : Color.Red);

                y = barY + barHeight + spacing;
            }
        }

        public void Draw()
        {
            var prevDepth = _graphicsDevice.DepthStencilState;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Left text (general info)
            if (!string.IsNullOrEmpty(_leftText))
            {
                var lines = _leftText.Split(new[] { '\n' }, StringSplitOptions.None);
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
            var yRight = (float)_margin;

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

            DrawItemProgressBars(ref yRight);

            _spriteBatch.End();

            _graphicsDevice.DepthStencilState = prevDepth;
        }

        public void SetRightOverlayFromInventory(System.Collections.Generic.IEnumerable<Item> inventory)
        {
            UpdateItemProgress(inventory);
            RightOverlay = null;
        }
    }
}