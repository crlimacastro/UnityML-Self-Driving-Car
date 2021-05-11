using UnityEngine;

public class FollowTargetHorizontally : MonoBehaviour
{
    public Transform target;
    public float lerpSpeed = 10f;

    private void Update()
    {
        Vector3 newPosition = Vector3.Lerp(transform.position, target.position, Time.deltaTime * lerpSpeed);
        transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.z);
    }
}