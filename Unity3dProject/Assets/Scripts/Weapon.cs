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
#region Owner
    public string _ownerRefId = "-";

    public GameObject owner;
#endregion



#region Melee Settings
    public bool useMeleeRaycasts = false;

    public bool raycastMeleeActive = false;

    [SerializeField]
    private float meleeRaycastLengthMult = 0.8f;
#endregion



#region Item Settingd
    [Header("Item Settings")]
    public WeaponType weaponType;

    public EquipSlotType equipSlotType;

    public ItemType itemType = 0;

    public Item item;


#endregion


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
            // }
            // else
            // {
        }
    }

    private void Update()
    {
        if (weaponType == WeaponType.sword && raycastMeleeActive)
        {
            MeleeRayHitCheck();
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
    private bool bShootWithRaycasts = true;

    [SerializeField]
    private Vector3 bulletSpeedVariance = new Vector3(0.1f, 0.1f, 0.1f);

    [SerializeField]
    private float shootDelay = 0.3f;

    private float lastShootTime = 0f;

    [SerializeField]
    private Projectile projectile;

    public Projectile GetDefaultProjectile() => projectile;

    [SerializeField]
    private LayerMask mask;

    [Header("Shooting FX")]
    [SerializeField]
    private ParticleSystem shootingSystem;

    [SerializeField]
    private ParticleSystem impactParticleSystem;

    [SerializeField]
    private TrailRenderer bulletTrail;

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
        weaponCollider?.gameObject.SetActive(true);
    }

    public void DisableWeaponCollider()
    {
        boxCollider.enabled = false;
        weaponCollider?.gameObject.SetActive(false);
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
                parryHitBox.onHitBlocked((Projectile) null);
                return true;
            }
        }
        return false;
    }

    public void MeleeRayHitCheck()
    {
        int layerMask = LayerMask.GetMask("Hitbox");

        Vector3 frontPos =
            firePoint.position + (firePoint.forward * meleeRaycastLengthMult);

        RaycastHit[] raycastHits =
            Physics
                .RaycastAll(firePoint.position,
                (frontPos - firePoint.position),
                meleeRaycastLengthMult,
                layerMask);

        Debug
            .DrawRay(firePoint.position,
            (frontPos - firePoint.position),
            Color.magenta);

        if (raycastHits.Length > 0)
        {
            Collider col = raycastHits[0].collider;

            ParryHitBox parryHitBox =
                col.gameObject.GetComponent<ParryHitBox>();
            if (
                parryHitBox != null &&
                parryHitBox.GetActor().refId != _ownerRefId
            )
            {
                Debug.Log("melee raycast hit on ParryHitbox");
                parryHitBox.onHitBlocked(this);
                raycastMeleeActive = false;
                // return;
            }
            else
            {
                Hitbox hitbox = col.gameObject.GetComponent<Hitbox>();
                if (hitbox != null)
                {
                    Debug.Log("melee raycast hit on Hitbox");
                    hitbox.onWeaponHit(this);
                    raycastMeleeActive = false;
                }
            }
        }
    }


#region Debugging
    [Header("Debug")]
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

        if (weaponType == WeaponType.sword)
        {
            Gizmos.color = Color.red;
            Vector3 frontPos =
                firePoint.position +
                (firePoint.forward * meleeRaycastLengthMult);
            Gizmos.DrawRay(firePoint.position, (frontPos - firePoint.position));
        }
    }
#endregion

}
