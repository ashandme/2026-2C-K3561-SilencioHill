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
        private readonly int _baseMargin;
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
            _baseMargin = margin;
            _leftColor = leftColor ?? Color.White;
            _rightColor = rightColor ?? Color.Lime;
            _defaultRightText = defaultRightText;
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        // UI scale and margin for the current viewport (reference sizes are for 1280x720).
        private float Scale => UiScale.Of(_graphicsDevice);
        private float Margin => _baseMargin * Scale;

        private void DrawText(string text, Vector2 position, Color color) =>
            _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);

        private float LineHeight => _font.LineSpacing * Scale;

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
                    if (interactionManager.Selected is TGC.MonoGame.TP.PropUtils.EvidenceProp)
                        interactionText = player?.CurrentItem is CameraItem
                            ? "E: Fotografiar"
                            : "Equipa la camara (3) para fotografiar";
                    else 
                    if (interactionManager.Selected is TGC.MonoGame.TP.PropUtils.CarProp car)
                        interactionText = car.Unlocked ? "E: Escapar" : "Necesitas las 5 pruebas para escapar";
                    else 
                    if (interactionManager.Selected is TGC.MonoGame.TP.PropUtils.DoorProp)
                        interactionText = "E: Abrir puerta";
                    else
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
            var scale = Scale;
            var barWidth = (int)(150 * scale);
            var barHeight = Math.Max(2, (int)(10 * scale));
            var spacing = 8f * scale;
            var viewportWidth = _graphicsDevice.Viewport.Width;

            foreach (var item in _itemProgress)
            {
                var labelSize = _font.MeasureString(item.Name) * scale;
                var labelX = MathF.Max(Margin, viewportWidth - Margin - barWidth);
                var labelY = y;
                var barY = labelY + labelSize.Y + 2f * scale;

                DrawText(item.Name, new Vector2(labelX, labelY), _rightColor);
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
                var x = Margin;
                var y = Margin;
                foreach (var line in lines)
                {
                    DrawText(line, new Vector2(x, y), _leftColor);
                    y += LineHeight;
                }
            }

            // Right HUD: status + item indicators appended below status
            var rightText = string.IsNullOrEmpty(_status) ? _defaultRightText : _status;
            var yRight = Margin;

            if (!string.IsNullOrEmpty(rightText))
                DrawRightAligned(rightText, ref yRight);

            // Additional per-item status lines are provided via RightOverlay
            if (!string.IsNullOrEmpty(RightOverlay))
                DrawRightAligned(RightOverlay, ref yRight);

            DrawItemProgressBars(ref yRight);

            _spriteBatch.End();

            _graphicsDevice.DepthStencilState = prevDepth;
        }

        private void DrawRightAligned(string text, ref float y)
        {
            var viewportWidth = _graphicsDevice.Viewport.Width;
            foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.None))
            {
                var size = _font.MeasureString(line) * Scale;
                var x = MathF.Max(Margin, viewportWidth - Margin - size.X);
                DrawText(line, new Vector2(x, y), _rightColor);
                y += LineHeight;
            }
        }

        public void SetRightOverlayFromInventory(System.Collections.Generic.IEnumerable<Item> inventory)
        {
            UpdateItemProgress(inventory);
            RightOverlay = null;
        }
    }
}