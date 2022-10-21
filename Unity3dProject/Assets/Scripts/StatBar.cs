using UnityEngine;

public enum StatType
{
    health = 0,
    stamina = 1,
    magic = 2,
}

public class StatBar : MonoBehaviour
{
    ActorEventManger actorEventManger;
    [SerializeField] Transform forground;
    [SerializeField] StatType stat;

    private void Start()
    {
        actorEventManger = GetComponentInParent<ActorEventManger>();
        if (actorEventManger != null)
        {
            switch (stat)
            {
                case StatType.stamina:
                    actorEventManger.OnUpdateStatBar_Stamina += OnUpdateStatBar;
                    break;
                case StatType.magic:
                    actorEventManger.OnUpdateStatBar_Magic += OnUpdateStatBar;
                    break;
                default:
                    actorEventManger.OnUpdateStatBar_Health += OnUpdateStatBar;
                    break;
            }
        }
    }


    public void UpdateBar(float fPercent)
    {
        if (forground == null) forground = transform.Find("Foreground");

        if (forground != null) forground.localScale = new Vector3(Mathf.Clamp(fPercent, 0, 1), 1);
    }

    void OnUpdateStatBar(float fPercent) => UpdateBar(fPercent);

    private void OnDestroy()
    {
        if (actorEventManger != null)
        {
            switch (stat)
            {
                case StatType.stamina:
                    actorEventManger.OnUpdateStatBar_Stamina -= OnUpdateStatBar;
                    break;
                case StatType.magic:
                    actorEventManger.OnUpdateStatBar_Magic -= OnUpdateStatBar;
                    break;
                default:
                    actorEventManger.OnUpdateStatBar_Health -= OnUpdateStatBar;
                    break;
            }
        }
    }
}