using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;

public class TerritoryUnit : MonoBehaviour
{
    enum UnitRole
    {
        defender = 0,
        attacker = 1
    }

    [SerializeField]
    private UnitRole role = UnitRole.defender;

    [SerializeField]
    private TeamGroup teamGroup;

    public TeamGroup GetTeamGroup() => teamGroup;

    public void SetTeamGroup(TeamGroup team)
    {
        teamGroup = team;
    }

    [SerializeField]
    private Territory territoryOwner;

    ActorEventManger actorEventManger;

    [SerializeField]
    private ActorNavigationData navigationData;

    [SerializeField]
    private CombatStateData combatStateData;

    [SerializeField]
    private int currentBorderWaypointIX = 0;

    [SerializeField]
    private Transform currentBorderWaypoint;

    private bool patrolCounterClockwise = false;

    private void UpdatePatrolPolicy()
    {
        patrolCounterClockwise = Random.Range(0, 100) < 50;
    }

    public void SetSourceTerritory(Territory source)
    {
        if (source != null)
        {
            territoryOwner = source;
            UpdatePatrolPolicy();
            EvaluateBorderWaypoint();
        }
    }

    public void SetUnitRole(bool attacker = false)
    {
        role = attacker ? UnitRole.attacker : UnitRole.defender;
    }

    public void EvaluateAttackPoint()
    {
    }

    public Transform EvaluateBorderWaypoint()
    {
        if (territoryOwner != null)
        {
            bool update = false;

            if (currentBorderWaypoint != null)
            {
                if (
                    Vector3
                        .Distance(this.transform.position,
                        currentBorderWaypoint.position) <
                    3.5f
                ) update = true;
            }
            else
                update = true;

            if (update)
            {
                Waypoint.WaypointData data =
                    territoryOwner
                        .NextBorderWaypoint(currentBorderWaypointIX,
                        patrolCounterClockwise);
                currentBorderWaypoint = data.transform;
                currentBorderWaypointIX = data.index;
            }
            return currentBorderWaypoint;
        }

        return null;
    }

    public void EvaluateHoldPositionData(GameObject leader = null)
    {
        if (
            teamGroup != null &&
            combatStateData != null &&
            navigationData != null
        )
        {
            if (teamGroup.IsTerrioryManaged())
            {
                if (role == UnitRole.defender)
                {
                    combatStateData.combatNavigationState =
                        CombatNavigationState.holdPosition;
                }
                else
                {
                    combatStateData.combatNavigationState =
                        CombatNavigationState.chase;
                }
            }

            if (leader == null || leader == this.gameObject)
            {
                navigationData
                    .UpdateHoldPositionData(territoryOwner
                        .GetHoldPositionCenter(),
                    territoryOwner.GetHoldPositionRadius());
            }
            else
            {
                navigationData
                    .UpdateHoldPositionData(teamGroup.transform,
                    teamGroup.GetHoldPositionRadius());
            }
        }
    }

    void onCombatStateChange(CombatState state)
    {
        if (territoryOwner != null)
        {
            if (state >= CombatState.active && role == UnitRole.defender)
            {
                territoryOwner
                    .InvasionStatusAlert(this.gameObject.transform.position);
            }
        }
    }

    private void Awake()
    {
        navigationData = GetComponent<ActorNavigationData>();
        combatStateData = GetComponent<CombatStateData>();
        actorEventManger = GetComponent<ActorEventManger>();

        if (actorEventManger != null)
            actorEventManger.onCombatStateChange += onCombatStateChange;
    }

    private void Start()
    {
        if (actorEventManger != null)
            actorEventManger.onCombatStateChange += onCombatStateChange;

        UpdatePatrolPolicy();
    }

    private void OnDestroy()
    {
        if (actorEventManger != null)
            actorEventManger.onCombatStateChange -= onCombatStateChange;
    }
}
