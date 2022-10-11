using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Hybrid.Components;

public class MoveOnAgent : MonoBehaviour
{

    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator animator;
    [SerializeField] CombatStateData combatStateData;
    [SerializeField] AnimationState animationState;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        combatStateData = GetComponent<CombatStateData>();
        animationState = GetComponent<AnimationState>();
    }

    // Update is called once per frame
    void Update()
    {
        if (animator != null)
        {
            bool bIsMoving = agent.velocity.magnitude > 0.01f;

            animator.SetBool("move", bIsMoving);
            animator.SetFloat("Speed", Mathf.Abs(agent.velocity.magnitude));

            animationState.isMoving = bIsMoving;

            if (combatStateData != null)
            {
                animator.SetFloat("YAxis", agent.velocity.magnitude * combatStateData.currentVelocityZ);
                animator.SetFloat("XAxis", agent.velocity.magnitude * combatStateData.currentVelocityX);
            }
            else
            {
                animator.SetFloat("YAxis", agent.velocity.magnitude);
            }
        }
    }
}
