using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP.Entities.Enemy
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

    internal class Enemy : Entity
    {
        public EnemyState State { get; private set; } = EnemyState.Roaming;
        // --- Deteccion ---
        private const float SneakNoiseDistance = 150f;         // dentro del cono trasero, si se acerca mas que esto hace ruido
        private const float SearchAtHidingSpotDuration = 3f; // Tiempo que vigila el escondite antes de desaparecer
        private const float SearchStandDistance = 60f; // se detiene a esta distancia del escondite
        private float _searchTimer;
        private const float MinInvisibleSeconds = 20f;   // tiempo minimo que pasa ausente
        private const float MaxInvisibleSeconds = 45f;   // tiempo maximo
        private const float MinRespawnDistance = 200f;   // no reaparece mas cerca que esto del jugador
        private readonly Random _random = new();
        private float _invisibleTimer;

        // Cono de vision del jugador (para saber si el jugador ve al enemigo)
        private readonly SphereCollisionShape _attackRange = new(Vector3.Zero, 50f);
        private readonly SphereCollisionShape _hidingSpotRange = new(Vector3.Zero, 70f);
        private readonly ConeCollisionShape _frontSight = new(Vector3.Zero, Vector3.Forward, 400f, 15f);
        private readonly ConeCollisionShape _rearSight = new(Vector3.Zero, Vector3.Backward, 400f, 67.5f, invertDirection: true);
        private readonly ConeCollisionShape _playerSight = new(Vector3.Zero, Vector3.Forward, 400f, 30f);

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

        public Enemy(Model model, Effect effect, string waypointsJsonPath)
            : base(
                new GameMesh(model, effect, Vector3.Zero, scale: Vector3.One * 0.1f),
                Vector3.Zero,
                Vector3.Zero,
                Vector3.One * 0.1f)
        {
            AddSensor(_attackRange);
            AddSensor(_hidingSpotRange);
            AddSensor(_frontSight);
            AddSensor(_rearSight);
            LoadWaypoints(waypointsJsonPath);
            SetPosition(_waypoints.Count > 0 ? _waypoints[0] : Vector3.Zero);
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

            FaceDirection(ForwardDirection);
        }

        private BoundingSphere PlayerSphere(Player player) => new(player.Position, 0f);

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
            if (distance > _frontSight.Distance)
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
            if (IsColliding(_hidingSpotRange, PlayerSphere(player)))
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

            FaceDirection(flat);
            if (toSpot.Length() > SearchStandDistance)
            {
                Move(flat * ChaseSpeed * elapsedTime);
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
            FaceDirection(direction);
            Move(direction * RoamSpeed * elapsedTime);
        }

        private void UpdateStaring(float elapsedTime, Player player)
        {
            var toPlayer = player.Position - Position;
            var distance = toPlayer.Length();

            var flatToPlayer = FlattenXZ(toPlayer);
            if (flatToPlayer != Vector3.Zero)
            {
                FaceDirection(flatToPlayer);
            }

            if (IsColliding(_attackRange, PlayerSphere(player)))
            {
                State = EnemyState.Attacking;
                return;
            }

            if (distance > _frontSight.Distance)
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

            if (IsColliding(_attackRange, PlayerSphere(player)))
            {
                State = EnemyState.Attacking;
                return;
            }

            // TODO: cuando haya audio, emitir el ruido de aviso cuando distance <= SneakNoiseDistance

            var flatDirection = FlattenXZ(toPlayer);
            if (flatDirection != Vector3.Zero)
            {
                FaceDirection(flatDirection);
                Move(flatDirection * SneakSpeed * elapsedTime);
            }
        }
        private void UpdateChasing(float elapsedTime, Player player)
        {
            var toPlayer = player.Position - Position;
            var distance = toPlayer.Length();

            // Caso 1: te agarra
            if (IsColliding(_attackRange, PlayerSphere(player)))
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
                FaceDirection(flatDirection);
                Move(flatDirection * ChaseSpeed * elapsedTime);
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
                FaceDirection(-flatDirection);
                Move(flatDirection * FleeSpeed * elapsedTime);
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
                SetPosition(_waypoints[_currentWaypointIndex]);
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
            if (IsColliding(_frontSight, PlayerSphere(player)))
                return DetectionZone.Front;

            if (IsColliding(_rearSight, PlayerSphere(player)))
                return DetectionZone.Rear;

            return DetectionZone.None;
        }

        private bool PlayerCanSeeEnemy(Player player)
        {
            _playerSight.Update(player.Position, player.FrontDirection);
            return IsColliding(_playerSight, new BoundingSphere(Position, 0f));
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            if (State != EnemyState.Invisible)
                base.Draw(view, projection);
        }
        // Debug: angulo actual entre hacia-donde-mira el enemigo y la direccion hacia el jugador
        public float DebugAngleToPlayerDegrees(Player player)
        {
            var toPlayer = FlattenXZ(player.Position - Position);
            if (toPlayer == Vector3.Zero) return -1f;

            var dot = Vector3.Dot(ForwardDirection, toPlayer);
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
            SetPosition(_waypoints[_currentWaypointIndex]);
        }
    }
}