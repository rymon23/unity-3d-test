using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;



public enum RumblePattern
{
    Constant = 0,
    Pulse,
    Linear
}

public enum ActionRumbleType
{
    meleeHit,
    shoot,
    cast
}
public enum HitRumbleType
{
    light_melee,
    light_projectile,
    other,
}

public class GamepadRumblerController : MonoBehaviour
{
    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private ActorEventManger actorEventManger;

    private float rumbleDuration;
    private float pulseDuration;
    private float lowA;
    private float hightA;
    private float rumbleStep;
    private bool isMotorActive = false;
    private RumblePattern activeRumblePatten;

    public void StopRumble()
    {
        var gamepad = GetGamepad();
        if (gamepad != null) gamepad.SetMotorSpeeds(0, 0);
    }

    private Gamepad GetGamepad()
    {
        return Gamepad.current;
    }

    public void RumbleConstant(float low, float high, float duration)
    {
        activeRumblePatten = RumblePattern.Constant;
        lowA = low;
        hightA = high;
        rumbleDuration = Time.time + duration;
        Invoke(nameof(StopRumble), duration);
    }
    public void RumblePulse(float low, float high, float burstTime, float duration)
    {
        activeRumblePatten = RumblePattern.Pulse;
        lowA = low;
        hightA = high;
        rumbleStep = burstTime;
        pulseDuration = Time.time + burstTime;
        rumbleDuration = Time.time + duration;
        isMotorActive = true;
        var gamepad = GetGamepad();
        gamepad?.SetMotorSpeeds(lowA, hightA);
        Invoke(nameof(StopRumble), duration);
    }
    public void RumbleLinear(float lowStart, float lowEnd, float highStart, float highEnd, float duration) { }



    private void Awake()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        actorEventManger = GetComponent<ActorEventManger>();

        if (actorEventManger != null)
        {
            actorEventManger.OnRumbleFire += DefaultRumble;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        StopRumble();
    }
    private void OnApplicationQuit()
    {
        StopRumble();
    }
    // private void OnApplicationPause(bool pauseStatus){    }

    private void Update()
    {
        if (Time.time > rumbleDuration)
            return;

        var gamepad = GetGamepad();
        if (gamepad == null)
            return;

        switch (activeRumblePatten)
        {
            case RumblePattern.Constant:
                gamepad.SetMotorSpeeds(lowA, hightA);
                break;
            case RumblePattern.Pulse:
                if (Time.time > pulseDuration)
                {
                    isMotorActive = !isMotorActive;
                    pulseDuration = Time.time + rumbleStep;
                    if (!isMotorActive)
                    {
                        gamepad.SetMotorSpeeds(0, 0);
                    }
                    else
                    {
                        gamepad.SetMotorSpeeds(lowA, hightA);
                    }
                }
                break;
            case RumblePattern.Linear:
                break;
            default:
                break;
        }
    }

    private void DefaultRumble()
    {
        RumbleConstant(0.8f, 1f, 0.4f);
    }
}
