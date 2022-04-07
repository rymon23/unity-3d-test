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
    }
}