using UnityEngine;

public class HealthBar : MonoBehaviour
{
    ActorEventManger actorEventManger;
    [SerializeField] Transform forground;

    private void Start()
    {
        actorEventManger = GetComponentInParent<ActorEventManger>();
        // if (actorEventManger != null) actorEventManger.onUpdateHealthBar += onUpdateHealthBar;
    }


    public void UpdateBar(float fPercent)
    {
        if (forground == null) forground = transform.Find("Foreground");

        if (forground != null) forground.localScale = new Vector3(Mathf.Clamp(fPercent, 0, 1), 1);
    }

    void onUpdateHealthBar(float fPercent) => UpdateBar(fPercent);

    private void OnDestroy()
    {
        // if (actorEventManger != null) actorEventManger.onTakeHit -= onUpdateHealthBar;
    }

    // void Update()
    // {
    //     transform.LookAt(Camera.main.transform);
    // }
}
