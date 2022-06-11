using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class TeamGroupManager : MonoBehaviour
{
    public static TeamGroupManager current;

    public static bool bDebug = true;


    // public static Dictionary<Faction, List<GameObject>> requestedFactionGroups;
    public static Dictionary<Faction, Hashtable> requestedFactionGroups;
    public static Dictionary<Faction, Transform> teamGroup;
    [SerializeField] private static Transform teamGroupPrefab;

    [SerializeField] private static float pingTimer = 0;
    public static float pingTime = 1.25f;
    public event Action OnTeamGroupPing;
    public void TeamGroupPing() => OnTeamGroupPing?.Invoke();
    public event Action<GameObject, Faction> OnTeamGroupPingResponse;
    public void TeamGroupPingResponse(GameObject actor, Faction faction) => OnTeamGroupPingResponse?.Invoke(actor, faction);


    // public static void RequestTeamGroupAssignment(GameObject actor, Faction faction)
    // {

    //     // if (requestedFactionGroups.ContainsKey(faction))
    //     // {
    //     //     requestedFactionGroups[faction].Add(actor.GetInstanceID(), actor);
    //     // } else {
    //     //     requestedFactionGroups.Add(Faction) [faction].Add(actor.GetInstanceID(), actor);
    //     // }
    // }
    public static void AssignToTeamGroup(GameObject actor, Faction faction)
    {

        if (teamGroup != null && !teamGroup.ContainsKey(faction))
        {
            Transform newGroup = Instantiate(teamGroupPrefab, actor.transform.position, Quaternion.identity);
            teamGroup.Add(faction, newGroup);
        }
        teamGroup[faction].GetComponent<TeamGroup>().AddMember(actor);
    }

    private void Start()
    {
        // requestedFactionGroups = new Dictionary<Faction, List<GameObject>>();
        requestedFactionGroups = new Dictionary<Faction, Hashtable>();
        teamGroup = new Dictionary<Faction, Transform>();
    }

    private void OnDestroy()
    {
    }

    private float updateTime = 1f;
    private float timer;
    private void FixedUpdate()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            timer = updateTime;

            TeamGroupPing();
        }
    }

}
