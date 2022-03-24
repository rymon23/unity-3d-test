using UnityEngine;

public class LookAtBasic : MonoBehaviour
{
    public Transform target;

    // Update is called once per frame
    void Update()
    {
        if (target != null) {
            transform.LookAt(target);    
        }
    }
}
