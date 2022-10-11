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
                _healthPercent = ((float)health / (float)healthMax + 0.001f);
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
        public StatBar healthBar;
        public StatBar staminaBar;
        public StatBar magicBar;
        ActorEventManger actorEventManger;


        private void Awake()
        {
            health = healthMax;
            stamina = staminaMax;
            magic = magicMax;
        }

        private void Start()
        {
            // healthBar = GetComponentInChildren<Sta>();
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


        private void DamageHealth(float fDamage) //, bool ignoreArmor = false)
        {
            if (!isInvincible && deathState < DeathState.dying)
            {
                Debug.Log("ActorHealth => DamageHealth: " + fDamage);
                float previousHealth = health;

                float currentArmor = energyArmor;
                currentArmor -= fDamage;

                DamageEnergyArmor(fDamage);

                float postArmordamage = currentArmor > 0 ? 0 : -currentArmor;

                health -= Math.Abs(postArmordamage);
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
                _stamina = value;
                _stamina = Mathf.Clamp(_stamina, 0, (float)staminaMax);
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
                _magic = value;
                _magic = Mathf.Clamp(_magic, 0, (float)magicMax);
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


        // ENERGY ARMOR
        [SerializeField] private float _energyArmor = 0;
        public float energyArmor
        {
            get => _energyArmor;
            set
            {
                _energyArmor = value;
                _energyArmor = Mathf.Clamp(_energyArmor, 0, (float)energyArmorMax);
                _energyArmorPercent = energyArmorMax <= 0 ? 0 : ((float)energyArmor / (float)energyArmorMax);
            }
        }
        public int energyArmorMax = 0;
        public float energyArmorRegenRate = 8f;
        public float energyArmorRegenDelay = 6f;
        public float energyArmorRegenDelayTimer = 0f;
        [SerializeField] private float _energyArmorPercent = 0f;
        public float energyArmorPercent
        {
            get => _energyArmorPercent;
            private set
            {
                _energyArmorPercent = value;
            }
        }
        private void DamageEnergyArmor(float fDamage)
        {
            if (isDead())
                return;

            Debug.Log("ActorHealth => DamageEnergyArmor: " + fDamage);
            energyArmor -= Math.Abs(fDamage);
            // actorEventManger.UpdateStatBar_Stamina(energyArmor / (float)energyArmorMax);

            if (energyArmor <= 0)
            {
                energyArmorRegenDelayTimer = energyArmorRegenDelay;
            }
            else
            {
                //Should evaluate flee state
            }
        }

    }
}