using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIBehavior : MonoBehaviour
{

    public bool LookAtTarget(Transform looker, Transform target, float rotSpeed = 5f)
    {
        if (target == null || looker == null) return false;

        Vector3 direction = target.position - looker.position;

        looker.rotation = Quaternion.Slerp(looker.rotation, 
                                                Quaternion.LookRotation(direction), 
                                                Time.deltaTime*rotSpeed);

        // if( Task.isInspected )
        //         Task.current.debugInfo = string.Format("angle={0}", 
        //             Vector3.Angle(looker.forward,direction));
        
        // if(Vector3.Angle(looker.forward,direction) < 5.0f)
        // {
        //     Task.current.Succeed();
        // }
        return true;
    }

}
