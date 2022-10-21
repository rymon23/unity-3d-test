using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;

public class AIFlightCombat : MonoBehaviour
{
    [SerializeField]
    private Transform castPoint;

    private Targeting targeting;

    GameObject combatTarget;

    CombatStateData targetCombatStateData;

    CombatStateData combatStateData;

    ActorHealth actorHealth;

    ActorFOV actorFOV;

    AnimationState animationState;

    [SerializeField]
    private MagicSpell magicSpell;

    [SerializeField]
    private float updateTime = 3f;

    [SerializeField]
    private float updateTimmer = 1f;

    [SerializeField]
    private Transform head;

    void Start()
    {
        targeting = this.GetComponent<Targeting>();
        combatStateData = this.GetComponent<CombatStateData>();
        animationState = this.GetComponent<AnimationState>();
        actorHealth = this.GetComponent<ActorHealth>();
        actorFOV = this.GetComponent<ActorFOV>();
    }

    private void FixedUpdate()
    {
        CheckRays();

        // Wait if start should be delayed
        if (updateTimmer > 0f)
        {
            updateTimmer -= Time.fixedDeltaTime;
            return;
        }

        updateTimmer = updateTime;

        if (castPoint != null && magicSpell != null)
        {
            FireSpell (magicSpell, castPoint);
        }
    }

    public float visibleDist = 24f;

    public void CheckRays()
    {
        Transform raycastPos;

        if (head != null)
            raycastPos = head;
        else
            raycastPos = castPoint;

        float
            fDist = visibleDist,
            rDist = visibleDist,
            lDist = visibleDist,
            r45Dist = visibleDist,
            l45Dist = visibleDist;

        float verticalAngle = 45f;

        Vector3 rightAngle =
            Quaternion.AngleAxis(verticalAngle, gameObject.transform.up) *
            gameObject.transform.forward *
            visibleDist;
        Vector3 leftAngle =
            Quaternion.AngleAxis(-verticalAngle, gameObject.transform.up) *
            gameObject.transform.forward *
            visibleDist;

        RaycastHit hit;
        if (
            Physics
                .Raycast(raycastPos.position,
                raycastPos.forward,
                out hit,
                visibleDist)
        )
        {
            fDist = hit.distance;
        }
        if (
            Physics
                .Raycast(raycastPos.position,
                raycastPos.right,
                out hit,
                visibleDist)
        )
        {
            rDist = hit.distance;
        }
        if (
            Physics
                .Raycast(raycastPos.position,
                -raycastPos.right,
                out hit,
                visibleDist)
        )
        {
            lDist = hit.distance;
        }
        if (
            Physics
                .Raycast(raycastPos.position, rightAngle, out hit, visibleDist)
        )
        {
            r45Dist = hit.distance;
        }
        if (
            Physics
                .Raycast(raycastPos.position, leftAngle, out hit, visibleDist)
        )
        {
            l45Dist = hit.distance;
        }

        // Debug rays
        Debug
            .DrawRay(raycastPos.position,
            raycastPos.forward * visibleDist,
            Color.red);
        Debug
            .DrawRay(raycastPos.position,
            raycastPos.right * visibleDist,
            Color.red);
        Debug
            .DrawRay(raycastPos.position,
            -raycastPos.right * visibleDist,
            Color.red);

        Debug.DrawRay(raycastPos.position, rightAngle, Color.red);
        Debug.DrawRay(raycastPos.position, leftAngle, Color.red);
    }

    public void FireSpell(MagicSpell spell, Transform spellCastPoint)
    {
        Debug.Log("FireSpell - " + magicSpell.name);

        if (magicSpell != null && spellCastPoint != null)
        {
            GameObject prefab = magicSpell.projectile.gameObject;

            float spread = 1f;
            Vector3 frontPos =
                spellCastPoint.position + (spellCastPoint.forward * 10);

            Vector3 aimDir =
                ((frontPos + (Vector3.up * 0.2f)) - spellCastPoint.position);

            // Calculate Spread
            float x = UnityEngine.Random.Range(-spread, spread);
            float y = UnityEngine.Random.Range(-spread, spread);

            Vector3 directionWithSpread = aimDir + new Vector3(x, y, 0);

            if (true)
                Debug
                    .DrawRay(spellCastPoint.position,
                    directionWithSpread,
                    Color.white);

            Transform currentBullet =
                Instantiate(prefab.transform,
                spellCastPoint.position,
                Quaternion.LookRotation(directionWithSpread, Vector3.up));

            Projectile projectile =
                currentBullet.gameObject.GetComponent<Projectile>();

            projectile.spellPrefab = magicSpell.spellPrefab;
            projectile.magicSpellPrefab = magicSpell;
            projectile.sender = spellCastPoint.gameObject;

            currentBullet.gameObject.transform.position =
                spellCastPoint.transform.position;
            currentBullet.transform.forward = directionWithSpread;
            currentBullet.gameObject.SetActive(true);
        }
    }
}
