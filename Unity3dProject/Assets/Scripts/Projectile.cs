using UnityEngine;


public enum ProjectileType
{
    spell = 0,
    bullet = 1,
    arrow = 2,
    bolt = 3,
    spear = 4
}

public class Projectile : MonoBehaviour
{
    public ProjectileType projectileType;
    public GameObject sender;
    public float damage = 0f;
    public float speed = 24f;
    public Weapon weapon;
    [SerializeField] public MagicSpell[] spells;// { get; private set; }
    [SerializeField] private bool timedSelfdestruct = true;
    [SerializeField] private float selfDestructTimer = 6f;
    private Rigidbody rigidbody;
    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.velocity = transform.forward * speed;

        if (timedSelfdestruct)
        {
            Destroy(gameObject, selfDestructTimer);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }
    public void InvokeSpells(GameObject target, GameObject sender)
    {
        if (spells != null && spells.Length > 0)
        {
            Debug.Log("InvokeSpells - " + spells.Length);
            spells[0].InvokeEFfects(target, sender);
        }
    }


}
