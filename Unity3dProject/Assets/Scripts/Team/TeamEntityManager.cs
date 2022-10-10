using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamEntityManager : MonoBehaviour
{
    [SerializeField]
    private TeamEntity[] teamEntities;

    void Start()
    {
    }

    void Update()
    {
        if (teamEntities?.Length > 0)
        {
            for (var i = 0; i < teamEntities.Length; i++)
            {
                // teamEntities[i];
            }
        }
    }
}
