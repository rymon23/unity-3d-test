using System.Collections;
using System.Collections.Generic;
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
    private int currentBorderWaypointIX = 0;

    [SerializeField]
    private Transform currentBorderWaypoint;

    public void SetSourceTerritory(Territory source)
    {
        if (source != null)
        {
            territoryOwner = source;
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
                WaypointData data =
                    territoryOwner.NextBorderWaypoint(currentBorderWaypointIX);
                currentBorderWaypoint = data.transform;
                currentBorderWaypointIX = data.index;
            }
            return currentBorderWaypoint;
        }

        return null;
    }
}
