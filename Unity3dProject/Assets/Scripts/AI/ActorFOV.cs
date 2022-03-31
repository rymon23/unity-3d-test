using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorFOV : MonoBehaviour
{
    [SerializeField] bool hasTargetLOS = false;
    // [SerializeField] private float updateTimeLOS = 1f;
    // public float timerUpdateLOS = 0f;
    // public float _timerUpdateLOS
    // {
    //     get => _timerUpdateLOS;
    //     set => _timerUpdateLOS = value;
    // }

    [Range(10, 180)] public float maxAngle = 70f;
    [Range(5, 100)] public float maxRadius = 40f;
    [Range(5, 20)] public float peripheralAngleOffset = 20f;
    [Range(5, 50)] public float peripheralRadius = 18f;
    public float overlapSphereRadius = 4f;
    [SerializeField] public GameObject currentTarget;
    public Transform viewPoint;

    public bool bDebugFOV;
    public bool bDebugLOS;

    public void SetTarget(GameObject newTarget)
    {
        if (newTarget == currentTarget) return;

        currentTarget = newTarget;
    }

    private void OnDrawGizmos()
    {
        if (bDebugFOV) DrawViewingAngle();

        if (!bDebugLOS) return;

        if (currentTarget != null)
        {
            Transform myPos = viewPoint != null ? viewPoint.transform : transform;
            if (UtilityHelpers.HasFOVAngle(myPos, currentTarget.transform.position, maxAngle, maxRadius))
            // if (UtilityHelpers.IsInFOVScope(this.transform, currentTarget.transform.position, maxAngle, maxRadius))
            {
                // Vector3 targetCenterPoint = currentTarget.transform.position + (currentTarget.transform.up * 2f);
                Vector3 targetPos = currentTarget.transform.position + (currentTarget.transform.up * 1.2f);

                if (UtilityHelpers.IsInFOVScope(myPos, targetPos, maxAngle, maxRadius))

                    Gizmos.color = Color.white;
                else
                {
                    Gizmos.color = Color.red;
                }
                Gizmos.DrawRay(myPos.position, (targetPos - myPos.position));
            }
        }
        // Gizmos.color = Color.white;
        // Gizmos.DrawWireSphere(gameObject.transform.position, overlapSphereRadius);
    }

    private void DrawViewingAngle()
    {
        float fovAngle = maxAngle;

        Vector3 fovLine1 = Quaternion.AngleAxis(fovAngle, gameObject.transform.up) * gameObject.transform.forward * peripheralRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fovAngle, gameObject.transform.up) * gameObject.transform.forward * peripheralRadius;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(gameObject.transform.position, fovLine1);
        Gizmos.DrawRay(gameObject.transform.position, fovLine2);

        fovAngle = maxAngle - peripheralAngleOffset;
        Vector3 peripheralLine1 = Quaternion.AngleAxis(fovAngle, gameObject.transform.up) * gameObject.transform.forward * maxRadius;
        Vector3 peripheralLine2 = Quaternion.AngleAxis(-fovAngle, gameObject.transform.up) * gameObject.transform.forward * maxRadius;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(gameObject.transform.position, peripheralLine1);
        Gizmos.DrawRay(gameObject.transform.position, peripheralLine2);
    }
}

