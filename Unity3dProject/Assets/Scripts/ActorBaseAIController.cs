using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.AI;

public class ActorBaseAIController : MonoBehaviour
{
    public enum DefaultBehaviorState
    {
        idle = 0,
        follow
    }

    [SerializeField]
    private DefaultBehaviorState
        currentdefaultBehaviorState = DefaultBehaviorState.idle;

    [SerializeField]
    private bool debug_gizmo = false;

    [SerializeField]
    private bool debug_WeaponOut = false;

    [SerializeField]
    private bool debug_StopMovement = false;

    [SerializeField]
    private bool debug_IsRestrained = false;

    ActorAIStateData aIStateData;

    ActorEventManger eventManger;

    ActorBaseBT actorBaseBT;

    Targeting targeting;

    CombatStateData combatStateData;

    AnimationState animationState;

    ActorHealth actorHealth;

    NavMeshAgent agent;

    ActorNavigationData actorNavigationData;

    Weapon weapon;

    GameObject combatTarget;

    CombatStateData targetCombatStateData;

    ActorFOV actorFOV;

    Follower follower;

    public float rotationSpeed = 3.5f;

    public float rotationSpeed_Strafe = 8.5f;

    public float rotationSpeed_fallBack = 6.5f;

    public float velocityX;

    public float velocityZ;

    public float currentVelocity;

    public float remainingDist;

    public Vector3 attackPos;

    public AttackPosColor attackPosColor = AttackPosColor.red;

    void Start()
    {
        aIStateData = this.GetComponent<ActorAIStateData>();
        eventManger = this.GetComponent<ActorEventManger>();
        agent = this.GetComponent<NavMeshAgent>();
        targeting = this.GetComponent<Targeting>();
        combatStateData = this.GetComponent<CombatStateData>();
        animationState = this.GetComponent<AnimationState>();
        actorHealth = this.GetComponent<ActorHealth>();
        actorFOV = this.GetComponent<ActorFOV>();
        actorNavigationData = this.GetComponent<ActorNavigationData>();
        actorBaseBT = this.GetComponent<ActorBaseBT>();
        follower = this.GetComponent<Follower>();
        follower = this.GetComponent<Follower>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (actorHealth.isDead()) return;

        if (debug_StopMovement || debug_IsRestrained)
        {
            if (agent != null && agent.destination != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        TrackAICurrentSpeed();

        if (debug_IsRestrained) return;

        remainingDist = agent.remainingDistance;

        switch (aIStateData.aiBehaviorState)
        {
            case AIState_Behavior.combat:
                HandleBehavior_Combat();
                return;
            default:
                HandleBehavior_Default();
                return;
        }
    }

    private void HandleBehavior_Combat()
    {
        switch (combatStateData.combatState)
        {
            case CombatState.alerted:
                HandleCombatState_Alerted();
                return;
            case CombatState.searching:
                HandleCombatState_Searching();
                return;
            default:
                HandleCombatState_Active();
                return;
        }
    }

    private void HandleBehavior_Default()
    {
        // Follower
        if (follower != null && follower.HasTarget())
        {
            currentdefaultBehaviorState = DefaultBehaviorState.follow;

            if (actorBaseBT != null) actorBaseBT.allowPandaBTTasks = false;

            FollowTarget();
            return;
        }

        if (actorBaseBT != null) actorBaseBT.allowPandaBTTasks = true;

        currentdefaultBehaviorState = DefaultBehaviorState.idle;

        if (actorNavigationData.travelPosition != null)
        {
            Seek(actorNavigationData.travelPosition.position);
        }
    }

    private void HandleCombatState_Active()
    {
        if (targeting == null) return;
        if (targeting.currentTarget == null) return;

        if (combatTarget != targeting.currentTarget)
        {
            combatTarget = targeting.currentTarget.gameObject;
            targetCombatStateData =
                combatTarget.GetComponent<CombatStateData>();
        }

        CalculateAttackPositon();

        float distance =
            Vector3
                .Distance(this.transform.position,
                combatTarget.transform.position);
        bool hasTargetInFOV =
            UtilityHelpers
                .HasFOVAngle(this.transform,
                combatTarget.transform.position,
                actorFOV.maxAngle,
                actorFOV.maxRadius);

        bool bShouldHoldPosition = false;

        //HOLD POSITION BEHAVIOR
        if (
            combatStateData.combatNavigationState ==
            CombatNavigationState.holdPosition &&
            actorNavigationData.holdPositionCenter != null
        )
        {
            float holdPositionDistance =
                Vector3
                    .Distance(gameObject.transform.position,
                    actorNavigationData.holdPositionCenter.position);

            bShouldHoldPosition =
                holdPositionDistance > actorNavigationData.holdPositionRadius;

            if (bShouldHoldPosition)
            {
                targeting.fallbackPosition =
                    actorNavigationData.holdPositionCenter.position;
                Fallback(actorNavigationData.holdPositionCenter.position);

                // Debug.Log("Fallback to hold position!");
                // return;
            }
        }

        if (hasTargetInFOV && distance > 0.01f)
        {
            if (weapon == null) weapon = this.GetComponentInChildren<Weapon>();
            if (weapon != null)
            {
                if (
                    weapon.weaponType == WeaponType.gun &&
                    UtilityHelpers
                        .HasShotLinedUp(weapon.firePoint,
                        combatTarget.transform.position)
                )
                {
                    weapon.bShouldFire = true;
                }
            }

            if (!bShouldHoldPosition)
            {
                if (
                    combatStateData.combatMovementType >=
                    CombatMovementType.flankRight
                )
                {
                    Strafe(combatStateData.combatMovementType ==
                    CombatMovementType.flankRight);
                }
                else if (
                    combatStateData.combatMovementType ==
                    CombatMovementType.fallBack
                )
                {
                    Fallback(Vector3.zero);
                }
                else
                {
                    if (distance < combatStateData.meleeAttackRange)
                    {
                        Fallback(Vector3.zero);
                    }
                    else
                    {
                        Seek(combatTarget.transform.position);
                    }
                }
            }
        }
        else
        {
            if (!bShouldHoldPosition)
            {
                Seek(combatTarget.transform.position);
            }
            UpdateRotation();
        }
    }

    private void HandleCombatState_Alerted()
    {
        Debug.Log("HandleCombatState_Alerted!");

        if (
            targeting.alertedPosition != Vector3.zero &&
            agent.destination != targeting.alertedPosition
        )
        {
            agent.SetDestination(targeting.alertedPosition);
            agent.updateRotation = true;
        }
    }

    private void HandleCombatState_Searching()
    {
        if (
            targeting.searchCenterPos != Vector3.zero &&
            agent.destination != targeting.searchCenterPos
        )
        {
            agent.SetDestination(targeting.searchCenterPos);
            agent.updateRotation = true;
        }
    }

    void CalculateAttackPositon()
    {
        // Predict attack trajectory
        Vector3 targetDir =
            combatTarget.transform.position - this.transform.position;

        // Check for & clamp negative velocityZ
        float forwardMult;
        if (velocityZ < 0.01f)
        {
            forwardMult = 0.01f;
            targetDir =
                this.transform.position - combatTarget.transform.position;
        }
        else
        {
            forwardMult = velocityZ;
        }

        float lookAhead;
        if (targetCombatStateData != null)
        {
            lookAhead =
                targetDir.magnitude /
                ((agent.speed) + targetCombatStateData.currentSpeed);
        }
        else
        {
            lookAhead = targetDir.magnitude / (agent.speed);
        }

        attackPos =
            (
            transform.position +
            transform.forward *
            (lookAhead + (combatStateData.meleeAttackRange * forwardMult))
            );
        targeting.attackPos = attackPos;
    }

    void UpdateRotation()
    {
        Vector3 lookAtGoal =
            new Vector3(combatTarget.transform.position.x,
                this.transform.position.y,
                combatTarget.transform.position.z);

        //Update Rotation
        Vector3 direction = lookAtGoal - this.transform.position;
        this.transform.rotation =
            Quaternion
                .Slerp(this.transform.rotation,
                Quaternion.LookRotation(direction),
                rotationSpeed * Time.deltaTime);
    }

    void TrackAICurrentSpeed()
    {
        if (combatStateData == null || agent == null) return;

        if (debug_StopMovement && agent.destination != null && !agent.isStopped)
        {
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;
        }

        Vector3 vel = agent.velocity;
        velocityZ = Vector3.Dot(vel.normalized, transform.forward);
        velocityX = Vector3.Dot(vel.normalized, transform.right);

        currentVelocity = vel.magnitude;

        vel.Normalize();
        vel *= Time.deltaTime;
        combatStateData.currentSpeed = agent.velocity.magnitude;
        combatStateData.currentVelocityZ = velocityZ;
        combatStateData.currentVelocityX = velocityX;

        // Handle Sprinting
        // if (velocityZ > 0.2) {
        //     if (agent.remainingDistance > 8f) {
        //         animationState.isSprinting = true;
        //     } else {
        //         animationState.isSprinting = false;
        //     }
        // }
    }

    void FollowTarget()
    {
        if (debug_StopMovement) return;

        // Transform target = follower.GetTarget();
        // if (follower.GetGoal() == Vector3.zero
        // || Vector3.Distance(target.position, follower.GetGoal()) > follower.distanceMax)
        // // || agent.remainingDistance <= agent.stoppingDistance)
        // {
        //     if (follower.followBehavior == FollowBehavior.relaxed)
        //     {
        //         Vector3 newPos = UtilityHelpers.GetRandomNavmeshPoint(follower.distanceMax * 0.5f, UtilityHelpers.GetFrontPosition(target, follower.distanceMax * 0.45f));
        //         follower.SetGoal(newPos);
        //     }
        //     else if (follower.followBehavior == FollowBehavior.take_point)
        //     {
        //         Vector3 newPos = UtilityHelpers.GetRandomNavmeshPoint(follower.distanceMax * 0.28f, UtilityHelpers.GetFrontPosition(target, follower.distanceMax * 0.7f));
        //         follower.SetGoal(newPos);
        //     }
        //     else if (follower.followBehavior == FollowBehavior.tail)
        //     {
        //         Vector3 newPos = UtilityHelpers.GetRandomNavmeshPoint(follower.distanceMax * 0.5f, UtilityHelpers.GetBehindPosition(target, follower.distanceMax * 0.45f));
        //         follower.SetGoal(newPos);
        //     }
        //     else
        //     {
        //         Vector3 newPos = UtilityHelpers.GetRandomNavmeshPoint(follower.distanceMax, target.position);
        //         follower.SetGoal(newPos);
        //     }
        // }
        if (
            agent.destination == follower.GetGoal() &&
            agent.remainingDistance <= agent.stoppingDistance &&
            !agent.pathPending
        )
        {
            follower.followState = FollowState.idle;
        }
        else
        {
            follower.followState = FollowState.catch_up;
            agent.SetDestination(follower.GetGoal());
        }

        agent.updateRotation = true;
    }

    void Seek(Vector3 location)
    {
        if (debug_StopMovement) return;

        agent.SetDestination (location);
        agent.updateRotation = true;

        if (targeting.fallbackPosition != Vector3.zero)
        {
            targeting.fallbackPosition = Vector3.zero;
        }
    }

    void Fallback(Vector3 location)
    {
        if (debug_StopMovement) return;

        if (
            targeting.fallbackPosition == Vector3.zero ||
            agent.remainingDistance <= agent.stoppingDistance
        )
        {
            if (location != Vector3.zero)
            {
                targeting.fallbackPosition = location;
            }
            else
            {
                Vector3 newFallbackPos =
                    getFallbackPosition(this.transform, 14f);
                targeting.fallbackPosition = newFallbackPos;
            }
        }
        agent.updateRotation = false;
        agent.SetDestination(targeting.fallbackPosition);

        //Update Rotation
        Vector3 lookAtGoal =
            new Vector3(combatTarget.transform.position.x,
                this.transform.position.y,
                combatTarget.transform.position.z);
        Vector3 direction = lookAtGoal - this.transform.position;
        this.transform.rotation =
            Quaternion
                .Slerp(this.transform.rotation,
                Quaternion.LookRotation(direction),
                rotationSpeed_fallBack * Time.deltaTime);
    }

    public Vector3 getFallbackPosition(Transform transform, float distance)
    {
        return UtilityHelpers
            .GetRandomNavmeshPoint(distance * 0.6f,
            UtilityHelpers.GetBehindPosition(transform, distance * 1.5f));
    }

    void Strafe(bool left)
    {
        if (debug_StopMovement) return;

        Vector3 offsetTarget =
            left
                ? this.transform.position - combatTarget.transform.position
                : combatTarget.transform.position - transform.position;
        Vector3 dir = Vector3.Cross(offsetTarget, Vector3.up);
        agent.SetDestination(transform.position + dir);

        Vector3 lookPos = combatTarget.transform.position - transform.position;
        lookPos.y = 0;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        transform.rotation =
            Quaternion
                .Slerp(transform.rotation,
                rotation,
                rotationSpeed_Strafe * Time.deltaTime);
    }


#region Debugging

    Vector4 debugDestinationColor;

    private void OnDrawGizmos()
    {
        if (!debug_gizmo) return;

        if (debugDestinationColor == Vector4.zero)
            debugDestinationColor = UtilityHelpers.GenerateRandomRGB();

        if (agent != null)
        {
            Gizmos.color = debugDestinationColor;
            Gizmos.DrawWireSphere(agent.destination, agent.stoppingDistance);
        }

        if (
            combatStateData != null &&
            actorNavigationData != null &&
            actorNavigationData.travelPosition
        )
        {
            if (
                combatStateData.combatNavigationState ==
                CombatNavigationState.holdPosition &&
                actorNavigationData.travelPosition != null
            )
            {
                Gizmos.color = Color.white;
                Gizmos
                    .DrawWireSphere(actorNavigationData.travelPosition.position,
                    actorNavigationData.holdPositionRadius);
            }
        }

        switch (attackPosColor)
        {
            case AttackPosColor.green:
                Gizmos.color = Color.green;
                break;
            case AttackPosColor.yellow:
                Gizmos.color = Color.yellow;
                break;
            default:
                Gizmos.color = Color.red;
                break;
        }

        if (combatTarget != null)
        {
            Gizmos.DrawWireSphere(targeting.attackPos, 0.45f);
            Gizmos
                .DrawRay(transform.position,
                (targeting.attackPos - transform.position));
        }
    }
#endregion

}
