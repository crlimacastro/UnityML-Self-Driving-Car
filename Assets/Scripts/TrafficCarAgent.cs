using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class TrafficCarAgent : Agent
{
    [Header("Dependencies")]
    public Transform target;

    private CarDriver carDriver;
    private Vector3 initialPosition;

    // Init Logic (called once on Play)
    public override void Initialize()
    {
        carDriver = GetComponent<CarDriver>();
        initialPosition = transform.position;
    }

    // Episode reset logic (called at the start of every episode)
    public override void OnEpisodeBegin()
    {
        // Go back to initial position
        transform.position = initialPosition;

        // Zero out all velocities
        carDriver.StopCompletely();
    }

    // Observations
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 vectorToTarget = target.position - transform.position;
        // Normalized vector pointing to target
        Vector3 normDirectionToTarget = vectorToTarget.normalized;

        sensor.AddObservation(normDirectionToTarget); // Vector3, 3 observations
    }

    // Actions
    public override void OnActionReceived(float[] vectorAction)
    {
        // vectorAction[0] == -1 back, 0 nothing, 1 forward
        // vectorAction[1] == -1 turn left, 0 nothing, 1 turn right
        carDriver.SetInputs(vectorAction[0], vectorAction[1]);


        if (MaxStep != 0)
        {
            // Existential punishment
            AddReward(-1f / MaxStep);
        }
        else
        {
            // Default to a punishment as if MaxStep was 5000
            AddReward(-1f / 5000f);
        }
        
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
        if (isBadCollision(collision))
        {
            AddReward(-0.5f); // Heavy punishment
        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "target")
        {
            AddReward(1f); // Reward for reaching the target
            EndEpisode();
        }
    }

    // Helper functions
    public bool isBadCollision(Collision collision)
    {
        return collision.gameObject.tag == "vehicle" || collision.gameObject.tag == "building" || collision.gameObject.tag == "sidewalk";
    }
}
