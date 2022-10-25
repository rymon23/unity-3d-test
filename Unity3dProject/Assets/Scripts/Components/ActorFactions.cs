using UnityEngine;

namespace Hybrid.Components
{
    public class ActorFactions : MonoBehaviour
    {
        [SerializeField] private Faction[] _factions;
        public Faction[] factions
        {
            get => _factions;
            set => _factions = value;
        }

        public Faction GetFirstFaction () {
            if (_factions?.Length == 0) return null;
            return _factions[0];
        }
    }
}