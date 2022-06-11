using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField]
    private Transform owner;

    [SerializeField]
    private bool bDebug = true;

    private void OnDrawGizmos()
    {
        if (!bDebug) return;
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(this.transform.position, 1);
    }
}
