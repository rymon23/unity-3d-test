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
                if (immortal)
                {
                    _health = Mathf.Clamp(value, 10f, (float)healthMax);
                }
                else
                {
                    _health = value;
                    _health = Mathf.Clamp(_health, 0, (float)healthMax);
                }
                _healthPercent = ((float)health / (float)healthMax);
            }
        }
        public int healthMax = 100;

        [SerializeField] private float _healthPercent = 1f;
        public float healthPercent
        {
            get => _healthPercent;
            private set
            {
                _healthPercent = value;
            }
        }


        public float regenRate = 0.25f;

        [SerializeField] private bool _invincible = false;
        public bool isInvincible
        {
            get => _invincible;
            set => _invincible = value;
        }
        [SerializeField] private bool _immortal = false;
        public bool immortal
        {
            get => _immortal;
            set => _immortal = value;
        }

        [SerializeField] private DeathState _deathState = 0;
        public DeathState deathState
        {
            get => _deathState;
            set => _deathState = value;
        }

        public bool isDead() => (deathState >= DeathState.dying);

        public HealthBar healthBar;
        ActorEventManger actorEventManger;


        private void Awake()
        {
            health = healthMax;
            stamina = staminaMax;
            magic = magicMax;
        }

        private void Start()
        {
            healthBar = GetComponentInChildren<HealthBar>();
            actorEventManger = GetComponent<ActorEventManger>();
            if (actorEventManger != null)
            {
                actorEventManger.onDamageHealth += DamageHealth;
                actorEventManger.onDamageStamina += DamageStamina;
                actorEventManger.onDamageMana += DamageMagic;
            }
        }
        private void OnDestroy()
        {
            if (actorEventManger != null)
            {
                actorEventManger.onDamageHealth -= DamageHealth;
                actorEventManger.onDamageStamina -= DamageStamina;
                actorEventManger.onDamageMana -= DamageMagic;
            }
        }


        private void DamageHealth(float fDamage)
        {
            if (!isInvincible && deathState < DeathState.dying)
            {
                Debug.Log("ActorHealth => DamageHealth: " + fDamage);
                float previousHealth = health;
                health -= Math.Abs(fDamage);
                actorEventManger.UpdateStatBar_Health(health / (float)healthMax);
                if (health <= 0)
                {
                    actorEventManger.ActorDeath();
                }
                else
                {
                    if (health / previousHealth < 0.8f) actorEventManger.EvaluateFleeingState(healthPercent);
                }
            }
        }

        void onHeal(float fAmount)
        {
            if (deathState < DeathState.dying)
            {
                health += Math.Abs(fAmount);
                actorEventManger.UpdateStatBar_Health(health / (float)healthMax);
            }
        }


        // STAMINA
        [SerializeField] private float _stamina = 100f;
        public float stamina
        {
            get => _stamina;
            set
            {
                if (immortal)
                {
                    _stamina = Mathf.Clamp(value, 10f, (float)staminaMax);
                }
                else
                {
                    _stamina = value;
                    _stamina = Mathf.Clamp(_stamina, 0, (float)staminaMax);
                }
                _staminaPercent = ((float)stamina / (float)staminaMax);
            }
        }
        public int staminaMax = 100;
        public float staminaRegenRate = 6f;
        [SerializeField] private float _staminaPercent = 1f;
        public float staminaPercent
        {
            get => _staminaPercent;
            private set
            {
                _staminaPercent = value;
            }
        }
        private void DamageStamina(float fDamage)
        {
            if (isDead())
                return;

            Debug.Log("ActorHealth => DamageStamina: " + fDamage);
            stamina -= Math.Abs(fDamage);
            actorEventManger.UpdateStatBar_Stamina(stamina / (float)staminaMax);
        }


        // MAGIC
        [SerializeField] private float _magic = 100f;
        public float magic
        {
            get => _magic;
            set
            {
                if (immortal)
                {
                    _magic = Mathf.Clamp(value, 10f, (float)magicMax);
                }
                else
                {
                    _magic = value;
                    _magic = Mathf.Clamp(_magic, 0, (float)magicMax);
                }
                _magicPercent = ((float)magic / (float)magicMax);
            }
        }
        public int magicMax = 100;
        public float magicRegenRate = 3f;
        [SerializeField] private float _magicPercent = 1f;
        public float magicPercent
        {
            get => _magicPercent;
            private set
            {
                _magicPercent = value;
            }
        }
        private void DamageMagic(float fDamage)
        {
            if (isDead())
                return;

            Debug.Log("ActorHealth => DamageMagic: " + fDamage);
            magic -= Math.Abs(fDamage);
            actorEventManger.UpdateStatBar_Magic(magic / (float)magicMax);
        }

    }

}