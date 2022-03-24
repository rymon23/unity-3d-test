using System.Collections;
using UnityEngine;

public class SmoothLookAtTarget : MonoBehaviour
{

    public Transform target;
    public float speed = 5f;
    
    private Coroutine LookAtCoroutine;
    public bool lookAtOn = false;
    [SerializeField] private bool isActive = false;


    private void Update() {
        if (lookAtOn && !isActive) {
            isActive = true;
            StartRoatating();
        }
    }

    public void StartRoatating() {
        if (LookAtCoroutine != null) {
            StopCoroutine(LookAtCoroutine);
        }
        LookAtCoroutine = StartCoroutine(LookAt());
    }

    private IEnumerator LookAt() {
        UnityEngine.Quaternion lookAtRotation = UnityEngine.Quaternion.LookRotation(target.position - transform.position);
        float time = 0;

        while (time < 1) {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAtRotation, time);

            time -= Time.deltaTime * speed;

            yield return null;
        }
    }

    // private void OnGUI() {
    //     if (GUI.Button(new Rect(10, 10, 100, 30), "Look At")){
    //         StartRoatating();
    //     }    
    // }
}
