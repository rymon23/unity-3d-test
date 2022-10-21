using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;

public enum WeaponType
{
    unarmed = 0,
    sword = 1,
    gun,
    bow
}

public class Weapon : MonoBehaviour
{
    public string _ownerRefId = "-";

    public WeaponType weaponType;

    public EquipSlotType equipSlotType;

    public ItemType itemType = 0;

    public Item item;

    public float

            timeBetweenShooting,
            spread,
            reloadTime,
            timeBetweenShots;

    public int

            magazineSize,
            bulletsPerTap;

    public bool allowInvoke;

    public bool fullAuto;

    int

            bulletsLeft,
            bulletsShot;

    bool

            readyToShoot,
            shooting,
            reloading;

    [SerializeField]
    public Transform firePoint;

    [SerializeField]
    private Transform ammunition;

    [SerializeField]
    private WeaponCollider weaponCollider;

    [SerializeField]
    private BoxCollider boxCollider;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    float attackRange = 1.1f;

    public float damage = 0;

    [SerializeField]
    private ActionNoiseLevel noiseLevel = ActionNoiseLevel.low;

    public bool canParry = true;

    public bool bShouldFire = false;

    // public HashSet<string> attackblockedList;
    private float updateTime = 0.1f;

    private float timer;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        timer = updateTime;
    }

    void Awake()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;

        weaponCollider =
            this.gameObject.GetComponentInChildren<WeaponCollider>();
        if (weaponCollider != null)
        {
            weaponCollider.weapon = this;
            boxCollider = this.gameObject.GetComponentInChildren<BoxCollider>();
            DisableWeaponCollider();
        }
    }

    private void FixedUpdate()
    {
        if (weaponType == WeaponType.gun)
        {
            timer -= Time.deltaTime;

            if (timer < 0)
            {
                timer = updateTime;

                if (bShouldFire)
                {
                    if (bShootWithRaycasts)
                    {
                        RaycastShoot();
                    }
                    else
                    {
                        Shoot();
                    }

                    float noiseRange = (int) noiseLevel * 30f;
                    GlobalEventManager
                        .LocationalActionBroadcast(this.transform.position,
                        noiseRange);
                    bShouldFire = false;
                }
            }
        }
    }

    public void FireBullet()
    {
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = 0;

            readyToShoot = false;

            if (bShootWithRaycasts)
            {
                RaycastShoot();
                return;
            }
            Shoot();
        }
    }


#region Shooting With Raycast
    [SerializeField]
    private Projectile projectile;

    public Projectile GetDefaultProjectile() => projectile;

    [SerializeField]
    private bool bShootWithRaycasts = true;

    [SerializeField]
    private Vector3 bulletSpeedVariance = new Vector3(0.1f, 0.1f, 0.1f);

    [SerializeField]
    private ParticleSystem shootingSystem;

    [SerializeField]
    private ParticleSystem impactParticleSystem;

    // [SerializeField]
    // private Transform bulletSpawnPoint;
    [SerializeField]
    private TrailRenderer bulletTrail;

    [SerializeField]
    private float shootDelay = 0.3f;

    [SerializeField]
    private LayerMask mask;

    [SerializeField]
    private float lastShootTime = 0f;

    public void RaycastShoot()
    {
        if (lastShootTime + shootDelay < Time.time)
        {
            // TODO: use object pooing
            // animate here
            // play sound
            shootingSystem.Play();
            Vector3 direction = transform.forward;

            float x = Random.Range(-spread, spread);
            float y = Random.Range(-spread, spread);

            Vector3 directionWithSpread = direction + new Vector3(x, y, 0);

            if (
                Physics
                    .Raycast(firePoint.position,
                    direction,
                    out RaycastHit hit,
                    float.MaxValue,
                    mask)
            )
            {
                // Debug.DrawRay(firePoint.position, direction, Color.magenta);
                if (bulletTrail != null)
                {
                    Debug.Log("bullet trail");

                    TrailRenderer trail =
                        Instantiate(bulletTrail,
                        firePoint.position,
                        Quaternion.identity);

                    StartCoroutine(SpawnTrail(trail,
                    directionWithSpread,
                    firePoint.position,
                    hit));
                }

                lastShootTime = Time.time;
            }
        }
    }

    private IEnumerator
    SpawnTrail(
        TrailRenderer trail,
        Vector3 fireDir,
        Vector3 firePos,
        RaycastHit hit
    )
    {
        float time = 0f;
        Vector3 startPos = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPos, hit.point, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }

        // animator.setbool isshooting false
        trail.transform.position = hit.point;

        int layerMask = LayerMask.GetMask("Hitbox");

        if (
            Physics
                .Raycast(firePos,
                fireDir,
                out RaycastHit impactHit,
                float.MaxValue,
                layerMask)
        )
        {
            Instantiate(impactParticleSystem,
            hit.point,
            Quaternion.LookRotation(hit.normal));

            Collider col = impactHit.collider;
            Hitbox hitbox = col.gameObject.GetComponent<Hitbox>();
            if (hitbox != null)
            {
                hitbox.onBulletHit(this);
            }
        }

        Destroy(trail.gameObject, trail.time);
    }
#endregion


    public void Shoot()
    {
        if (ammunition != null && firePoint != null)
        {
            Vector3 frontPos = firePoint.position + (firePoint.forward * 10);
            Vector3 aimDir = (frontPos - firePoint.position).normalized;

            // Calculate Spread
            float x = Random.Range(-spread, spread);
            float y = Random.Range(-spread, spread);

            Vector3 directionWithSpread = aimDir + new Vector3(x, y, 0);

            if (bDebug)
                Debug
                    .DrawRay(firePoint.position,
                    directionWithSpread,
                    Color.red);

            if (audioSource != null)
            {
                audioSource.Play();
            }

            if (!PreFireRayCheckIsBlocking(directionWithSpread))
            {
                // Transform spell = Instantiate(ammunition, firePoint.position, Quaternion.LookRotation(aimDir, Vector3.up));
                Transform currentBullet =
                    Instantiate(ammunition,
                    firePoint.position,
                    Quaternion.LookRotation(directionWithSpread, Vector3.up));
                Projectile projectile =
                    currentBullet.gameObject.GetComponent<Projectile>();
                projectile.damage += damage;
                projectile.weapon = this;
                projectile.sender = this.gameObject;
                currentBullet.gameObject.transform.position =
                    firePoint.transform.position;
                currentBullet.transform.forward = directionWithSpread;
                currentBullet.gameObject.SetActive(true);
            }
        }

        bulletsLeft--;
        bulletsShot++;

        if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShooting);
            allowInvoke = false;
        }

        if (bulletsShot > bulletsPerTap && bulletsLeft > 0)
        {
            Invoke("Shoot", timeBetweenShots);
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowInvoke = true;
    }

    private void Reload()
    {
        reloading = true;
        Invoke("ReloadFinished", reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    public void EnableWeaponCollider()
    {
        boxCollider.enabled = true;
        weaponCollider.gameObject.SetActive(true);
    }

    public void DisableWeaponCollider()
    {
        boxCollider.enabled = false;
        weaponCollider.gameObject.SetActive(false);
    }

    public bool PreFireRayCheckIsBlocking(Vector3 direction)
    {
        int layerMask = LayerMask.GetMask("Hurtbox");

        RaycastHit[] raycastHits =
            Physics
                .RaycastAll(firePoint.position,
                direction,
                Mathf.Infinity,
                layerMask);
        if (raycastHits.Length > 0)
        {
            Collider col = raycastHits[0].collider;
            Debug
                .Log("PreFireRayCheck => First Hit : " +
                col.name +
                " Layer: " +
                LayerMask.LayerToName(col.gameObject.layer));

            // Debug.DrawRay(firePoint.position, col.gameObject.transform.position, Color.yellow);
            ParryHitBox parryHitBox =
                col.gameObject.GetComponent<ParryHitBox>();
            if (parryHitBox != null)
            {
                Debug.Log("PreFireRayCheck => Hit Blocking Hitbox");
                parryHitBox.onHitBlocked(null);
                return true;
            }
        }
        return false;
    }

    public bool bDebug = false;

    private void OnDrawGizmos()
    {
        if (!bDebug) return;

        if (weaponType == WeaponType.gun)
        {
            Gizmos.color = Color.green;
            Vector3 frontPos = firePoint.position + (firePoint.forward * 10);

            // Vector3 aimDir = (frontPos - firePoint.position)
            Gizmos.DrawRay(firePoint.position, (frontPos - firePoint.position));

            // Gizmos.DrawRay(frontPos, aimDir);
            // Gizmos.DrawRay(frontPos, (currentTarget.transform.position - myPos));
        }
    }
}
