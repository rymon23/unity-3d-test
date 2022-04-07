using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
// using Panda;

public class ActorAI : MonoBehaviour
{
    public Transform player;
    public Slider healthBar;   
    NavMeshAgent agent;
    public Vector3 destination; // The movement destination.
    public Vector3 target;      // The position to aim to.
    float health = 100.0f;
    float rotSpeed = 5.0f;

    float visibleRange = 80.0f;
    float shotRange = 40.0f;

    float  wanderRadius = 100.0f;

    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        agent.stoppingDistance = shotRange - 5; //for a little buffer
        InvokeRepeating("UpdateHealth",5,0.5f);
    }

    void Update()
    {
        // Vector3 healthBarPos = Camera.main.WorldToScreenPoint(this.transform.position);
        // healthBar.value = (int)health;
        // healthBar.transform.position = healthBarPos + new Vector3(0,60,0);
    }

    void UpdateHealth()
    {
       if(health < 100)
        health ++;
    }

    void OnCollisionEnter(Collision col)
    {
        if(col.gameObject.tag == "bullet")
        {
            health -= 10;
        }
    }


    public Vector3 GetBehindPosition(Transform target, float distanceBehind) {
        return target.position - (target.forward * distanceBehind);
    }
    public Vector3 UpdateFallBackPosition(Transform target, float distance){
        return RandomNavmeshLocation(distance, GetBehindPosition(target, distance));
    }
    public Vector3 RandomNavmeshLocation(float radius, Vector3 center ) {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
         randomDirection += center;
         NavMeshHit hit;
         Vector3 finalPosition = Vector3.zero;
         if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1)) {
             finalPosition = hit.position;            
         }
         return finalPosition;
     }


    // [Task]
    // public void PickDestination(float x, float z)
    // {
    //     Vector3 dest = new Vector3(x,0,z);
    //     agent.SetDestination(dest);
    //     Task.current.Succeed();
    // }

    // [Task]
    // public void PickRandomDestination()
    // {
    //     Vector3 dest = RandomNavmeshLocation(wanderRadius, transform.position);
    //     agent.SetDestination(dest);
    //     Task.current.Succeed();
    // }


    // [Task]
    // public void MoveToDestination()
    // {
    //     if( Task.isInspected )
    //             Task.current.debugInfo = string.Format("t={0:0.00}", Time.time);

    //     if(agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
    //     {
    //         Task.current.Succeed();
    //     }   
    // }

    // [Task]
    // public void TargetPlayer()
    // {
    //     target = player.transform.position;
    //     Task.current.Succeed();
    // }

    // [Task]
    // bool Turn( float angle )
    // {
    //     var p = this.transform.position +  Quaternion.AngleAxis( angle, Vector3.up) * this.transform.forward;
    //     target = p;
    //     return true;
    // }

    // [Task]
    // public void LookAtTarget()
    // {
    //     Vector3 direction = target - this.transform.position;

    //     this.transform.rotation = Quaternion.Slerp(this.transform.rotation, 
    //                                             Quaternion.LookRotation(direction), 
    //                                             Time.deltaTime*rotSpeed);

    //     if( Task.isInspected )
    //             Task.current.debugInfo = string.Format("angle={0}", 
    //                 Vector3.Angle(this.transform.forward,direction));
        
    //     if(Vector3.Angle(this.transform.forward,direction) < 5.0f)
    //     {
    //         Task.current.Succeed();
    //     }
    // }

    // [Task]
    // bool SeePlayer()
    // {
    //     Vector3 distance = player.transform.position - this.transform.position;
        
    //     RaycastHit hit;
    //     bool seeWall = false;

    //     Debug.DrawRay(this.transform.position,distance, Color.red);

    //     if (Physics.Raycast(this.transform.position, distance, out hit))
    //     {
    //         if(hit.collider.gameObject.tag == "wall")
    //         {
    //             seeWall = true;
    //         }
    //     }

    //     if( Task.isInspected )
    //             Task.current.debugInfo = string.Format("wall={0}", seeWall);

    //     if(distance.magnitude < visibleRange && !seeWall)
    //         return true;
    //     else
    //         return false;
    // }

    // [Task]
    // public bool IsHealthLessThan( float health )
    // {
    //     return this.health < health;
    // }

    // [Task]
    // public bool InDanger( float minDist)
    // {
    //     Vector3 distance = player.transform.position - this.transform.position;
    //     return (distance.magnitude < minDist);
    // }

    // [Task]
    // public void TakeCover()
    // {
    //     Vector3 awayFromPlayer = this.transform.position - player.transform.position;
    //     Vector3 dest = this.transform.position + awayFromPlayer * 2;
    //     agent.SetDestination(dest);
    //     Task.current.Succeed();
    // }

    // [Task]
    // public bool Explode()
    // {
    //     Destroy(healthBar.gameObject);
    //     Destroy(this.gameObject);
    //     return true;
    // }

    // [Task]
    // public void SetTargetDestination()
    // {
    //     agent.SetDestination(target);
    //     Task.current.Succeed();
    // }
    
    // [Task]
    // bool ShotLinedUp()
    // {
    //     Vector3 distance = target - this.transform.position;
    //     if(distance.magnitude < shotRange &&
    //         Vector3.Angle(this.transform.forward, distance) < 1.0f)
    //         return true;
    //     else
    //         return false;
    // }

}