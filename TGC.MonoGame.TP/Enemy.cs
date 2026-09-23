using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    public enum EnemyState
    {
        Roaming,
        Staring,
        Sneaking,
        Attacking,
        Fleeing
    }

    internal class Enemy
    {
        public EnemyState State { get; private set; } = EnemyState.Roaming;
        public Vector3 Position { get; private set; }
        public Vector3 FrontDirection { get; private set; } = Vector3.Forward;

        // --- Deteccion ---
        private const float DetectionRadius = 400f;
        private const float FrontConeHalfAngleDegrees = 15f;   // 30 grados totales
        private const float RearConeHalfAngleDegrees = 67.5f;  // 135 grados totales
        private const float SneakNoiseDistance = 150f;         // dentro del cono trasero, si se acerca mas que esto hace ruido
        private const float AttackRadius = 50f;

        // Cono de vision del jugador (para saber si el jugador ve al enemigo)
        private const float PlayerSightConeHalfAngleDegrees = 45f;

        // --- Velocidades ---
        private const float RoamSpeed = 25f;
        private const float SneakSpeed = 15f;
        private const float FleeSpeed = 100f;

        // --- Roaming ---
        private readonly List<Vector3> _waypoints = new();
        private int _currentWaypointIndex;
        private const float WaypointReachDistance = 5f;
        private const float WaypointWaitSeconds = 1.5f;
        private float _waitTimer;

        // --- Fleeing ---
        private const float FleeDuration = 4f;
        private float _fleeTimer;

        // Aplana un vector al plano XZ (ignora diferencias de altura) y lo normaliza
        private static Vector3 FlattenXZ(Vector3 v)
        {
            var flat = new Vector3(v.X, 0f, v.Z);
            return flat.LengthSquared() > 0.0001f ? Vector3.Normalize(flat) : Vector3.Zero;
        }
        
        private readonly Prop _prop;

        public Enemy(Model model, Effect effect, string waypointsJsonPath)
        {
            LoadWaypoints(waypointsJsonPath);
            Position = _waypoints.Count > 0 ? _waypoints[0] : Vector3.Zero;
            _prop = new Prop(model, effect, Position);
        }

        private void LoadWaypoints(string filePath)
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"No se encontro el archivo de ruta en: {fullPath}");
            }

            var jsonText = File.ReadAllText(fullPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var routeData = JsonSerializer.Deserialize<RouteData>(jsonText, options);

            _waypoints.Clear();
            if (routeData?.Waypoints == null) return;

            foreach (var point in routeData.Waypoints)
            {
                if (point is { Length: >= 3 })
                {
                    _waypoints.Add(new Vector3(point[0], point[1], point[2]));
                }
            }
        }

        public void Update(GameTime gameTime, Player player)
        {
            var elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (State)
            {
                case EnemyState.Roaming:
                    UpdateRoaming(elapsedTime, player);
                    break;
                case EnemyState.Staring:
                    UpdateStaring(elapsedTime, player);
                    break;
                case EnemyState.Sneaking:
                    UpdateSneaking(elapsedTime, player);
                    break;
                case EnemyState.Attacking:
                    UpdateAttacking(player);
                    break;
                case EnemyState.Fleeing:
                    UpdateFleeing(elapsedTime, player);
                    break;
            }

            _prop.Position = Position;
            // Rotar el modelo para que visualmente mire hacia FrontDirection
            var yaw = MathF.Atan2(FrontDirection.X, FrontDirection.Z) - MathHelper.PiOver2;
            _prop.Rotation = new Vector3(0f, yaw, 0f);  
        }

        private void UpdateRoaming(float elapsedTime, Player player)
{
            var detection = DetectPlayer(player);
            if (detection != DetectionZone.None)
            {
                // El enemigo te detecto (por delante o por el costado/atras).
                // Si hay contacto visual mutuo -> Staring. Si no lo estas viendo -> Sneaking.
                State = PlayerCanSeeEnemy(player) ? EnemyState.Staring : EnemyState.Sneaking;
                return;
            }

            if (_waypoints.Count == 0) return;

            var target = _waypoints[_currentWaypointIndex];
            var toTarget = target - Position;
            var distance = toTarget.Length();

            if (distance <= WaypointReachDistance)
            {
             _waitTimer += elapsedTime;
                if (_waitTimer >= WaypointWaitSeconds)
                {
                    _waitTimer = 0f;
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
                }
                return;
            }

    var direction = Vector3.Normalize(toTarget);
    FrontDirection = direction;
    Position += direction * RoamSpeed * elapsedTime;
}

        private void UpdateStaring(float elapsedTime, Player player)
        {
            var toPlayer = player.Position - Position;
            var distance = toPlayer.Length();

            var flatToPlayer = FlattenXZ(toPlayer);
            if (flatToPlayer != Vector3.Zero)
            {
                FrontDirection = flatToPlayer;
            }

            if (distance <= AttackRadius)
            {
                State = EnemyState.Attacking;
                return;
            }

            if (distance > DetectionRadius)
            {
                State = EnemyState.Fleeing;
                _fleeTimer = 0f;
                return;
            }

            // Se corto el contacto visual (dejaste de mirarlo): pasa a acecharte
            if (!PlayerCanSeeEnemy(player))
            {
                State = EnemyState.Sneaking;
            }
        }

        private void UpdateSneaking(float elapsedTime, Player player)
        {
            var toPlayer = player.Position - Position;
            var distance = toPlayer.Length();

            if (PlayerCanSeeEnemy(player))
            {
                State = EnemyState.Fleeing;
                _fleeTimer = 0f;
                return;
            }

            if (distance <= AttackRadius)
            {
                State = EnemyState.Attacking;
                return;
            }

            // TODO: cuando haya audio, emitir el ruido de aviso cuando distance <= SneakNoiseDistance

            var flatDirection = FlattenXZ(toPlayer);
            if (flatDirection != Vector3.Zero)
            {
                FrontDirection = flatDirection;
                Position += flatDirection * SneakSpeed * elapsedTime;
            }
        }

        private void UpdateAttacking(Player player)
        {
            player.Catch();
            State = EnemyState.Fleeing;
            _fleeTimer = 0f;
        }

        private void UpdateFleeing(float elapsedTime, Player player)
        {
            _fleeTimer += elapsedTime;

            var awayFromPlayer = Position - player.Position;
            var flatDirection = FlattenXZ(awayFromPlayer);
            if (flatDirection != Vector3.Zero)
            {
                FrontDirection = -flatDirection;
                Position += flatDirection * FleeSpeed * elapsedTime;
            }

            if (_fleeTimer >= FleeDuration)
            {
                State = EnemyState.Roaming;
            }
        }

        private enum DetectionZone { None, Front, Rear }

        private DetectionZone DetectPlayer(Player player)
        {
            var toPlayerRaw = player.Position - Position;
            var distance = toPlayerRaw.Length();
            if (distance > DetectionRadius || distance < 0.0001f) return DetectionZone.None;

            var direction = FlattenXZ(toPlayerRaw);
            if (direction == Vector3.Zero) return DetectionZone.None;

            var dot = Vector3.Dot(FrontDirection, direction);
            var angleDegrees = MathHelper.ToDegrees(MathF.Acos(MathHelper.Clamp(dot, -1f, 1f)));

            if (angleDegrees <= FrontConeHalfAngleDegrees) return DetectionZone.Front;

            var rearAngleDegrees = 180f - angleDegrees;
            if (rearAngleDegrees <= RearConeHalfAngleDegrees) return DetectionZone.Rear;

            return DetectionZone.None;
        }

        private bool PlayerCanSeeEnemy(Player player)
        {
            var toEnemyRaw = Position - player.Position;
            var distance = toEnemyRaw.Length();
            if (distance > DetectionRadius || distance < 0.0001f) return false;

            var direction = FlattenXZ(toEnemyRaw);
            if (direction == Vector3.Zero) return false;

            var dot = Vector3.Dot(player.FrontDirection, direction);
            var angleDegrees = MathHelper.ToDegrees(MathF.Acos(MathHelper.Clamp(dot, -1f, 1f)));

            return angleDegrees <= PlayerSightConeHalfAngleDegrees;
        }

        public void Draw(Matrix view, Matrix projection)
        {
            _prop.Draw(view, projection);
        }
        // Debug: angulo actual entre hacia-donde-mira el enemigo y la direccion hacia el jugador
        public float DebugAngleToPlayerDegrees(Player player)
        {
        var toPlayer = FlattenXZ(player.Position - Position);
            if (toPlayer == Vector3.Zero) return -1f;

            var dot = Vector3.Dot(FrontDirection, toPlayer);
            return MathHelper.ToDegrees(MathF.Acos(MathHelper.Clamp(dot, -1f, 1f)));
        }
    }
}