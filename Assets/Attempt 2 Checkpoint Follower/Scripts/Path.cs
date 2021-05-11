using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    private List<Transform> checkpoints = new List<Transform>();

    public Transform Start => checkpoints[0];
    public Transform End => checkpoints[checkpoints.Count - 1];
    public int Count => checkpoints.Count;
    public Transform this[int i] => checkpoints[i];


    private void Awake()
    {
        // Get all checkpoints and add them to the list
        // Make each checkpoint's forward face the next one (except the first and last one)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // Add checkpoint to list
            checkpoints.Add(child);

            // Do this for every child except the first and last one
            if (i > 0 && i < transform.childCount - 1)
            {
                Transform nextChild = transform.GetChild(i + 1);

                // Change checkpoint's forward to face the next one
                Vector3 vectorToNextCheckpoint = nextChild.position - child.position;
                child.forward = vectorToNextCheckpoint.normalized;
            }
        }

        // Check that there were at least two
        if (checkpoints.Count <= 1)
        {
            throw new UnityException("Path must contain at least two checkpoints");
        }
    }
}
