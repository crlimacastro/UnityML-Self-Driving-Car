using UnityEngine;

public class WakeUpRBOnCollisionWithCar : MonoBehaviour
{
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.Sleep();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If a car hit this
        if (collision.gameObject.TryGetComponent<CarDriver>(out var car))
        {
            rb.WakeUp();
        }
    }
}