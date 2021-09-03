using System.Net.Mime;
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
    // [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private bool jumpRequest;
    [SerializeField] private bool slideRequest;

    private enum GroundedAnimationState
    {
        idle,
        walking,
        running,
        crouching,
        sliding,
    }

    [SerializeField] private GroundedAnimationState groundedState;


    //Refs
    private CharacterController controller;
    private Animator anim;
    private AnimationController animationController;

    private void Start() {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        animationController = GetComponentInChildren<AnimationController>();
    }

    private void Update() {
        Move();   

        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            Attack();
            // StartCoroutine(Attack());
        }
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            Block();
        }
    }

    private void FixedUpdate() {
        if (jumpRequest) {
            Jump();
            jumpRequest = false;
        }
        if (velocity.y < 0) {
            ApplyFallForce();
        }
        if (slideRequest) {
            Slide();
            slideRequest = false;
        }
    }

    private void Move() {
        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);
        anim.SetBool("Grounded", isGrounded);

        if (isGrounded && velocity.y < 0 ) {
            velocity.y = -2f;
        }

        float moveZ = Input.GetAxis("Vertical");
        float moveX = Input.GetAxis("Horizontal");

        moveDirection = new Vector3(moveX, 0, moveZ);
        moveDirection = transform.TransformDirection(moveDirection);

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


            if (Input.GetKey(KeyCode.LeftControl) && groundedState == GroundedAnimationState.running){
                slideRequest = true;
            }
        }

        controller.Move(moveDirection * Time.deltaTime); //Apply move speed


        velocity.y += gravity * Time.deltaTime; //Calculate gravity
        controller.Move(velocity * Time.deltaTime); //Apply gravity
    }

    private void Idle() {
        groundedState = GroundedAnimationState.idle;

        anim.SetFloat("YAxis", 0, 0.1f, Time.deltaTime);
    }
    private void Walk() {
        moveSpeed = walkSpeed;
        groundedState = GroundedAnimationState.walking;

        anim.SetFloat("YAxis", 0.5f, 0.1f, Time.deltaTime);
    }
    private void Run() {
        moveSpeed = runSpeed;
        groundedState = GroundedAnimationState.running;

        anim.SetFloat("YAxis", 1, 0.1f, Time.deltaTime);
    } 
    private void Jump() {
        velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
        anim.SetTrigger("Jump");
        // animationController.ChangeAnimationState("Jump");
    }
    private void Slide() {
        groundedState = GroundedAnimationState.sliding;
    }

    private void ApplyFallForce() {
        velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;    
    }
    private void Attack() {
        anim.SetTrigger("Attack");
    }
    private void Block() {
        anim.SetTrigger("Block");
    }
    // private IEnumerator Attack() {
    //     anim.SetLayerWeight(anim.GetLayerIndex("Attack Layer"), 1);
    //     anim.SetTrigger("Attack");

    //     yield return new WaitForSeconds(0.9f);
    //     anim.SetLayerWeight(anim.GetLayerIndex("Attack Layer"), 0);
    // }

}
