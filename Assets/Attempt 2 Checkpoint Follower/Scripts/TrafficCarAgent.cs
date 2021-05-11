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
    private Transform currentCheckpoint; // Current checkpoint agent is trying to reach
    private float distanceMilestone; // Closest distance reached to checkpoint

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
        if (transform && currentCheckpoint)
        {
            // Vector pointing to checkpoint
            Vector3 vectorToCheckpoint = currentCheckpoint.position - transform.position;
            // Normalized vector pointing to checkpoint
            Vector3 normDirToCheckpoint = vectorToCheckpoint.normalized;

            // Dot product that indicates whether agent is facing the checkpoint
            // (+1 facing it directly, -1 facing directly away, 0 perpendicular left/right)
            sensor.AddObservation(Vector3.Dot(transform.forward, normDirToCheckpoint)); // float, 1 observation

            float distanceToCheckpoint = vectorToCheckpoint.magnitude;

            sensor.AddObservation(distanceToCheckpoint); // float, 1 observation

            // When the agent gets closer to the next checkpoint
            if (distanceToCheckpoint < distanceMilestone)
            {
                // Update closest distance reached
                distanceMilestone = distanceToCheckpoint;

                // Small reward relative to how close to next checkpoint (avoids 0 division)
                AddReward(distanceToCheckpoint == 0 ? 0 : 1f / distanceToCheckpoint);
            }

        }
        else
        {
            sensor.AddObservation(new float[2]);
        }

        // Total observations: 2
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

    public void OnCollisionStay(Collision collision)
    {
        if (IsBadCollision(collision))
        {
            AddReward(-0.1f); // Punishment for staying in bad collision
        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        // If it is the current checkpoint the agent is looking for
        if (collider.transform == currentCheckpoint)
        {
            AddReward(1f); // Reward for reaching the checkpoint

            // Set previous checkpoint to inactive
            currentCheckpoint.gameObject.SetActive(false);

            // Update the current checkpoint
            currentCheckpoint = currentPath.GetNextCheckPoint(currentCheckpoint);
            
            if (currentCheckpoint)
            {
                // Update closest distance reached
                distanceMilestone = GetDistanceToCheckpoint(currentCheckpoint);

                // Set the current checkpoint to active
                currentCheckpoint.gameObject.SetActive(true);
            }
            else
            {
                // Only null if this was the last checkpoint in the path
                // Reset agent
                Reset();
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
            pathPool.Return(currentPath); // Returns path for other agents to use
        }

        // Get a random path
        currentPath = pathPool.FetchRandom();
        // Go to its start position and rotation
        transform.position = currentPath.Start.position;
        transform.rotation = currentPath.Start.rotation;

        // Set the current checkpoint to the first in the path
        currentCheckpoint = currentPath.Start;
        // Update closest distance reached
        distanceMilestone = GetDistanceToCheckpoint(currentCheckpoint);
    }

    private float GetDistanceToCheckpoint(Transform checkpoint)
    {
        return (checkpoint.position - transform.position).magnitude;
    }
}
