using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class TrafficCarAgent : Agent
{
    // External Dependencies
    [SerializeField] private PathPool pathPool;

    // Components
    private CarDriver carDriver;

    // Agent Logic 
    private Path currentPath; // Current path agent is following
    private Transform currentCheckpoint; // Current checkpoint agent is trying to reach
    private float distanceMilestone; // Closest distance reached to checkpoint
    private bool inBadCollision = false; // Whether the agent is colliding with objects it shouldn't be (buildings, vehicles, etc.)

    // Init Logic (called once on Play)
    public override void Initialize()
    {
        // Dependencies
        carDriver = GetComponent<CarDriver>();
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
            // Collect Necessary Data
            // Vector pointing to checkpoint
            Vector3 vectorToCheckpoint = currentCheckpoint.position - transform.position;
            // Normalized vector pointing to checkpoint
            Vector3 normDirToCheckpoint = vectorToCheckpoint.normalized;

            // Orientation Observations
            // Dot product that indicates whether agent is facing the checkpoint
            // (+1 facing it directly, -1 facing directly away, 0 perpendicular left/right)

            float facingCheckpointDot = Vector3.Dot(transform.forward, normDirToCheckpoint);
            sensor.AddObservation(facingCheckpointDot); // float, 1 observation

            // Wedge product that indicates whether agent is facing to the left of target or to the right
            // (+1 facing right, -1 facing left, 0 facing straight at it or away)
            float wedge = Vector3.Dot(Vector3.Cross(transform.forward, normDirToCheckpoint), transform.up);

            sensor.AddObservation(wedge); // float, 1 observation

            // Distance Observations
            float distanceToCheckpoint = vectorToCheckpoint.magnitude;

            sensor.AddObservation(distanceToCheckpoint); // float, 1 observation

            // Collision Observations
            sensor.AddObservation(inBadCollision); // bool, 1 observation

            // Observation Rewards
            // When the agent gets closer to the next checkpoint
            if (distanceToCheckpoint < distanceMilestone)
            {
                // Update closest distance reached
                distanceMilestone = distanceToCheckpoint;

                AddReward(0.001f); // Small reward for getting closer to the checkpoint

                // Removed
                //// Small reward for facing the checkpoint (does not punish by going into the negatives)
                //AddReward(0.001f * Mathf.Clamp01(facingCheckpointDot));
                ////// Small reward relative to how close to next checkpoint (avoids 0 division)
                //AddReward(0.01f * distanceToCheckpoint == 0 ? 0 : 1f / distanceToCheckpoint);
            }

            if (inBadCollision)
            {
                // Punishment for staying in collision with objects like buildings, vehicles, etc.
                AddReward(-0.1f);
            }
        }
        else
        {
            sensor.AddObservation(new float[4]);
        }

        // Total observations: 4
    }

    // Actions
    public override void OnActionReceived(float[] vectorAction)
    {
        // vectorAction[0] == 0 back, 1 nothing, 2 forward
        // vectorAction[1] == 0 turn left, 1 nothing, 2 turn right

        float forward = vectorAction[0] - 1;
        float turn = vectorAction[1] - 1;

        carDriver.SetInputs(forward, turn);

        // Action Rewards
        //if (inBadCollision && (forward != 0 && turn != 0))
        //{
        //    // Small reward back for moving while in a bad collision
        //    // Encouraging agent to get out of bad collisions
        //    AddReward(0.001f);
        //}

        // Removed
        //// Existential punishment (avoids 0 division)
        //AddReward(MaxStep == 0 ? 0 : -1f / MaxStep);
    }

    // Heuristic (player controlled)
    public override void Heuristic(float[] actionsOut)
    {
        float forward = 1; // do nothing
        float turn = 1; // do nothing

        if (Input.GetKey(KeyCode.W)) forward += 1;
        if (Input.GetKey(KeyCode.S)) forward -= 1;
        if (Input.GetKey(KeyCode.A)) turn -= 1;
        if (Input.GetKey(KeyCode.D)) turn += 1;

        actionsOut[0] = forward;
        actionsOut[1] = turn;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (IsBadCollision(collision))
        {
            inBadCollision = true;
        }
    }

    public void OnCollisionExit(Collision collision)
    {
        if (IsBadCollision(collision))
        {
            inBadCollision = false;
        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        // If it is the current checkpoint the agent is looking for
        if (collider.transform == currentCheckpoint)
        {
            // Reward for reaching the checkpoint facing it, punishment for driving into it backwards
            Vector3 vectorToCheckpoint = currentCheckpoint.position - transform.position;
            Vector3 normDirToCheckpoint = vectorToCheckpoint.normalized;
            float facingCheckpointDot = Vector3.Dot(transform.forward, normDirToCheckpoint);
            AddReward(1f * facingCheckpointDot);

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
