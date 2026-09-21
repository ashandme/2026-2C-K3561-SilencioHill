using Microsoft.Xna.Framework;
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

        // Llama al constructor de FreeCamera y arranca el registro de teclado
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