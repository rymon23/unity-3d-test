using UnityEngine;

namespace Hybrid.Components
    {

    public enum CombatState
    {
        inactive = 0,
        active = 1,
        searching = 2
    }

    public class CombatStateData : MonoBehaviour
    {
        public bool shoulldAttack = false; 
        [SerializeField] private CombatState _combatState = 0;
        public CombatState combatState {
            get => _combatState;
            set => _combatState = value;
        }

    }
}