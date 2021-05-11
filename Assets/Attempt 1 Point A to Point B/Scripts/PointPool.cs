using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointPool : MonoBehaviour
{
    private List<Transform> points = new List<Transform>();
    private System.Random random = new System.Random();

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            points.Add(child);
        }
    }

    public Transform FetchRandom()
    {
        // Shuffle pool
        points = points.OrderBy(p => random.Next()).ToList();

        // Return the first non active point in pool
        // And set it to active
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].gameObject.activeInHierarchy == false)
            {
                points[i].gameObject.SetActive(true);
                return points[i];
            }
        }

        // No inactive point found
        return null;
    }

    public virtual void Return(Transform point)
    {
        if (points.Contains(point))
        {
            point.gameObject.SetActive(false);
        }
    }
}
