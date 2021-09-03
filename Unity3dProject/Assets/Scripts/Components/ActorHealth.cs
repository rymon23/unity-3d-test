using UnityEngine;

namespace Hybrid.Components
{
    public class ActorHealth : MonoBehaviour
    {
        public float health;
        public int healthMax = 100;
        public float regenRate = 0.25f;

        private void Start() {
            health = healthMax;
        }
    }
}