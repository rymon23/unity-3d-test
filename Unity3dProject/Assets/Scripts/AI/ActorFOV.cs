using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorFOV : MonoBehaviour {
    [Range(10, 180)] public float maxAngle = 70f;
    [Range(5, 100)] public float maxRadius = 40f;
    [Range(5, 20)] public float peripheralAngleOffset = 20f;
    [Range(5, 50)] public float peripheralRadius = 18f;
    public float overlapSphereRadius = 4f;    
    [SerializeField] public GameObject currentTarget;
    public Transform viewPoint;

    public bool bDebug;

    public void SetTarget(GameObject newTarget) {
        if (newTarget == currentTarget) return;

        currentTarget = newTarget;
    }

    private void OnDrawGizmos()
    {
        if (!bDebug) return;

        DrawViewingAngle();

        if (currentTarget) {
            Vector3 myPos = viewPoint != null ? viewPoint.position : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(myPos, (currentTarget.transform.position - myPos));
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(gameObject.transform.position, overlapSphereRadius);
    }

    private void DrawViewingAngle() {
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

