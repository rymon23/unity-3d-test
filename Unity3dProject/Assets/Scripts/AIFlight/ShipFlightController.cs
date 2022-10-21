using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipFlightController : MonoBehaviour
{
    public float

            forwardSpeed = 9f,
            strafeSpeed = 4.5f,
            hoverSpeed = 2.5f;

    [SerializeField]
    private float

            activeForwardSpeed,
            activeStrafeSpeed,
            activeHoverSpeed;

    [SerializeField]
    private float

            forwardAcceleration = 2.5f,
            strafeAcceleration = 3f,
            hoverAcceleration = 2f;

    public float lookRateSpeed = 60f;

    [SerializeField]
    private Vector2

            lookInput,
            screenCenter,
            mouseDistance;

    [SerializeField]
    private StarterAssetsInputs _input;

    void Start()
    {
        screenCenter.x = Screen.width * 0.5f;
        screenCenter.y = Screen.height * 0.5f;

        _input = GetComponent<StarterAssetsInputs>();
    }

    void Update()
    {
        lookInput.x = 0f; //Input.mousePosition.x;
        lookInput.y = 0f; //Input.mousePosition.y;

        mouseDistance.x = (lookInput.x - screenCenter.x) / screenCenter.y;
        mouseDistance.y = (lookInput.y - screenCenter.y) / screenCenter.y;

        mouseDistance = Vector2.ClampMagnitude(mouseDistance, 1f);

        // transform
        //     .Rotate(-mouseDistance.y * lookRateSpeed * Time.deltaTime,
        //     mouseDistance.x * lookRateSpeed * Time.deltaTime,
        //     0f,
        //     Space.Self);
        activeForwardSpeed =
            Mathf
                .Lerp(activeForwardSpeed,
                _input.move.y * forwardSpeed,
                // Input.GetAxisRaw("Vertical") * forwardSpeed,
                forwardAcceleration * Time.deltaTime);

        // activeStrafeSpeed =
        //     Mathf
        //         .Lerp(activeStrafeSpeed,
        //         _input.move.x * strafeSpeed,
        //         // Input.GetAxisRaw("Horizontal") * strafeSpeed,
        //         strafeAcceleration * Time.deltaTime);
        // activeForwardSpeed =
        //     Mathf
        //         .Lerp(activeHoverSpeed,
        //         Input.GetAxisRaw("Hover") * hoverSpeed,
        //         hoverAcceleration * Time.deltaTime);
        transform.position +=
            transform.forward * activeForwardSpeed * Time.deltaTime;
        transform.position +=
            (transform.right * activeStrafeSpeed * Time.deltaTime) +
            (transform.up * activeHoverSpeed * Time.deltaTime);
    }
}
