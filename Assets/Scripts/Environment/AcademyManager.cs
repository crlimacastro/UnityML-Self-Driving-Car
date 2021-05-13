using UnityEngine;
using Unity.MLAgents;
using System;

public class AcademyManager : MonoBehaviour
{
    [SerializeField] private int maxStep = 5000; // Global max step for all agents in the scene
    private Agent[] agents; // All agents in the scene

    public int MaxStep => maxStep;
    public event Action OnEpisodeEnd;

    private void Awake()
    {
        // Get all agents on awake
        agents = FindObjectsOfType<Agent>();
        // Set their max step to the academy's
        foreach (Agent agent in agents)
        {
            agent.MaxStep = maxStep;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reset();
        }
    }

    private void FixedUpdate()
    {
        if (Academy.Instance.StepCount % maxStep == 0)
        {
            OnEpisodeEnd?.Invoke();
        }
    }

    // Helper methods
    private void Reset()
    {
        foreach (Agent agent in agents)
        {
            agent.EndEpisode();
        }
    }
}
