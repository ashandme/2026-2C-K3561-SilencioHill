using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP
{
    public enum EnemyState
    {
        Roaming,
        Staring,
        Sneaking,
        Attacking,
        Chasing,
        Fleeing,
        Invisible
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
        private const float SearchAtHidingSpotDuration = 3f; // Tiempo que vigila el escondite antes de desaparecer
        private const float SearchStandDistance = 60f; // se detiene a esta distancia del escondite
        private float _searchTimer;
        private const float MinInvisibleSeconds = 20f;   // tiempo minimo que pasa ausente
        private const float MaxInvisibleSeconds = 45f;   // tiempo maximo
        private const float MinRespawnDistance = 200f;   // no reaparece mas cerca que esto del jugador
        private readonly Random _random = new();
        private float _invisibleTimer;

        // Cono de vision del jugador (para saber si el jugador ve al enemigo)
        private const float PlayerSightConeHalfAngleDegrees = 30f;

        // --- Velocidades ---
        private const float RoamSpeed = 25f;
        private const float SneakSpeed = 15f;
        private const float FleeSpeed = 100f;
        private const float ChaseSpeed = 75f;
        // --- Staring / Chasing ---
        private const float StaringDuration = 3f;      // segundos que se queda observando antes de perseguir (el "changui")
        private float _staringTimer;
        private const float ChaseGiveUpDistance = 600f;      // si te alejas mas que esto durante la persecucion, abandona

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
            _prop.Scale *= 0.1f;
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

            // Si el jugador esta escondido, esto manda; si no, corre la maquina de estados normal
            if (!ReactToHiddenPlayer(elapsedTime, player))
            {
                UpdateCurrentState(elapsedTime, player);
            }

            _prop.Position = Position;
            // Rotar el modelo para que visualmente mire hacia FrontDirection
            var yaw = MathF.Atan2(FrontDirection.X, FrontDirection.Z) - MathHelper.PiOver2;
            _prop.Rotation = new Vector3(0f, yaw, 0f);
        }

        private void UpdateCurrentState(float elapsedTime, Player player)
        {
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
                case EnemyState.Chasing:
                    UpdateChasing(elapsedTime, player);
                    break;
                case EnemyState.Attacking:
                    UpdateAttacking(player);
                    break;
                case EnemyState.Fleeing:
                    UpdateFleeing(elapsedTime, player);
                    break;
                case EnemyState.Invisible:
                    UpdateInvisible(elapsedTime, player);
                    break;
            }
        }

        // Reaccion al jugador escondido. Devuelve true si el enemigo esta vigilando el escondite
        // (en ese caso Update no corre la maquina de estados normal)
        private bool ReactToHiddenPlayer(float elapsedTime, Player player)
        {
            if (player.State != PlayerState.Undetectable)
            {
                _searchTimer = 0f;
                return false;
            }

            var distance = Vector3.Distance(Position, player.Position);

            // Fuera del radio de deteccion: pierde el rastro al instante
            if (distance > DetectionRadius)
            {
                if (State != EnemyState.Roaming && State != EnemyState.Invisible)
                {
                    State = EnemyState.Roaming;
                    _searchTimer = 0f;
                }
                return false;
            }

            // Ya patrullando o invisible: no lo vigila
            if (State == EnemyState.Roaming || State == EnemyState.Invisible) return false;

            // Dentro del radio: se acerca al escondite, se queda unos instantes y desaparece
            if (distance <= SearchStandDistance + 10f)
            {
                _searchTimer += elapsedTime;   // el timer solo corre cuando ya esta cerca
            }

            if (_searchTimer >= SearchAtHidingSpotDuration)
            {
                _searchTimer = 0f;
                Vanish(player.Position);
                return false;
            }

            WatchHidingSpot(elapsedTime, player);
            return true;
        }

        // Mira el escondite y se acerca hasta SearchStandDistance, sin atacar
        private void WatchHidingSpot(float elapsedTime, Player player)
        {
            var toSpot = player.Position - Position;
            var flat = FlattenXZ(toSpot);
            if (flat == Vector3.Zero) return;

            FrontDirection = flat;
            if (toSpot.Length() > SearchStandDistance)
            {
                Position += flat * ChaseSpeed * elapsedTime;
            }
        }
        private void UpdateRoaming(float elapsedTime, Player player)
        {
            if (player.State != PlayerState.Undetectable)
            {
                var detection = DetectPlayer(player);
                if (detection != DetectionZone.None)
                {
                    State = PlayerCanSeeEnemy(player) ? EnemyState.Staring : EnemyState.Sneaking;
                    _staringTimer = 0f;
                    return;
                }
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
                _staringTimer = 0f;
                return;
            }

            //La chance dura 3s pase lo que pase (aunque te des vuelta)
            _staringTimer += elapsedTime;
            if (_staringTimer >= StaringDuration)
            {
                State = EnemyState.Chasing;
                _staringTimer = 0f;
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
        private void UpdateChasing(float elapsedTime, Player player)
        {
            var toPlayer = player.Position - Position;
            var distance = toPlayer.Length();

            // Caso 1: te agarra
            if (distance <= AttackRadius)
            {
                State = EnemyState.Attacking;
                return;
            }

            // Caso 2: te fuiste muy lejos, abandona la persecucion
            if (distance > ChaseGiveUpDistance)
            {
                Vanish(player.Position);
                return;
            }

            var flatDirection = FlattenXZ(toPlayer);
            if (flatDirection != Vector3.Zero)
            {
                FrontDirection = flatDirection;
                Position += flatDirection * ChaseSpeed * elapsedTime;
            }
        }
        private void UpdateAttacking(Player player)
        {
            player.Catch();
            Vanish(player.Position);
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

        private void UpdateInvisible(float elapsedTime, Player player)
        {
            _invisibleTimer -= elapsedTime;
            if (_invisibleTimer <= 0f)
            {
                Reappear(player);
            }
        }
        
        // Desaparece: se va al waypoint mas lejano y queda invisible un tiempo aleatorio
        
        private void Vanish(Vector3 playerPosition)
        {
            TeleportToFarthestWaypoint(playerPosition);
            State = EnemyState.Invisible;
            _invisibleTimer = MinInvisibleSeconds + (float)_random.NextDouble() * (MaxInvisibleSeconds - MinInvisibleSeconds);
        }
        
        // Reaparece en un waypoint lejos del jugador y retoma la patrulla

        private void Reappear(Player player)
        {
            var candidates = new List<int>();
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (Vector3.Distance(_waypoints[i], player.Position) >= MinRespawnDistance)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count > 0)
            {
                _currentWaypointIndex = candidates[_random.Next(candidates.Count)];
                Position = _waypoints[_currentWaypointIndex];
            }
            else
            {
                TeleportToFarthestWaypoint(player.Position);   // mapa chico: usa el mas lejano
            }

            State = EnemyState.Roaming;
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
            if (State != EnemyState.Invisible)
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
        private void TeleportToFarthestWaypoint(Vector3 playerPosition)
        {
            if (_waypoints.Count == 0) return;

            var farthestIndex = 0;
            var maxDistSq = -1f;

            for (int i = 0; i < _waypoints.Count; i++)
            {
                var distSq = Vector3.DistanceSquared(_waypoints[i], playerPosition);
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    farthestIndex = i;
                }
            }

            _currentWaypointIndex = farthestIndex;
            Position = _waypoints[_currentWaypointIndex];
        }
    }
}