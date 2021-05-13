using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathPool : MonoBehaviour
{
    private List<Path> paths = new List<Path>();
    private System.Random random = new System.Random();

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            paths.Add(child.GetComponent<Path>());
        }
    }

    public Path FetchRandom()
    {
        // Shuffle pool
        paths = paths.OrderBy(p => random.Next()).ToList();

        // Return the first non active path in pool
        // And set it to active
        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i].gameObject.activeInHierarchy == false)
            {
                paths[i].gameObject.SetActive(true);
                return paths[i];
            }
        }

        // No inactive path found
        return null;
    }

    public void Return(Path path)
    {
        if (paths.Contains(path))
        {
            path.gameObject.SetActive(false);
        }
    }
}
