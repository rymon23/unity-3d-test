using System;
using Hybrid.Components;
using UnityEngine;

public struct LocationalAction
{
    public LocationalAction(Vector3 _position, float _radius)
    {
        radius = _radius;
        position = _position;
    }

    public Vector3 position { get; }

    public float radius { get; }
}

public class GlobalEventManager : MonoBehaviour
{
    public static GlobalEventManager current;

    public static bool bDebug = true;


#region Territory Events
    public static event Action<Territory, Territory.InvasionState>
        onTerritoryInvasionState;

    public static void TerritoryInvasionStateChange(
        Territory territory,
        Territory.InvasionState invasionState
    ) => onTerritoryInvasionState?.Invoke(territory, invasionState);
#endregion



#region LocationAction
    public static float locationalActionBroadcastCoolDownTime = 0.5f;

    [SerializeField]
    private static float locationalActionBroadcastCoolDownTimer = 0;

    public static event Action<Vector3, float> onLocationalActionBroadcast;

    private static Nullable<LocationalAction> lastLocationAction = null;

    public static void LocationalActionBroadcast(Vector3 position, float radius)
    {
        // if (lastLocationAction != null)
        // {
        //     LocationalAction la = (LocationalAction)lastLocationAction;
        //     if (Vector3.Distance(la.position, position) > radius * 0.6f)
        //     {
        //         HandleLocationalActionBroadcast(position, radius);
        //         return;
        //     }
        // }
        if (locationalActionBroadcastCoolDownTimer > 0)
        {
            return;
        }

        HandleLocationalActionBroadcast (position, radius);
    }

    private static void HandleLocationalActionBroadcast(
        Vector3 position,
        float radius
    )
    {
        locationalActionBroadcastCoolDownTimer =
            locationalActionBroadcastCoolDownTime;
        onLocationalActionBroadcast?.Invoke(position, radius);

        lastLocationAction = new LocationalAction(position, radius);
    }
#endregion


    public event Action<Vector3> onCombatAlert;

    public void CombatAlert(Vector3 position) =>
        onCombatAlert?.Invoke(position);

    private void OnDestroy()
    {
        lastLocationAction = null;
    }

    private void FixedUpdate()
    {
        if (locationalActionBroadcastCoolDownTimer > 0)
        {
            locationalActionBroadcastCoolDownTimer -= Time.deltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        if (!bDebug) return;

        if (lastLocationAction != null)
        {
            LocationalAction la = (LocationalAction) lastLocationAction;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(la.position, la.radius);
        }
    }
}
