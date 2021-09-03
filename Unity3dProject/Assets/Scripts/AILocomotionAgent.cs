using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AILocomotionAgent : MonoBehaviour
{
	Animator anim;
	UnityEngine.AI.NavMeshAgent agent;
    Vector2 groundDeltaPosition;
	Vector2 velocity = Vector2.zero;

	void Start () {
		anim = GetComponentInChildren<Animator>();
		agent = GetComponent<UnityEngine.AI.NavMeshAgent> ();
		agent.updatePosition = false;
	}
	
	void Update () {
		Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

		// Map 'worldDeltaPosition' to local space
        groundDeltaPosition.x = Vector3.Dot (transform.right, worldDeltaPosition);
        groundDeltaPosition.y = Vector3.Dot (transform.forward, worldDeltaPosition);
        velocity = (Time.deltaTime > 1e-5f) ? groundDeltaPosition / Time.deltaTime : velocity = Vector2.zero;
		bool shouldMove = velocity.magnitude > 0.025f && agent.remainingDistance > agent.radius;

		// Update animation parameters
		anim.SetBool("move", shouldMove);
		anim.SetFloat ("XAxis", velocity.x);
		anim.SetFloat ("YAxis", agent.velocity.magnitude);
		// anim.SetFloat ("YAxis", velocity.y);

		LookAt lookAt = GetComponent<LookAt>();
		if (lookAt)
			lookAt.lookAtTargetPosition = agent.steeringTarget + transform.forward;

		// Pull character towards agent
		if (worldDeltaPosition.magnitude > agent.radius)
			transform.position = agent.nextPosition - 0.9f*worldDeltaPosition;
	}

	void OnAnimatorMove () {
		// Update postion to agent position
		// transform.position = agent.nextPosition;

		// Update position based on animation movement using navigation surface height
		Vector3 position = anim.rootPosition;
		position.y = agent.nextPosition.y;
		transform.position = position;


		if (Time.deltaTime > 0)
		{
			Vector3 v = (anim.deltaPosition * 1f) / Time.deltaTime;
			Rigidbody m_Rigidbody = GetComponent<Rigidbody>();
			// we preserve the existing y part of the current velocity.
			v.y = m_Rigidbody.velocity.y;
			m_Rigidbody.velocity = v;
		}
	}

}
