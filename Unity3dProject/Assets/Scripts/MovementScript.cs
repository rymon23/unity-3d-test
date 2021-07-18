using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    Animator animator;
    Vector2 input;

    public float speed = 6f; 
    public float strafeSpeed = 4f; 

    private void Start() {
        animator = GetComponent<Animator>();
    }
    void Update()
    {  
        // input.x = Input.GetAxis("Horizontal");
        // input.y = Input.GetAxis("Vertical");

        // animator.SetFloat("Horizontal", input.x);
        // animator.SetFloat("Vertical", input.y);

        //vertiacl move
        if (Input.GetKey(KeyCode.W)) {
            transform.position += Vector3.forward * Time.deltaTime * speed;
        } else if (Input.GetKey(KeyCode.S)){
            transform.position += Vector3.back * Time.deltaTime * speed;
        }

        //horizontal move
       if (Input.GetKey(KeyCode.A)) {
            transform.position += Vector3.left * Time.deltaTime * speed;
        } else if (Input.GetKey(KeyCode.D)){
            transform.position += Vector3.right * Time.deltaTime * speed;
        }

    }
}

