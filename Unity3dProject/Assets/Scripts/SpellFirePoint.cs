using UnityEngine;

namespace Hybrid.Components
{
    public class SpellFirePoint : MonoBehaviour
    {
        public bool bDebug = true;

        [SerializeField]
        private Transform firePoint;

        [SerializeField]
        private Transform targeter;

        public void Fire(GameObject prefab)
        {
            if (prefab != null)
            {
                // Vector3 aimDir = (targeter.position - firePoint.position).normalized;
                // Vector3 aimDir = (targeter.position - firePoint.position);
                Vector3 aimDir =
                    (
                    (targeter.position + (Vector3.up * 0.2f)) -
                    firePoint.position
                    );

                if (bDebug)
                    Debug.DrawRay(firePoint.position, aimDir, Color.red);

                Transform currentBullet =
                    Instantiate(prefab.transform,
                    firePoint.position,
                    Quaternion.LookRotation(aimDir, Vector3.up));
                Projectile projectile =
                    currentBullet.gameObject.GetComponent<Projectile>();
                projectile.sender = this.transform.root.gameObject;
                currentBullet.gameObject.transform.position =
                    firePoint.transform.position;
                currentBullet.transform.forward = aimDir;
            }
        }
    }
}
