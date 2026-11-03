using UnityEngine;
using UnityEngine.AI;

public class CharacterMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    // Distance from the current position for selecting a random destination; adjust for your map size
    public float wanderDistance = 20f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        MoveToRandomPoint();
    }

    void MoveToRandomPoint()
    {
        // Get a random point within wanderDistance from player's current position
        Vector3 randomDirection = Random.insideUnitSphere * wanderDistance;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
