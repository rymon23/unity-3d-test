using UnityEngine;
using System;

namespace Hybrid.Components
{

    public enum DeathState
    {
        alive = 0,
        dying = 1,
        dead = 2,
    }

    public class ActorHealth : MonoBehaviour

    {
        [SerializeField] private float _health = 100f;
        public float health
        {
            get => _health;
            set
            {
                _health = value;
                _health = Mathf.Clamp(_health, 0, (float)healthMax);
            }
        }
        // public float health = 100f;
        public int healthMax = 100;
        public float regenRate = 0.25f;

        [SerializeField] private bool _invincible = false;
        public bool isInvincible
        {
            get => _invincible;
            set => _invincible = value;
        }

        [SerializeField] private DeathState _deathState = 0;
        public DeathState deathState
        {
            get => _deathState;
            set => _deathState = value;
        }

        public float GetHealthPercentage() => ((float)health / (float)healthMax);


        public HealthBar healthBar;
        ActorEventManger actorEventManger;


        private void Awake()
        {
            health = healthMax;
        }

        private void Start()
        {
            healthBar = GetComponentInChildren<HealthBar>();
            actorEventManger = GetComponent<ActorEventManger>();
            if (actorEventManger != null)
            {
                actorEventManger.onDamageHealth += DamageHealth;
            }
        }

        private void DamageHealth(float fDamage)
        {
            if (!isInvincible && deathState < DeathState.dying)
            {
                Debug.Log("ActorHealth => DamageHealth: " + fDamage);

                health -= Math.Abs(fDamage);
                actorEventManger.UpdateHealthBar(health / (float)healthMax);
                if (health <= 0)
                {
                    actorEventManger.ActorDeath();
                }
            }
        }

        void onHeal(float fAmount)
        {
            if (deathState < DeathState.dying)
            {
                health += Math.Abs(fAmount);
                actorEventManger.UpdateHealthBar(health / (float)healthMax);
            }
        }

        private void OnDestroy()
        {
            if (actorEventManger != null) actorEventManger.onDamageHealth -= DamageHealth;
        }
    }
}