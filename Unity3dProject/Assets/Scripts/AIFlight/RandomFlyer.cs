using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (Rigidbody))]
public class RandomFlyer : MonoBehaviour
{
    [SerializeField]
    float

            idleSpeed,
            turnSpeed,
            switchSeconds,
            idleRatio;

    [SerializeField]
    Vector2

            animSpeedMinMax,
            moveSpeedMinMax,
            changeAnimEveryFromTo;

    [SerializeField]
    Vector2 changeTargetEveryFromTo;

    [SerializeField]
    Transform

            homeTarget,
            flyingTarget;

    [SerializeField]
    Vector2 radiusMinMax;

    [SerializeField]
    Vector2 yMinMax;

    [SerializeField]
    public bool returnToBase = false;

    [SerializeField]
    public float

            randomBaseOffset = 5,
            delayStart = 0f;

    private Animator animator;

    private Rigidbody body;

    public float changeTarget = 0f;

    private float

            changeAnim = 0f,
            timeSinceTarget = 0f,
            timeSinceAnim = 0f,
            prevAnim,
            currentAnim = 0f,
            prevSpeed,
            speed,
            zturn,
            prevZ,
            turnSpeedBackup;

    private Vector3

            rotateTarget,
            position,
            direction,
            velocity,
            randomizedBase;

    private Quaternion lookRotation;

    public float

            distanceFromBase,
            distanceFromTarget;

    [SerializeField]
    private bool debug_gizmo = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody>();
        turnSpeedBackup = turnSpeed;
        direction = Quaternion.Euler(transform.eulerAngles) * (Vector3.forward);
        if (delayStart < 0f) body.velocity = idleSpeed * direction;
    }

    private void FixedUpdate()
    {
        // Wait if start should be delayed
        if (delayStart > 0f)
        {
            delayStart -= Time.fixedDeltaTime;
            return;
        }

        // Calculate distances
        distanceFromBase = Vector3.Magnitude(randomizedBase - body.position);
        distanceFromTarget =
            Vector3.Magnitude(flyingTarget.position - body.position);

        // Allow drastic turns close to base to ensure target can be reached
        if (returnToBase && distanceFromBase < 10f)
        {
            if (turnSpeed != 300f && body.velocity.magnitude != 0f)
            {
                turnSpeedBackup = turnSpeed;
                turnSpeed = 300f;
            }
            else if (distanceFromBase <= 1f)
            {
                body.velocity = Vector3.zero;
                turnSpeed = turnSpeedBackup;
                return;
            }
        }

        // Time for a new animation speed
        if (changeAnim < 0f)
        {
            prevAnim = currentAnim;
            currentAnim = ChangeAnim(currentAnim);
            changeAnim =
                Random.Range(changeAnimEveryFromTo.x, changeAnimEveryFromTo.y);
            timeSinceAnim = 0f;
            prevSpeed = speed;
            if (currentAnim == 0)
                speed = idleSpeed;
            else
                speed =
                    Mathf
                        .Lerp(moveSpeedMinMax.x,
                        moveSpeedMinMax.y,
                        (currentAnim - animSpeedMinMax.x) /
                        (animSpeedMinMax.y - animSpeedMinMax.x));
        }

        // Time for a new target position
        if (changeTarget < 0f)
        {
            rotateTarget = ChangeDirection(body.transform.position);
            if (returnToBase)
                changeTarget = 0.2f;
            else
                changeTarget =
                    Random
                        .Range(changeTargetEveryFromTo.x,
                        changeTargetEveryFromTo.y);
            timeSinceTarget = 0f;
        }

        // Turn when approaching height limits
        if (
            body.transform.position.y < yMinMax.x + 10f ||
            body.transform.position.y > yMinMax.x - 10f
        )
        {
            if (body.transform.position.y < yMinMax.x + 10f)
                rotateTarget.y = 1f;
            else
                rotateTarget.y = -1f;
        }

        // Tilt
        zturn =
            Mathf
                .Clamp(Vector3.SignedAngle(rotateTarget, direction, Vector3.up),
                -45f,
                45f);

        // Update Times
        changeAnim -= Time.fixedDeltaTime;
        changeTarget -= Time.fixedDeltaTime;
        timeSinceTarget += Time.fixedDeltaTime;
        timeSinceAnim += Time.fixedDeltaTime;

        // Rotate towards target
        if (rotateTarget != Vector3.zero)
            lookRotation = Quaternion.LookRotation(rotateTarget, Vector3.up);
        Vector3 rotation =
            Quaternion
                .RotateTowards(body.transform.rotation,
                lookRotation,
                turnSpeed * Time.fixedDeltaTime)
                .eulerAngles;
        body.transform.eulerAngles = rotation;

        // Rotate on z-axis to tilt body towards turn direction
        float temp = prevZ;
        if (prevZ < zturn)
            prevZ += Mathf.Min(turnSpeed * Time.fixedDeltaTime, zturn - prevZ);
        else if (prevZ >= zturn)
            prevZ -= Mathf.Min(turnSpeed * Time.fixedDeltaTime, prevZ - zturn);

        // Min and Max rotation on z-axis - can also be parameterized
        prevZ = Mathf.Clamp(prevZ, -45f, 45f);

        // Remove temp it transform is rotated back earlier in FixedUpdate
        body.transform.Rotate(0f, 0f, prevZ - temp, Space.Self);

        // Move flyer
        if (returnToBase && distanceFromBase < idleSpeed)
        {
            body.velocity = Mathf.Min(idleSpeed, distanceFromBase) * direction;
        }
        else
            direction =
                Quaternion.Euler(transform.eulerAngles) * Vector3.forward;
        body.velocity =
            Mathf
                .Lerp(prevSpeed,
                speed,
                Mathf.Clamp(timeSinceAnim / switchSeconds, 0f, 1f)) *
            direction;

        // Hard-limt the height, in case the limit is breached despite of turnaround attempt
        if (
            body.transform.position.y < yMinMax.x ||
            body.transform.position.y > yMinMax.y
        )
        {
            position = body.transform.position;
            position.y = Mathf.Clamp(position.y, yMinMax.x, yMinMax.y);
            body.transform.position = position;
        }
    }

    private float ChangeAnim(float currentAnim)
    {
        float newState;
        if (Random.Range(0f, 1f) < idleRatio)
            newState = 0f;
        else
        {
            newState = Random.Range(animSpeedMinMax.x, animSpeedMinMax.y);
        }
        if (newState != currentAnim)
        {
            animator.SetFloat("flySpeed", newState);
            if (newState == 0)
                animator.speed = 1f;
            else
                animator.speed = newState;
        }
        return newState;
    }

    private Vector3 ChangeDirection(Vector3 currentPosition)
    {
        Vector3 newDir;
        if (returnToBase)
        {
            randomizedBase = homeTarget.position;
            randomizedBase.y +=
                Random.Range(-randomBaseOffset, randomBaseOffset);
            newDir = randomizedBase - currentPosition;
        }
        else if (distanceFromTarget > radiusMinMax.y)
        {
            newDir = flyingTarget.position - currentPosition;
        }
        else if (distanceFromTarget > radiusMinMax.x)
        {
            newDir = currentPosition - flyingTarget.position;
        }
        else
        {
            // 360-degree freedom of choice on horizontal plane
            float angleXZ = Random.Range(-Mathf.PI, Mathf.PI);

            // Limited max steepness of ascent/descent in vertical direction
            float angleY = Random.Range(-Mathf.PI / 48f, Mathf.PI / 48f);

            // Calculate direction
            newDir =
                Mathf.Sin(angleXZ) * Vector3.forward +
                Mathf.Cos(angleXZ) * Vector3.right +
                Mathf.Sin(angleY) * Vector3.up;
        }

        return newDir.normalized;
    }

    private void OnDrawGizmos()
    {
        if (!debug_gizmo) return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(homeTarget.position, radiusMinMax.y);
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(flyingTarget.position, radiusMinMax.y);
    }
}
