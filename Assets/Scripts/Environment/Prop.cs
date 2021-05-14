using UnityEngine;

public class Prop : MonoBehaviour
{
    private Rigidbody rb;
    private AcademyManager academyManager;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        academyManager = FindObjectOfType<AcademyManager>();

        rb.isKinematic = true;
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnEnable()
    {
        academyManager.OnEpisodeEnd += Reset;
    }

    private void OnDisable()
    {
        academyManager.OnEpisodeEnd -= Reset;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If it was a car or a prop that hit this
        if (collision.gameObject.TryGetComponent<CarDriver>(out var agent) || collision.gameObject.TryGetComponent<Prop>(out var prop))
        {
            rb.isKinematic = false;
        }
    }

    private void Reset()
    {
        rb.isKinematic = true;
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}