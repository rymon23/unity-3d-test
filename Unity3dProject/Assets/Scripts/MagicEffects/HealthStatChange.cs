using UnityEngine;

namespace Hybrid.Components
{
    public class HealthStatChange : MonoBehaviour, IMagicEffect
    {
        SpellEventManager spellEventManager;

        SpellInstanceData instanceData;

        public enum HealthStat
        {
            health,
            magic,
            stamina
        }

        public enum Behavior
        {
            damage,
            restore,
            reset
        }

        [SerializeField]
        private HealthStat healthStat = HealthStat.health;

        [SerializeField]
        private Behavior behavior;

        [SerializeField]
        private float duration = 0.1f;

        public float baseAmount = 90f;

        public bool amountIsStatic = false;

        [SerializeField]
        public void OnEffectStart(SpellInstanceData spellInstance)
        {
            instanceData = spellInstance;

            Debug
                .Log("HealthStatChange: " +
                behavior +
                " " +
                healthStat +
                " - " +
                baseAmount +
                "pts. Target: " +
                instanceData.target.name);

            Damage(instanceData.target, instanceData.caster);

            Destroy(gameObject, 0.1f);
        }

        public void OnEffectUpdate(SpellInstanceData spellInstance)
        {
            Debug
                .Log("HealthStatChange: " +
                behavior +
                " " +
                healthStat +
                " - " +
                baseAmount +
                "pts. Target: " +
                instanceData.target.name);
        }

        public void OnEffectFinish(SpellInstanceData spellInstance)
        {
        }

        private void Awake()
        {
            spellEventManager = GetComponent<SpellEventManager>();
            if (spellEventManager != null)
            {
                spellEventManager.onEffectStart += OnEffectStart;
                spellEventManager.onEffectUpdate += OnEffectUpdate;
            }
        }

        private void OnDestroy()
        {
            spellEventManager = GetComponent<SpellEventManager>();
            if (spellEventManager != null)
            {
                spellEventManager.onEffectStart -= OnEffectStart;
                spellEventManager.onEffectUpdate -= OnEffectUpdate;
            }
        }

        public void Damage(GameObject target, GameObject sender)
        {
            Debug
                .Log("Spell: Damage Health effect: target: " +
                target.name +
                " / sender: " +
                sender.name);

            ActorEventManger actorEventManger =
                target.GetComponent<ActorEventManger>();
            actorEventManger.DamageHealth (baseAmount);
            actorEventManger.TriggerAnim_Stagger(0);
        }
    }
}
