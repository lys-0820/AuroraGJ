using UnityEngine;

public class MazeExit : MonoBehaviour
{
    private MazeManager mazeManager;

    private void Awake()
    {
        // 运行时自动在场景中找 MazeManager
        mazeManager = FindObjectOfType<MazeManager>();
        if (mazeManager == null)
        {
            Debug.LogError("MazeExit: Cannot find MazeManager in scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && mazeManager != null)
        {
            mazeManager.GenerateNewMaze();
        }
    }
}
