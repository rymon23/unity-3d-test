using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;
using UnityEngine.AI;

public enum ActorGroundNavigationSpeed
{
    walk = 0,
    speedWalk,
    jog,
    run,
    sprint
}

public class NavAgentController : MonoBehaviour
{
    [SerializeField]
    float baseSpeed = 2.8f;

    [SerializeField]
    float speedMult_default = 1f;

    [SerializeField]
    float speedMult_stagger = 0.33f;

    [SerializeField]
    float speedMult_blocking = 0.8f;

    [SerializeField]
    float speedMult_midAtack = 0.2f;

    [SerializeField]
    float speedMult_sprint = 1.4f;

    [SerializeField]
    float accelerationWalking = 1f;

    [SerializeField]
    float accelerationRunning = 50f;

    // [SerializeField]
    // public ActorGroundNavigationSpeed
    //     groundNavigationSpeed = ActorGroundNavigationSpeed.walk;

    [SerializeField] private ActorGroundNavigationSpeed _groundNavigationSpeed = ActorGroundNavigationSpeed.walk;
    public bool bDebugMode = false;

    public ActorGroundNavigationSpeed groundNavigationSpeed
    {
        get => _groundNavigationSpeed;
    set
        {   
            if (value > ActorGroundNavigationSpeed.sprint)  {
                _groundNavigationSpeed = ActorGroundNavigationSpeed.sprint;
            }
                _groundNavigationSpeed = value;

            // _groundNavigationSpeed = (ActorGroundNavigationSpeed)Mathf.Clamp((float)_groundNavigationSpeed, 0,(float)ActorGroundNavigationSpeed.sprint);
        }        
    }


    ActorEventManger actorEventManger;

    NavMeshAgent agent;

    CombatStateData combatStateData;

    AnimationState animationState;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        actorEventManger = GetComponent<ActorEventManger>();
        animationState = GetComponent<AnimationState>();
        combatStateData = GetComponent<CombatStateData>();
        if (actorEventManger != null)
        {
            // actorEventManger.onTriggerAnim_Stagger += TriggerAnim_Stagger;
            // actorEventManger.onTriggerAnim_Block += TriggerAnim_Block;
        }
    }

    private void Update()
    {
        EvaluateAgentSpeed();
        EvaluateNavAcceleration();
    }

    private void EvaluateAgentSpeed()
    {
        if (animationState.isSprinting)
        {
            agent.speed = baseSpeed * speedMult_sprint;
        }
        else if (animationState.isAttacking)
        {
            agent.speed = baseSpeed * speedMult_midAtack;
        }
        else if (animationState.IsStaggered())
        {
            agent.speed = baseSpeed * speedMult_stagger;
        }
        else if (animationState.IsInBlockHitFame())
        {
            agent.speed = baseSpeed * speedMult_blocking;
        }
        else
        {
            if (combatStateData.IsInCombat())
            {
                baseSpeed = 2.8f;
            }
            else
            {
                switch (groundNavigationSpeed)
                {
                    case ActorGroundNavigationSpeed.sprint:
                        baseSpeed = 3f;
                        break;
                    case ActorGroundNavigationSpeed.run:
                        baseSpeed = 2.6f;
                        break;
                    case ActorGroundNavigationSpeed.jog:
                        baseSpeed = 1.9f;
                        break;
                    case ActorGroundNavigationSpeed.speedWalk:
                        baseSpeed = 1.45f;
                        break;
                    default:
                        baseSpeed = 0.8f;
                        break;
                }
            }

            agent.speed = baseSpeed * speedMult_default;
        }
    }

    private void EvaluateNavAcceleration()
    {
        if (groundNavigationSpeed >= ActorGroundNavigationSpeed.jog)
        {
            agent.acceleration = accelerationRunning;
        }
        else
        {
            agent.acceleration = accelerationWalking;
        }
    }
}
