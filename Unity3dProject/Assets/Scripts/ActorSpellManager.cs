using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;

namespace Hybrid.Components
{
    public enum SpellCastingBehavior
    {
        offensive,
        defensive = 1
    }

    public enum SpellCastingRange
    {
        midRange = 0,
        longRange = 1,
        closeRange = 2
    }

    public class ActorSpellManager : MonoBehaviour
    {
        private ActorHealth myHealth;

        private ActorSpells mySpells;

        // public List<MagicSpell> defensiveSpells;
        // public List<MagicSpell> offensive;
        private ActorEventManger myEventManger;

        private Targeting myTargeting;

        GameObject currentSpell;

        float fEvaluationTimer = 0.1f;

        float fEvaluationUpdateTime = 3f;

        [SerializeField]
        private float fCloseRangeMax = 2f;

        [SerializeField]
        private float fMidRangeMax = 3f;

        [SerializeField]
        private SpellCastingBehavior currentCastingBehavior;

        [SerializeField]
        private SpellCastingRange currentCastingRange;

        private void EvaluateSpells()
        {
            bool hasSpells =
                mySpells != null &&
                mySpells.spellsTemp != null &&
                mySpells.spellsTemp.Length > 0;
        }

        private void EvaluateCastingBehavior()
        {
            if (myHealth.magicPercent < 0.45)
            {
                currentCastingBehavior = SpellCastingBehavior.defensive;
            }
            else
            {
                currentCastingBehavior = SpellCastingBehavior.offensive;
            }
            EvaluateSpells();
            EvaluateCastingRange();
        }

        private void EvaluateCastingRange()
        {
            if (myTargeting.currentTarget != null)
            {
                float distance =
                    Vector3
                        .Distance(this.transform.position,
                        myTargeting.currentTarget.transform.position);

                if (distance < fCloseRangeMax)
                {
                    currentCastingRange = SpellCastingRange.closeRange;
                }
                else if (distance < fMidRangeMax)
                {
                    currentCastingRange = SpellCastingRange.midRange;
                }
                else
                {
                    currentCastingRange = SpellCastingRange.longRange;
                }
            }
        }

        private void CastSpell()
        {
        }

        private void Start()
        {
            myHealth = GetComponent<ActorHealth>();
            mySpells = GetComponent<ActorSpells>();
            myTargeting = GetComponent<Targeting>();
            myEventManger = GetComponent<ActorEventManger>();
            if (myEventManger != null)
            {
                myEventManger.onEvaluateCastingBehavior +=
                    EvaluateCastingBehavior;
            }
        }

        private void OnDestroy()
        {
            if (myEventManger != null)
            {
                myEventManger.onEvaluateCastingBehavior -=
                    EvaluateCastingBehavior;
            }
        }
    }
}
