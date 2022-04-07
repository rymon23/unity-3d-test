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


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        combatStateData = GetComponent<CombatStateData>();
    }

    // Update is called once per frame
    void Update()
    {
        if (animator != null)
        {
            animator.SetBool("move", agent.velocity.magnitude > 0.01f);

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
