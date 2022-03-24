using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hybrid.Components;

public class MoveLocal : MonoBehaviour
{
    Targeting targeting;
    public Transform goal;

    public float speed = 2.0f;
    public float accuracy = 1.0f;
    public float rotationSpeed = 4.5f;

    private void Start()
    {
        targeting = this.GetComponent<Targeting>();

    }

    private void LateUpdate()
    {
        if (targeting != null)
        {
            goal = targeting.currentTarget.gameObject.transform;
        }

        Vector3 lookAtGoal = new Vector3(goal.position.x, this.transform.position.y, goal.position.z);

        //Update Rotation
        Vector3 direction = lookAtGoal - this.transform.position;
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        // this.transform.LookAt(lookAtGoal

        // if (Vector3.Distance(transform.position, lookAtGoal) > accuracy)
        // {
        //     this.transform.Translate(0, 0, speed * Time.deltaTime);
        // }
    }
}
