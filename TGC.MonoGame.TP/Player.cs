using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;

namespace TGC.MonoGame.TP
{
    // Estados posibles del jugador
    public enum PlayerState
    {
        Walking,
        Crouching,
        Undetectable,
        Caught
    }
    // Jugador en primera persona: hereda camara/mouse-look de FreeCamera, agrega estados y movimiento restringido al plano XZ
    internal class Player : FreeCamera
    {
        // Estado actual del jugador; solo Player puede modificarlo
        public PlayerState State { get; private set; } = PlayerState.Walking;

        // Alturas de camara y velocidades segun estado
        private const float StandHeight = 16f;
        private const float CrouchHeight = 10f;
        private const float GroundY = 0f;
        private const float WalkSpeed = 60f;
        private const float RunSpeed = 110f;
        private const float CrouchSpeed = 30f;

        // Flag seteado desde afuera cuando el jugador esta cerca de un escondite
        private bool _nearHidingSpot;

        // Cuenta regresiva del estado Caught
        private float _caughtTimer;
        private const float CaughtDuration = 2.5f;

        // Teclado del frame anterior, para detectar "tecla recien presionada"
        private KeyboardState _previousKeyboardState;

        // Inventory: hasta 2 items
        private readonly Item[] _inventory = new Item[2];

        // Index del item actualmente en mano (-1 = ninguno)
        private int _currentItemIndex = -1;

        // Reusable rasterizer state for HUD item rendering
        private static readonly RasterizerState HudRasterizer = new RasterizerState
        {
            CullMode = CullMode.None
        };

        public Player(float aspectRatio, Vector3 position, Point screenCenter)
            : base(aspectRatio, position, screenCenter)
        {
            _previousKeyboardState = Keyboard.GetState();
        }

        // Lo llama codigo externo (Map/TGCGame) cuando el jugador esta cerca de un escondite
        public void SetNearHidingSpot(bool near) => _nearHidingSpot = near;

        // Lo llama el bicho cuando atrapa al jugador
        public void Catch()
        {
            if (State == PlayerState.Caught) return;
            State = PlayerState.Caught;
            _caughtTimer = CaughtDuration;
        }

        public bool PickupItem(Item item)
        {
            if (item == null) return false;

            for (int i = 0; i < _inventory.Length; i++)
            {
                if (_inventory[i] == null)
                {
                    _inventory[i] = item;
                    if (_currentItemIndex == -1)
                    {
                        EquipIndex(i);
                    }
                    return true;
                }
            }

            return false;
        }

        public void DropCurrentItem()
        {
            if (_currentItemIndex < 0) return;
            _inventory[_currentItemIndex]?.OnUnequip(this);
            _inventory[_currentItemIndex] = null;

            _currentItemIndex = -1;
            for (int i = 0; i < _inventory.Length; i++)
            {
                if (_inventory[i] != null)
                {
                    EquipIndex(i);
                    break;
                }
            }
        }

        public void EquipIndex(int index)
        {
            if (index < 0 || index >= _inventory.Length) return;
            if (_currentItemIndex == index) return;

            if (_currentItemIndex >= 0 && _inventory[_currentItemIndex] != null)
            {
                _inventory[_currentItemIndex].OnUnequip(this);
            }

            _currentItemIndex = index;

            if (_currentItemIndex >= 0 && _inventory[_currentItemIndex] != null)
            {
                _inventory[_currentItemIndex].OnEquip(this);
            }
        }

        public void CycleItem()
        {
            if (_inventory[0] == null && _inventory[1] == null) return;

            if (_currentItemIndex == -1)
            {
                for (int i = 0; i < _inventory.Length; i++)
                {
                    if (_inventory[i] != null)
                    {
                        EquipIndex(i);
                        return;
                    }
                }
            }

            int start = _currentItemIndex;
            for (int offset = 1; offset <= _inventory.Length; offset++)
            {
                int idx = (start + offset) % _inventory.Length;
                if (_inventory[idx] != null)
                {
                    EquipIndex(idx);
                    return;
                }
            }
        }

        public void UseCurrentItem()
        {
            var current = GetCurrentItem();
            current?.Use(this);
        }

        public string CurrentItemName() => GetCurrentItem()?.Name ?? "Empty";

        // Public accessor to avoid reflection
        public Item CurrentItem => GetCurrentItem();

        // Draw the currently held item attached to the camera.
        // effect: shared effect instance to use for rendering the item
        public void DrawHeldItem(Effect effect, Matrix projection, GraphicsDevice graphicsDevice)
        {
            var current = GetCurrentItem();
            if (current == null) return;

            // Do not draw when player state forbids it
            if (State == PlayerState.Undetectable) return;

            // Save previous states and set HUD-friendly states
            var prevRaster = graphicsDevice.RasterizerState;
            var prevBlend = graphicsDevice.BlendState;
            var prevDepth = graphicsDevice.DepthStencilState;

            graphicsDevice.RasterizerState = HudRasterizer;
            graphicsDevice.BlendState = BlendState.AlphaBlend;

            // Compute camera basis and world matrix for the held item
            var camPos = Position;
            var camFront = FrontDirection;
            var camUp = UpDirection;
            var camRight = RightDirection;

            // Camera-local offset defined per item (X=right, Y=up, Z=forward)
            var offset = current.CameraLocalOffset;
            var worldPos = camPos + camRight * offset.X + camUp * offset.Y + camFront * offset.Z;

            // Orient item to camera basis
            var orientation = Matrix.CreateWorld(worldPos, camFront, camUp);

            // No extra local rotation/scale here; items can override DrawModel if needed
            var world = orientation;

            // Delegate actual mesh drawing to the item (it will set World/View/Projection and assign effects)
            current.DrawModel(effect, world, View, projection);

            // Restore previous device states
            graphicsDevice.RasterizerState = prevRaster;
            graphicsDevice.BlendState = prevBlend;
            graphicsDevice.DepthStencilState = prevDepth;
        }

        private Item GetCurrentItem() =>
            _currentItemIndex >= 0 && _currentItemIndex < _inventory.Length
                ? _inventory[_currentItemIndex]
                : null;

        // Maquina de estados + movimiento; se ejecuta una vez por frame via FreeCamera.Update()
        protected override void ProcessKeyboard(float elapsedTime)
        {
            var keyboardState = Keyboard.GetState();

            switch (State)
            {
                // Sin control mientras dura el timer de atrapado
                case PlayerState.Caught:
                    _caughtTimer -= elapsedTime;
                    if (_caughtTimer <= 0f)
                    {
                        State = PlayerState.Walking;
                    }
                    ApplyHeight();
                    _previousKeyboardState = keyboardState;
                    return;

                // Sin movimiento; sale del escondite si se acaba de apretar E
                case PlayerState.Undetectable:
                    if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                    {
                        State = PlayerState.Walking;
                    }
                    ApplyHeight();
                    _previousKeyboardState = keyboardState;
                    return;

                // Walking/Crouching: entra al escondite si corresponde, si no define postura segun Ctrl
                default:
                    if (_nearHidingSpot && keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                    {
                        State = PlayerState.Undetectable;
                        ApplyHeight();
                        _previousKeyboardState = keyboardState;
                        return;
                    }

                    State = keyboardState.IsKeyDown(Keys.LeftControl)
                        ? PlayerState.Crouching
                        : PlayerState.Walking;
                    break;
            }

            // Solo se puede correr si esta Walking (no agachado)
            var isRunning = State == PlayerState.Walking && keyboardState.IsKeyDown(Keys.LeftShift);
            var currentSpeed = State == PlayerState.Crouching
                ? CrouchSpeed
                : (isRunning ? RunSpeed : WalkSpeed);

            // Direcciones sin componente vertical, para no subir/bajar al caminar mirando arriba o abajo
            var flatFront = Vector3.Normalize(new Vector3(FrontDirection.X, 0, FrontDirection.Z));
            var flatRight = Vector3.Normalize(new Vector3(RightDirection.X, 0, RightDirection.Z));

            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            {
                Position += flatFront * currentSpeed * elapsedTime;
                _changed = true;
            }

            if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            {
                Position -= flatFront * currentSpeed * elapsedTime;
                _changed = true;
            }

            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            {
                Position -= flatRight * currentSpeed * elapsedTime;
                _changed = true;
            }

            if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            {
                Position += flatRight * currentSpeed * elapsedTime;
                _changed = true;
            }

            ApplyHeight();

            // F: use current item
            if (keyboardState.IsKeyDown(Keys.F) && !_previousKeyboardState.IsKeyDown(Keys.F))
            {
                UseCurrentItem();
            }

            if (keyboardState.IsKeyDown(Keys.Q) && !_previousKeyboardState.IsKeyDown(Keys.Q))
            {
                CycleItem();
            }
            // R: drop current item
            if (keyboardState.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R))
            {
                DropCurrentItem();
            }

            // 1/2: equip slot 0 / slot 1 (edge)
            if (keyboardState.IsKeyDown(Keys.D1) && !_previousKeyboardState.IsKeyDown(Keys.D1))
            {
                EquipIndex(0);
            }

            if (keyboardState.IsKeyDown(Keys.D2) && !_previousKeyboardState.IsKeyDown(Keys.D2))
            {
                EquipIndex(1);
            }

            _previousKeyboardState = keyboardState;
        }

        // Ajusta la altura de la camara (Position.Y) segun el estado actual
        private void ApplyHeight()
        {
            var targetHeight = State == PlayerState.Walking ? StandHeight : CrouchHeight;
            var targetY = GroundY + targetHeight;

            if (Position.Y != targetY)
            {
                Position = new Vector3(Position.X, targetY, Position.Z);
                _changed = true;
            }
        }
    }
}