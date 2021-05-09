using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class TrafficCarAgent : Agent
{
    private CarDriver carDriver;
    private PointPool spawnPointPool;
    private PointPool targetPointPool;

    private Transform currentTarget;

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
        if (transform && currentTarget)
        {
            Vector3 vectorToTarget = currentTarget.position - transform.position;
            // Normalized vector pointing to target
            Vector3 normDirectionToTarget = vectorToTarget.normalized;

            sensor.AddObservation(normDirectionToTarget); // Vector3, 3 observations

            float distanceToTarget = vectorToTarget.magnitude;
            sensor.AddObservation(distanceToTarget); // float, 1 observation
        }
        else
        {
            // Defaults
            sensor.AddObservation(Vector3.zero); // Vector3, 3 observations
            sensor.AddObservation(0); // float, 1 observation
        }

        // Total observations: 4
    }

    // Actions
    public override void OnActionReceived(float[] vectorAction)
    {
        // vectorAction[0] == -1 back, 0 nothing, 1 forward
        // vectorAction[1] == -1 turn left, 0 nothing, 1 turn right

        if (vectorAction[0] < 0)
        {
            AddReward(-0.01f); // Punish driving back too much
        }

        carDriver.SetInputs(vectorAction[0], vectorAction[1]);

        // Existential punishment (5000 if no MaxStep to avoid 0 division)
        AddReward(-1f / (MaxStep == 0 ? MaxStep : 5000));

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
            AddReward(-0.5f); // Heavy punishment for colliding with obstacles
        }
    }

    public void OnCollisionStay(Collision collision)
    {
        if (IsBadCollision(collision))
        {
            AddReward(-0.1f); // Smaller punishment for staying
        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "target")
        {
            AddReward(1f); // Reward for reaching the target
            targetPointPool.Return(currentTarget); // Return the target for other agents to go to
            EndEpisode();
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

        // Get a random target
        currentTarget = targetPointPool.FetchRandom();
    }
}
