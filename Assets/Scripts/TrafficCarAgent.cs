using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class TrafficCarAgent : Agent
{
    private CarDriver carDriver;
    private PointPool spawnPointPool;
    private PointPool targetPointPool;

    private Transform currentTarget;
    private float closestReachedToTarget;

    // Init Logic (called once on Play)
    public override void Initialize()
    {
        carDriver = GetComponent<CarDriver>();
        spawnPointPool = GameObject.Find("SpawnPoints").GetComponent<PointPool>();
        targetPointPool = GameObject.Find("Targets").GetComponent<PointPool>();
    }

    // Episode reset logic (called at the start of every episode)
    public override void OnEpisodeBegin()
    {
        Reset();
    }

    // Observations
    public override void CollectObservations(VectorSensor sensor)
    {
        if (transform)
        {
            // Observe the agent's position
            sensor.AddObservation(transform.position); // Vector3, 3 observations 

            // Observer the agent's local rotation
            sensor.AddObservation(transform.localRotation.normalized); // Quaternion, (4 observations)
        }
        else
        {
            sensor.AddObservation(new float[7]);
        }
        if (transform && currentTarget)
        {
            Vector3 vectorToTarget = currentTarget.position - transform.position;
            // Normalized vector pointing to target
            Vector3 normDirectionToTarget = vectorToTarget.normalized;
            sensor.AddObservation(normDirectionToTarget); // Vector3, 3 observations

            // Dot product that indicates whether agent is facing the target
            // (+1 they are directly facing it directly, -1 they are facing directly away, 0 they are facing perpendicularly left/right)
            sensor.AddObservation(Vector3.Dot(transform.forward, normDirectionToTarget)); // float, 1 observation

            // Wedge product that indicates whether agent is pointing to the left of target or to the right
            // (+1 they are pointing right, -1 they are pointing left, 0 they are pointing straight at it or away)
            float wedge = Vector3.Dot(Vector3.Cross(transform.forward, normDirectionToTarget), transform.up);
            sensor.AddObservation(wedge); // float, 1 observation

            float distanceToTarget = vectorToTarget.magnitude;
            sensor.AddObservation(distanceToTarget); // float, 1 observation

            // Reward the agent every time they get closer to the target (avoids zero division)
            if (distanceToTarget < closestReachedToTarget)
            {
                closestReachedToTarget = distanceToTarget; // Update the closest the agent has gotten
                float inverseDistance = distanceToTarget == 0 ? 0 : 1f / distanceToTarget; // Bigger when the agent is closer
                AddReward(inverseDistance);
            }
        }
        else
        {
            sensor.AddObservation(new float[6]);
        }

        // Total observations: 13
    }

    // Actions
    public override void OnActionReceived(float[] vectorAction)
    {
        // vectorAction[0] == -1 back, 0 nothing, 1 forward
        // vectorAction[1] == -1 turn left, 0 nothing, 1 turn right

        carDriver.SetInputs(vectorAction[0], vectorAction[1]);

        // Existential punishment (avoids 0 division)
        float existentialPunishment = MaxStep == 0 ? 0 : -1f / MaxStep;
        AddReward(existentialPunishment);
    }

    // Heuristic (player controlled)
    public override void Heuristic(float[] actionsOut)
    {
        // actionsOut[0] == -1 back, 0 nothing, 1 forward
        // actionsOut[1] == -1 turn left, 0 nothing, 1 turn right
        actionsOut[0] = Input.GetAxis("Vertical");
        actionsOut[1] = Input.GetAxis("Horizontal");
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (IsBadCollision(collision))
        {
            AddReward(-1f); // Heavy punishment for colliding with obstacles
        }
    }

    public void OnCollisionStay(Collision collision)
    {
        if (IsBadCollision(collision))
        {
            AddReward(-0.01f); // Smaller punishment for staying
        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "target")
        {
            AddReward(20f); // Reward for reaching the target
            Debug.Log("Reward: " + GetCumulativeReward());
            Reset();
        }
    }

    // Helper functions
    private bool IsBadCollision(Collision collision)
    {
        string tag = collision.gameObject.tag;
        return tag == "vehicle" || tag == "building" || tag == "sidewalk" || tag == "props";
    }

    private void Reset()
    {
        // Zero out all velocities
        carDriver.StopCompletely();

        // Go to a random spawn point
        Transform spawnPoint = spawnPointPool.FetchRandom();
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        spawnPointPool.Return(spawnPoint);

        // Return previous target (if any)
        if (currentTarget)
        {
            targetPointPool.Return(currentTarget); // Return the target for other agents to go to
        }

        // Get a new random target
        currentTarget = targetPointPool.FetchRandom();
        // Reset closest distance to initial distance
        closestReachedToTarget = (currentTarget.position - transform.position).magnitude; 
    }

    private void OnDrawGizmosSelected()
    {
        if (transform && currentTarget)
        {
            Gizmos.DrawRay(transform.position, currentTarget.position - transform.position);
        }
    }
}
