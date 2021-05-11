using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class TrafficCarAgent : Agent
{
    // Dependencies
    private CarDriver carDriver;
    private PathPool pathPool;

    // Agent Logic 
    private Path currentPath; // Current path agent is following
    private int currentCheckpointIndex; // Index pointing to current checkpoint agent is trying to reach
    private Transform CurrentCheckpoint => currentPath[currentCheckpointIndex]; // Shortcut to current checkpoint transform

    // Init Logic (called once on Play)
    public override void Initialize()
    {
        // Dependencies
        carDriver = GetComponent<CarDriver>();
        pathPool = FindObjectOfType<PathPool>();
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
        if (transform && CurrentCheckpoint)
        {
            Vector3 vectorToCheckpoint = CurrentCheckpoint.position - transform.position;
            // Normalized vector pointing to checkpoint
            Vector3 normDirToCheckpoint = vectorToCheckpoint.normalized;

            sensor.AddObservation(normDirToCheckpoint); // Vector3, 3 observations

            // Dot product that indicates whether agent is facing the target
            // (+1 facing it directly, -1 facing directly away, 0 perpendicular left/right)
            sensor.AddObservation(Vector3.Dot(transform.forward, normDirToCheckpoint)); // float, 1 observation

            // Wedge product that indicates whether agent is facing to the left of target or to the right
            // (+1 facing right, -1 facing left, 0 facing straight at it or away)
            float wedge = Vector3.Dot(Vector3.Cross(transform.forward, normDirToCheckpoint), transform.up);

            sensor.AddObservation(wedge); // float, 1 observation

            // Dot product that indicates whether the agent is facing in the direction that the checkpoint points to
            sensor.AddObservation(Vector3.Dot(transform.forward, CurrentCheckpoint.forward)); // float, 1

            float distanceToTarget = vectorToCheckpoint.magnitude;

            sensor.AddObservation(distanceToTarget); // float, 1 observation
        }
        else
        {
            sensor.AddObservation(new float[7]);
        }

        // Total observations: 14
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
        // If it is the current checkpoint the agent is looking for
        if (collider.transform == CurrentCheckpoint)
        {
            AddReward(1f); // Reward for reaching the checkpoint

            // Make checkpoint reached invisible
            CurrentCheckpoint.GetComponent<MeshRenderer>().enabled = false;

            // Update the currentCheckpoint that is being looked for
            currentCheckpointIndex++;

            // Don't go over the last index of the path (avoid index out of range)
            currentCheckpointIndex = Mathf.Min(currentCheckpointIndex, currentPath.Count - 1);

            // If this was the last checkpoint in the path
            if (collider.transform == currentPath.End)
            {
                Debug.Log("Reward: " + GetCumulativeReward());
                Reset();
            }
            else
            {
                // Make new checkpoint visible
                CurrentCheckpoint.GetComponent<MeshRenderer>().enabled = true;
            }
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

        // Return previous path being followed (if any)
        if (currentPath)
        {
            // If the agent has not reached it's checkpoint
            // (Happens on reset from OnEpisodeBegin)
            if (CurrentCheckpoint)
            {
                // Make the check point that was not reached invisible again
                CurrentCheckpoint.GetComponent<MeshRenderer>().enabled = false;
            }

            pathPool.Return(currentPath); // Returns path for other agents to use
        }
        // Get a random path
        currentPath = pathPool.FetchRandom();
        // Reset current checkpoint index
        currentCheckpointIndex = 0;
        // Go to its start point
        transform.position = currentPath.Start.position;
        transform.rotation = currentPath.Start.rotation;
    }
}
