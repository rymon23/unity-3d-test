using Hybrid.Components;
using UnityEngine;

public enum ActorAction
{
    attack,
    block,
    cast,
    bash
}

public class ActorActionController : MonoBehaviour
{
    ActorEventManger actorEventManger;

    Animator animator;

    AnimationState animationState;

    ActorHealth actorHealth;

    CombatStateData combatStateData;

    Targeting targeting;

    ActorFOV myFOV;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animationState = GetComponent<AnimationState>();
        actorHealth = GetComponent<ActorHealth>();
        combatStateData = GetComponent<CombatStateData>();
        targeting = GetComponent<Targeting>();
        myFOV = GetComponent<ActorFOV>();

        actorEventManger = GetComponent<ActorEventManger>();
    }

    private void Start()
    {
        if (actorEventManger != null)
            actorEventManger.onEvaluateAndAction += onEvaluateAndAction;
    }

    private void OnDestroy()
    {
        if (actorEventManger != null)
            actorEventManger.onEvaluateAndAction -= onEvaluateAndAction;
    }

    private void onEvaluateAndAction(ActorAction actorAction)
    {
        switch (actorAction)
        {
            case ActorAction.attack:
                EvaluateCombatStrategy();
                break;
            default:
                break;
        }
    }

    public void EvaluateCombatStrategy()
    {
        if (targeting.currentTarget == null) return;

        GameObject enemyGameObject = targeting.currentTarget.gameObject;
        AnimationState enemyAnimState =
            enemyGameObject.GetComponent<AnimationState>();
        CombatStateData enemyCombatStateData =
            enemyGameObject.GetComponent<CombatStateData>();

        AIDecisionController myDecisionController =
            GetComponent<AIDecisionController>();

        float attackDistance =
            Vector3
                .Distance(targeting.attackPos,
                targeting.currentTarget.transform.position);
        targeting.targetAttackDistance = attackDistance;

        float distance =
            Vector3
                .Distance(transform.position,
                targeting.currentTarget.transform.position);
        targeting.targetDistance = distance;

        // Enemy Is Attacking
        //Attack
        //Block
        targeting.targetDistance = distance;

        Vector3 targetPos =
            targeting.currentTarget.transform.position +
            (targeting.currentTarget.transform.up * 1.2f);

        // bool hasTargetInFOV = UtilityHelpers.IsInFOVScope(myFOV.viewPoint, targetPos, myFOV.maxAngle, myFOV.maxRadius);
        bool hasTargetInFOV =
            UtilityHelpers
                .IsTargetDetectable(myFOV.viewPoint,
                targetPos,
                myFOV.maxAngle,
                myFOV.maxRadius);

        Debug.Log("hasTargetInFOV: " + hasTargetInFOV);

        if (hasTargetInFOV)
        {
            EvaluateDefend (distance);

            EvaluateAttack (distance);

            EvaluateCast (distance);
        }
    }

    private void EvaluateDefend(float targetdistance)
    {
        Debug.Log("EvaluateDefend");

        if (!animationState.IsAbleToBlock() || !animationState.IsAbleToAttack())
            return;

        CombatStateData enemyCombatStateData =
            targeting.currentTarget.GetComponent<CombatStateData>();

        if (
            targetdistance < enemyCombatStateData.meleeAttackRange &&
            targetdistance > 0.01f
        )
        {
            AnimationState enemyAnimState =
                targeting.currentTarget.GetComponent<AnimationState>();

            if (
                enemyAnimState.isAttacking &&
                enemyAnimState.attackAnimationState <=
                AttackAnimationState.attackPreHit
            )
            {
                if (animationState.IsAbleToBlock())
                {
                    T_Block();
                }
            }
        }
    }

    private void EvaluateCast(float targetdistance)
    {
        Debug.Log("EvaluateCast");

        ActorSpells actorSpells = GetComponent<ActorSpells>();

        //Check for Spells
        if (
            actorSpells != null &&
            actorSpells.spellsTemp != null &&
            actorSpells.spellsTemp.Length > 0
        )
        {
            // Check if range and can cast
            if (
                targetdistance > 1.6f &&
                !animationState.isCasting &&
                actorHealth.magic >= actorSpells.spellsTemp[0].baseMagicCost
            )
            {
                animator.SetTrigger("tCastSpell");
                return;
            }
        }
    }

    private void EvaluateAttack(float targetdistance)
    {
        Debug.Log("EvaluateAttack");
        if (animationState.IsAbleToAttack()) T_MeleeAttack();
    }

    private void EvaluateMeleeAttack()
    {
    }

    private void T_MeleeAttack()
    {
        int currentBlockVariant =
            ((int) animator.GetFloat("fAnimMeleeAttackType"));
        int maxBlendTreeLength = 3;
        int nextAttackType =
            (
            currentBlockVariant +
            UnityEngine.Random.Range(1, maxBlendTreeLength)
            ) %
            maxBlendTreeLength;

        animator.SetFloat("fAnimMeleeAttackType", nextAttackType);
        animator.SetTrigger("tAttack");

        // int rand = UnityEngine.Random.Range(0, 100);
        // if (rand < 78)
        // {
        //     enemyCombatStateData =
        //         enemyGameObject.GetComponent<CombatStateData>();
        //     if (enemyCombatStateData != null)
        //     {
        //         enemyCombatStateData.bShouldBlock = rand < 66;
        //     }
        // }
    }

    private void T_MagicBlock()
    {
    }

    private void T_Dodge()
    {
    }

    private void T_Block()
    {
        // if (UnityEngine.Random.Range(0, 100) < 80)
        // {
        //     int currentBlockVariant = ((int)animator.GetFloat("fAnimBlockType"));
        //     int maxBlendTreeLength = 6;
        //     int nextBlockType = (currentBlockVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;
        //     animator.SetFloat("fAnimBlockType", nextBlockType);
        //     animator.SetTrigger("BlockHit");
        // }
        // else
        // {
        //     animator.SetTrigger("Dodge");
        // }
    }

    private void T_Bash()
    {
        // if (attackDistance < 1.2f)
        // {
        //     animator.SetTrigger("tBash");
        //     return;
        // }
    }
}
