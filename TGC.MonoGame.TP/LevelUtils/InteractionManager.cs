using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TGC.MonoGame.TP.Entities;
using TGC.MonoGame.TP.PropUtils;

namespace TGC.MonoGame.TP.LevelUtils
{
    internal class InteractionManager
    {
        private const float MaxInteractDistance = 45f;

        private readonly List<InteractiveProp> _candidates = new();
        private InteractiveProp _selected;

        public InteractiveProp Selected => _selected;
        public int CandidateCount => _candidates.Count;

        // Llamar una vez por frame: recalcula que props estan en rango y ordena por prioridad
        public void UpdateCandidates(Vector3 playerPosition, Level level)
        {
            _candidates.Clear();
            if (level == null) { _selected = null; return; }

            foreach (var prop in level.GetProps())
            {
                if (prop is InteractiveProp interactive &&
                    Vector3.Distance(playerPosition, interactive.Position) <= MaxInteractDistance)
                {
                    _candidates.Add(interactive);
                }
            }

            // Mayor prioridad primero
            _candidates.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Si lo seleccionado salio de rango (o no habia nada elegido), tomo el mejor candidato
            if (_selected == null || !_candidates.Contains(_selected))
            {
                _selected = _candidates.Count > 0 ? _candidates[0] : null;
            }
        }

        // Pasa manualmente al siguiente candidato en rango (boton de alternar)
        public void CycleSelection()
        {
            if (_candidates.Count == 0) return;

            var currentIndex = _selected != null ? _candidates.IndexOf(_selected) : -1;
            var nextIndex = (currentIndex + 1) % _candidates.Count;
            _selected = _candidates[nextIndex];
        }

        public bool Interact(Player player, Level level)
        {
            if (_selected == null || player == null) return false;

            var consumed = _selected.OnInteract(player);
            if (consumed)
            {
                level?.RemoveProp(_selected);
                _candidates.Remove(_selected);
                _selected = _candidates.Count > 0 ? _candidates[0] : null;
            }

            return consumed;
        }
    }
}