using UnityEngine;

public class MazeExit : MonoBehaviour
{
    private MazeManager mazeManager;
    private Transform player;
    private Collider exitCollider;

    [Header("出现距离设置")]
    [Tooltip("玩家距离出口小于这个值时，出口才会出现并可触发")]
    public float appearDistance = 50f;

    [Tooltip("需要一起隐藏/显示的渲染器，不填会自动抓取自身所有 Renderer")]
    public Renderer[] renderersToToggle;

    private bool isVisible = false;

    private void Awake()
    {
        // 找 MazeManager
        mazeManager = FindObjectOfType<MazeManager>();
        if (mazeManager == null)
        {
            Debug.LogError("MazeExit: Cannot find MazeManager in scene!");
        }
        else
        {
            player = mazeManager.player;
            if (player == null)
            {
                Debug.LogWarning("MazeExit: MazeManager.player is null, distance check will not work.");
            }
        }

        exitCollider = GetComponent<Collider>();

        // 如果没有手动拖渲染器，就自动拿自己和子物体上的所有 Renderer
        if (renderersToToggle == null || renderersToToggle.Length == 0)
        {
            renderersToToggle = GetComponentsInChildren<Renderer>();
        }

        // 初始先隐藏
        SetVisible(false);
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        bool shouldBeVisible = dist <= appearDistance;

        if (shouldBeVisible != isVisible)
        {
            SetVisible(shouldBeVisible);
        }
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        // 控制渲染
        if (renderersToToggle != null)
        {
            foreach (var r in renderersToToggle)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }

        // 控制碰撞（不想玩家在看不见的时候也能通关，就一起关掉）
        if (exitCollider != null)
        {
            exitCollider.enabled = visible;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 只有在“已出现”的时候才允许触发
        if (!isVisible) return;

        if (other.CompareTag("Player") && mazeManager != null)
        {
            mazeManager.GenerateNewMaze();
        }
    }
}
