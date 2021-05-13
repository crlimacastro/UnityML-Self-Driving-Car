using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    private List<Transform> checkpoints = new List<Transform>();

    public Transform Start => checkpoints[0];
    public Transform End => checkpoints[checkpoints.Count - 1];


    private void Awake()
    {
        // Get all checkpoints, make their gameObjects inactive and add them to the list
        // Make each checkpoint's forward face the next one (except the first and last one)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // Set checkpoint to inavtive
            child.gameObject.SetActive(false);

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

    // Every time this object is set to active
    private void OnEnable()
    {
        // Set the start to active
        Start.gameObject.SetActive(true);
    }

    // Every time this object is set to inactive
    private void OnDisable()
    {
        // Set each checkpoint to be inactive
        foreach (Transform checkpoint in checkpoints)
        {
            checkpoint.gameObject.SetActive(false);
        }
    }

    // Returns the checkpoint that goes after the one provided.
    // Returns null if no next checkpoint
    public Transform GetNextCheckPoint(Transform currentCheckpoint)
    {
        int i = checkpoints.IndexOf(currentCheckpoint);

        if(i == -1)
        {
            throw new UnityException("Checkpoint not found in path");
        }
        else if(i == checkpoints.Count - 1) {
            return null;
        }
        else
        {
            return checkpoints[i + 1];
        }
    }
}
