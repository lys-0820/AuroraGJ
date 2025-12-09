using System.Collections.Generic;
using UnityEngine;

public class MainPath : MonoBehaviour
{
    public List<Transform> waypoints = new List<Transform>();

    void Awake()
    {
        waypoints.Clear();
        foreach (Transform child in transform)
        {
            waypoints.Add(child);
        }
        // waypoints.Sort((a, b) => string.Compare(a.name, b.name));
    }
}
