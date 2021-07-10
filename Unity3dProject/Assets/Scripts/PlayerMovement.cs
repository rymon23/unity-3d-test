using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //Vars
    [SerializeField] private float moveSpeed;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    
    private Vector3 moveDirection;
    private Vector3 velocity; //keep track of gravity & jumping

    [SerializeField] private bool isGrounded;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float gravity;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float fallMultiplier = 2.75f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private bool jumpRequest;

    //Refs
    private CharacterController controller;

    private void Start() {
        controller = GetComponent<CharacterController>();
    }

    private void Update() {
        Move();   
    }

    private void FixedUpdate() {
        if (jumpRequest) {
            Jump();
            jumpRequest = false;
        }
        if (velocity.y < 0) {
            ApplyFallForce();
        }
    }

    private void Move() {
        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);

        if (isGrounded && velocity.y < 0 ) {
            velocity.y = -2f;
        }

        float moveZ = Input.GetAxis("Vertical");

        moveDirection = new Vector3(0, 0, moveZ);
        moveDirection *= walkSpeed;

        if (isGrounded) {
            if (moveDirection != Vector3.zero && !Input.GetKey(KeyCode.LeftShift)) {
                Walk();
            }
            else if (moveDirection != Vector3.zero && Input.GetKey(KeyCode.LeftShift)) {
                Run();

            } else if (moveDirection == Vector3.zero) {
                Idle();
            }
            moveDirection *= moveSpeed; //Calculate move speed

            if (Input.GetKeyDown(KeyCode.Space)){
                jumpRequest = true;
            }
        }

        controller.Move(moveDirection * Time.deltaTime); //Apply move speed


        velocity.y += gravity * Time.deltaTime; //Calculate gravity
        controller.Move(velocity * Time.deltaTime); //Apply gravity
    }

    private void Idle() {

    }
    private void Walk() {
        moveSpeed = walkSpeed;
    }
    private void Run() {
        moveSpeed = runSpeed;
    }
    private void Jump() {
        velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
    }

    private void ApplyFallForce() {
        velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;    
    }

}
