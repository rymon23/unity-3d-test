using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public class ActiveSpellController : MonoBehaviour
    {
        [SerializeField]
        private float selfDestructTimer = 6f;

        private float updateTimer = 0.1f;

        private float updateTime = 1f;

        SpellEventManager spellEventManager;

        private SpellInstanceData instanceData;

        [SerializeField]
        private MagicSpell baseMagicSpell;

        public void SetBaseMagicSpell(MagicSpell magicSpell)
        {
            if (baseMagicSpell) baseMagicSpell = magicSpell;
        }

        public void FireMagicEffects(SpellInstanceData spellInstanceData)
        {
            instanceData = spellInstanceData;

            Debug
                .Log("ActiveSpellController - FireMagicEffects: sender: " +
                instanceData.caster.name);

            if (spellEventManager != null)
            {
                spellEventManager.EffectStart (instanceData);
            }
            // Destroy (gameObject, selfDestructTimer);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            // if (spellEventManager )
            updateTimer -= Time.deltaTime;
            if (updateTimer < 0)
            {
                spellEventManager.EffectUpdate (instanceData);
            }
        }

        private void Awake()
        {
            spellEventManager = GetComponent<SpellEventManager>();
        }

        private void Start()
        {
            spellEventManager = GetComponent<SpellEventManager>();
            updateTimer = updateTime;
        }

        private void OnDestroy()
        {
            spellEventManager = GetComponent<SpellEventManager>();
            if (spellEventManager != null)
            {
            }
        }
    }
}
