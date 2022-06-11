using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamGroup : MonoBehaviour
{
    // [SerializeField] private List<Faction> factions;
    [SerializeField] private List<GameObject> members;
    [SerializeField] private GameObject leader;
    [SerializeField] private int memberCount = 0;

    public void AddMember(GameObject actor)
    {
        if (!members.Contains(actor))
        {
            members.Add(actor);
            ActorEventManger actorEventManger = actor.GetComponent<ActorEventManger>();
            if (actorEventManger != null)
            {
                actorEventManger.onActorDeath += OnMemberDeath;
            }
            memberCount++;
        }
    }

    public void RemoveMember(GameObject actor)
    {
        if (members.Contains(actor))
        {
            members.Remove(actor);
            memberCount--;
        }
    }

    public GameObject GetTeamLeader(GameObject actor) => leader;
    private void SetTeamLeader(GameObject actor)
    {
        leader = actor;
    }
    private void EvaluateTeamLeader()
    {
        memberCount = members.Count;
        if (memberCount > 2)
        {
            // leader = members[Random.Range(0, memberCount - 1) % memberCount];
            // }
            // else
            // {
            leader = members[0];
        }
    }

    private void OnMemberDeath(GameObject member)
    {
        RemoveMember(member);
        member.GetComponent<ActorEventManger>().onActorDeath -= OnMemberDeath;
        Debug.Log("TeamGroup: " + this.name + " - Member died: " + member.name);
        if (leader == member)
        {
            leader = null;
            EvaluateTeamLeader();
        }
    }

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

                ActorEventManger actorEventManger = member.GetComponent<ActorEventManger>();
                if (actorEventManger != null)
                {
                    actorEventManger.onActorDeath -= OnMemberDeath;
                }
            }
        }
    }

    private float updateTime = 1f;
    private float timer;
    private void FixedUpdate()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            timer = updateTime;

            if (memberCount > 1 && leader == null)
            {
                EvaluateTeamLeader();
            }
            else
            {
                // SELF DESTRUCT
            }
        }
    }

    [SerializeField] private bool bDebug = true;
    private void OnDrawGizmos()
    {
        if (!bDebug) return;

        if (members.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (GameObject member in members)
            {
                Vector3 direction = (member.transform.position - this.transform.position);
                Gizmos.DrawRay(this.transform.position, direction);
            }
        }



    }
}
