using System;
using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;

public class TeamGroup : MonoBehaviour
{
    public TeamGroup(
        TeamRole _role,
        Territory _territory,
        bool _territoryManaged,
        Vector3 _goalPos
    )
    {
        role = _role;
        territoryOwner = _territory;
        bTerritoryManaged = _territoryManaged;
        currentGoalPosition = _goalPos;
    }

    public enum Status
    {
        active = 0,
        inactive = 1,
        dead = 2
    }

    [SerializeField]
    private Status currentStatus = Status.active;

    public Status GetStatus() => currentStatus;


#region Role
    public enum TeamRole
    {
        defend = 0,
        assault = 1
    }

    [SerializeField]
    private TeamRole role = TeamRole.assault;

    public void SetTeamRole(bool assault = false)
    {
        role = assault ? TeamRole.assault : TeamRole.defend;
        if (leader) SetTeamLeader(leader); //To update leader's role
    }

    public TeamRole GetTeamRole() => role;
#endregion



#region Events
    public event Action<TeamGroup> onTeamGroupDeath;

    public void TeamGroupDeath() => onTeamGroupDeath?.Invoke(this);
#endregion



#region Territory Controls
    [SerializeField]
    private Territory territoryOwner;

    public Territory GetTerritoryOwer() => territoryOwner;

    public void SetTerritoryOwner(Territory territory)
    {
        if (territory != null)
        {
            territoryOwner = territory;
            assignedColor = territory.GetAssignedColor();
        }
    }

    [SerializeField]
    private Territory currentTerritoryLocation;

    public Territory GetLocalTerritory() => currentTerritoryLocation;

    public void SetLocalTerritory(Territory localTerritory)
    {
        if (currentTerritoryLocation != localTerritory)
        {
            currentTerritoryLocation = localTerritory;

            if (currentTerritoryLocation != null)
            {
                currentTerritoryLocation.TeamGroupDetected(this);
            }
        }
    }

    private Vector4 assignedColor = Vector4.zero;

    [SerializeField]
    private bool bTerritoryManaged = true;

    public bool IsTerrioryManaged() => bTerritoryManaged;
#endregion



#region Team Goal Controls
    [SerializeField]
    private float holdPositionRadius = 12f;

    public float GetHoldPositionRadius() => holdPositionRadius;

    [SerializeField]
    private float goalDistanceMin = 3f;

    [SerializeField]
    private Vector3 currentGoalPosition;

    public Vector3 GetGoalPosition() => currentGoalPosition;

    public void SetGoalPosition(Vector3 newPos)
    {
        currentGoalPosition = newPos;
    }

    private void EvaluateTeamGoal()
    {
        if (!bTerritoryManaged) return;

        if (
            currentGoalPosition == Vector3.zero ||
            Vector3.Distance(currentGoalPosition, this.transform.position) <
            goalDistanceMin
        )
        {
            SetGoalPosition(territoryOwner.GetRandomAttackPoint());
        }
        else
        {
            return;
        }
    }
#endregion



#region Member Controls
    [SerializeField]
    private List<GameObject> members;

    [SerializeField]
    private GameObject leader;

    [SerializeField]
    private int memberCount = 0;

    public void AddMember(GameObject actor)
    {
        if (!members.Contains(actor))
        {
            members.Add (actor);
            ActorEventManger actorEventManger =
                actor.GetComponent<ActorEventManger>();
            if (actorEventManger != null)
            {
                actorEventManger.onActorDeath += OnMemberDeath;
            }

            memberCount++;

            EvaluateTeamFollower (actor);
        }
    }

    public void RemoveMember(GameObject actor)
    {
        if (members.Contains(actor))
        {
            members.Remove (actor);
            memberCount--;
        }
    }

    private void OnMemberDeath(GameObject member, GameObject killer)
    {
        RemoveMember (member);
        member.GetComponent<ActorEventManger>().onActorDeath -= OnMemberDeath;
        Debug.Log("TeamGroup: " + this.name + " - Member died: " + member.name);
        if (leader == member)
        {
            leader = null;
            EvaluateTeamLeader();
        }
    }


#region Leader Controls
    public GameObject GetTeamLeader() => leader;

    private void SetTeamLeader(GameObject actor)
    {
        leader = actor;

        TerritoryUnit territoryUnit = leader.GetComponent<TerritoryUnit>();
        if (territoryUnit != null)
        {
            territoryUnit.SetTeamGroup(this);
            territoryUnit.SetUnitRole(role == TeamRole.assault);
            territoryUnit.EvaluateHoldPositionData (leader);
        }
    }

    private void EvaluateTeamLeader()
    {
        memberCount = members.Count;
        if (memberCount >= 1)
        {
            SetTeamLeader(members[0]);

            Debug
                .Log("TeamGroup:" +
                this.name +
                " - New Leader: " +
                leader.name);

            UpdateTeamFollowers();
        }
    }
#endregion



#region Follower Controls
    private void EvaluateTeamFollower(GameObject member)
    {
        if (leader != null && member != leader && memberCount > 1)
        {
            Follower follower = member.GetComponent<Follower>();
            if (follower != null)
            {
                follower.SetTarget(leader.transform);
                follower.EvaluateFollowBehavior();
            }
        }
    }

    private void UpdateTeamFollowers()
    {
        if (leader != null && members.Count > 1)
        {
            foreach (GameObject member in members)
            {
                if (member != leader)
                {
                    Follower follower = member.GetComponent<Follower>();
                    if (follower == null)
                    {
                        Debug
                            .Log("TeamGroup: " +
                            this.name +
                            " - NO Follower Component Found: " +
                            member.name);
                        return;
                    }

                    follower.SetTarget(leader.transform);
                    follower.EvaluateFollowBehavior();

                    TerritoryUnit territoryUnit =
                        member.GetComponent<TerritoryUnit>();
                    if (territoryUnit != null)
                    {
                        territoryUnit.EvaluateHoldPositionData (leader);
                    }
                }
            }
        }
    }


#endregion


#endregion


    [SerializeField]
    private float morale = 1f;

    private void Awake()
    {
        members = new List<GameObject>();
    }

    private void OnDestroy()
    {
        if (members != null)
        {
            foreach (GameObject member in members)
            {
                if (member == null) return;

                ActorEventManger actorEventManger =
                    member.GetComponent<ActorEventManger>();
                if (actorEventManger != null)
                {
                    actorEventManger.onActorDeath -= OnMemberDeath;
                }
            }
        }
    }

    private float updateTime = 1f;

    private float timer;

    private float delayStart = 1f;

    private void FixedUpdate()
    {
        if (currentStatus == Status.dead) return;

        if (delayStart > 0f)
        {
            delayStart -= Time.fixedDeltaTime;
            return;
        }

        if (timer > 0f)
        {
            timer -= Time.fixedDeltaTime;
            return;
        }

        timer = updateTime;

        if (memberCount >= 1)
        {
            if (leader == null)
            {
                EvaluateTeamLeader();
            }
            else
            {
                this.transform.position = leader.transform.position;
            }

            EvaluateTeamGoal();
        }
        else
        {
            // SELF DESTRUCT
            if (members.Count < 1)
            {
                currentStatus = Status.dead;
                TeamGroupDeath();
            }
        }
    }


#region Debugging

    [SerializeField]
    private bool bDebug = true;

    private void OnDrawGizmos()
    {
        if (!bDebug) return;

        if (members.Count > 0)
        {
            foreach (GameObject member in members)
            {
                if (member == leader)
                {
                    Gizmos.color = assignedColor; //Color.mmgenta
                    Gizmos.DrawWireSphere(leader.transform.position, 0.4f);
                }

                Gizmos.color = assignedColor; //Color.yellow;
                Transform sourceTransform;

                if (leader != null && member != leader)
                {
                    sourceTransform = leader.transform;
                }
                else
                {
                    sourceTransform = this.transform;
                }

                Vector3 direction =
                    (member.transform.position - sourceTransform.position);
                Gizmos.DrawRay(sourceTransform.position, direction);
            }

            if (holdPositionRadius > 0)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(transform.position, holdPositionRadius);
            }
        }
    }
#endregion

}
