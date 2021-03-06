using UnityEngine;

public class CarDriver : MonoBehaviour
{

    #region Fields
    private float speed;
    private float speedMax = 18f;
    private float speedMin = -20f;
    private float acceleration = 20f;
    private float brakeSpeed = 150f;
    private float reverseSpeed = 10f;
    private float idleSlowdown = 10f;

    private float turnSpeed;
    private float turnSpeedMax = 400f;
    private float turnSpeedAcceleration = 400f;
    private float turnIdleSlowdown = 700f;

    private float forwardAmount;
    private float turnAmount;

    private Rigidbody carRigidbody;
    #endregion

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (forwardAmount > 0)
        {
            // Accelerating
            speed += forwardAmount * acceleration * Time.deltaTime;
        }
        if (forwardAmount < 0)
        {
            if (speed > 0)
            {
                // Braking
                speed += forwardAmount * brakeSpeed * Time.deltaTime;
            }
            else
            {
                // Reversing
                speed += forwardAmount * reverseSpeed * Time.deltaTime;
            }
        }

        if (forwardAmount == 0)
        {
            // Not accelerating or braking
            if (speed > 0)
            {
                speed -= idleSlowdown * Time.deltaTime;
            }
            if (speed < 0)
            {
                speed += idleSlowdown * Time.deltaTime;
            }
        }

        speed = Mathf.Clamp(speed, speedMin, speedMax);

        carRigidbody.velocity = transform.forward * speed;
        carRigidbody.velocity += Physics.gravity;


        if (speed < 0)
        {
            // Going backwards, invert wheels
            turnAmount = turnAmount * -1f;
        }

        if (turnAmount > 0 || turnAmount < 0)
        {
            // Turning
            if ((turnSpeed > 0 && turnAmount < 0) || (turnSpeed < 0 && turnAmount > 0))
            {
                // Changing turn direction
                float minTurnAmount = 20f;
                turnSpeed = turnAmount * minTurnAmount;
            }
            turnSpeed += turnAmount * turnSpeedAcceleration * Time.deltaTime;
        }
        else
        {
            // Not turning
            if (turnSpeed > 0)
            {
                turnSpeed -= turnIdleSlowdown * Time.deltaTime;
            }
            if (turnSpeed < 0)
            {
                turnSpeed += turnIdleSlowdown * Time.deltaTime;
            }
            if (turnSpeed > -1f && turnSpeed < +1f)
            {
                // Stop rotating
                turnSpeed = 0f;
            }
        }

        float speedNormalized = speed / speedMax;
        float invertSpeedNormalized = Mathf.Clamp(1 - speedNormalized, .75f, 1f);

        turnSpeed = Mathf.Clamp(turnSpeed, -turnSpeedMax, turnSpeedMax);

        carRigidbody.angularVelocity = new Vector3(0, turnSpeed * (invertSpeedNormalized * 1f) * Mathf.Deg2Rad, 0);

        if (transform.eulerAngles.x > 2 || transform.eulerAngles.x < -2 || transform.eulerAngles.z > 2 || transform.eulerAngles.z < -2)
        {
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "vehicle" || collision.gameObject.tag == "building")
        {
            speed = Mathf.Clamp(speed, 0f, 20f);
        }
    }

    public void SetInputs(float forwardAmount, float turnAmount)
    {
        this.forwardAmount = forwardAmount;
        this.turnAmount = turnAmount;
    }

    public void ClearTurnSpeed()
    {
        turnSpeed = 0f;
    }

    public float GetSpeed()
    {
        return speed;
    }


    public void SetSpeedMax(float speedMax)
    {
        this.speedMax = speedMax;
    }

    public void SetTurnSpeedMax(float turnSpeedMax)
    {
        this.turnSpeedMax = turnSpeedMax;
    }

    public void SetTurnSpeedAcceleration(float turnSpeedAcceleration)
    {
        this.turnSpeedAcceleration = turnSpeedAcceleration;
    }

    public void StopCompletely()
    {
        speed = 0f;
        turnSpeed = 0f;
    }

}
