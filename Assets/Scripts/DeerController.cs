using System.Collections.Generic;
using UnityEngine;

public class DeerController : MonoBehaviour
{
    public enum DeerState
    {
        Hidden,         // 不在场景中（等待下次出现条件）
        Idle,           // 出现，静止等待玩家注视
        RunAway,        // 正在跑走
        Finished        // 剧情结束，不再出现（但最后一个光晕仍然可以被踩掉）
    }

    [Header("路径 & 玩家")]
    public MainPath mainPath;          // 你的主路线组件（下挂一串 waypoints）
    public Transform player;           // 玩家或摄像机的 Transform

    [Header("行为设置")]
    public float appearRadius = 2f;        // 玩家到达“光晕”多近时触发事件
    public float lookMaxDistance = 20f;    // 注视判定的最大距离
    [Range(0f, 90f)]
    public float lookAngleThreshold = 30f; // 视线与鹿方向夹角阈值（度）
    public float lookDuration = 3f;        // 需要注视多久才会触发跑走（秒）

    public float runSpeed = 4f;        // 跑走速度
    public float runDistance = 3f;     // 从当前点朝下一点方向跑多远就消失

    [Header("贴地设置")]
    [Tooltip("从当前鹿位置向上抬起多少再往下射线检测地面")]
    public float groundCheckHeight = 5f;
    [Tooltip("鹿脚与地面之间的微小偏移")]
    public float deerHeightOffset = 0.05f;
    [Tooltip("地面所在的 LayerMask（勾选 Terrain / Ground 等）")]
    public LayerMask groundMask = ~0;  // 默认对所有碰撞体有效

    [Header("掉落光晕")]
    public GameObject haloPrefab;          // 光晕预制体（发光圈、粒子等）
    public float haloHeightOffset = 0.01f; // 光晕离地面微小偏移
    private GameObject currentHaloInstance;
    [Header("动画")]
    public Animator animator;              // 指向鹿的 Animator
    public string runBoolName = "IsRunning"; // Animator 中控制跑步的 Bool 参数名
    [Header("调试查看")]
    public DeerState currentState = DeerState.Hidden;
    public int currentIndex = 0;       // 正在处理的路径点索引（鹿出现的位置索引）
    [Header("可视隐藏")]
    public GameObject deerVisualRoot;   // 指向鹿的模型根节点（不是挂脚本的这个）

    private List<Transform> pathPoints;
    private Vector3 currentVanishPos;  // 当前段的消失位置（鹿跑到这里消失并掉光晕）
    private Vector3 lastVanishPos;     // 上一段的消失位置/光晕位置
    private bool   waitingForNextSpawn = false;
    private bool   finalSegmentPending = false; // 是否正在等待“最后一段”的光晕被踩掉
    private float  lookTimer = 0f;

    void Start()
    {
        if (mainPath == null)
        {
            Debug.LogError("DeerController: MainPath 未设置");
            enabled = false;
            return;
        }

        pathPoints = mainPath.waypoints;
        if (pathPoints == null || pathPoints.Count == 0)
        {
            Debug.LogError("DeerController: 路径点为空");
            enabled = false;
            return;
        }

        // 第一个点：一开始就出现
        currentIndex = 0;
        SpawnAtCurrentPoint(firstSpawn: true);
    }

    void Update()
    {
        Debug.Log( $"DeerController: 当前状态 = {currentState}, 当前索引 = {currentIndex}" );
        switch (currentState)
        {
            
            case DeerState.Hidden:
                UpdateHidden();
                break;

            case DeerState.Idle:
                UpdateIdle();
                break;

            case DeerState.RunAway:
                UpdateRunAway();
                break;

            case DeerState.Finished:
                // 剧情结束，但可能还有最后一个光晕在地上
                UpdateFinishedHalo();
                break;
        }
    }

    void SetDeerVisible(bool visible)
    {
        if (deerVisualRoot != null)
            deerVisualRoot.SetActive(visible);
    }   
    void SetRunAnimation(bool running)
    {
    if (animator != null)
    {
        animator.SetBool(runBoolName, running);
    }
    }
    #region 状态更新

    // 隐藏：等待玩家走到上一段消失点 / 光晕位置，再在下一个点出现
    void UpdateHidden()
    {
        if (!waitingForNextSpawn)
            return;

        if (player == null) return;
        Vector3 a = player.position;
        Vector3 b = lastVanishPos;
        a.y = b.y = 0f;                  
        float dist = Vector3.Distance(a, b);
        Debug.Log($"DeerController: 玩家与光晕距离 = {dist}");
        if (dist <= appearRadius)
        {
            // 玩家踩到光晕
            RemoveHalo();

            waitingForNextSpawn = false;

            // 如果是最后一段，就只删除光晕，不再出现鹿
            if (finalSegmentPending)
            {
                finalSegmentPending = false;
                currentState = DeerState.Finished;
                return;
            }

            // 否则在下一个点出现
            SpawnAtCurrentPoint(firstSpawn: false);
        }
    }

    // Idle：鹿静止，检测玩家是否注视超过 lookDuration 秒
    void UpdateIdle()
    {
        if (player == null) return;

        Vector3 toDeer = transform.position - player.position;
        toDeer.y = 0f;
        float distance = toDeer.magnitude;

        if (distance > lookMaxDistance)
        {
            lookTimer = 0f;
            return;
        }

        if (distance < 0.001f)
        {
            // 太近了，不好算角度，当作没看
            lookTimer = 0f;
            return;
        }

        toDeer.Normalize();
        Vector3 playerForward = player.forward;
        playerForward.y = 0f;
        playerForward.Normalize();

        float angle = Vector3.Angle(playerForward, toDeer);

        // 是否在“面对鹿”的视角锥内
        if (angle <= lookAngleThreshold)
        {
            lookTimer += Time.deltaTime;

            if (lookTimer >= lookDuration)
            {
                // 开始跑走
                StartRunAway();
            }
        }
        else
        {
            lookTimer = 0f;
        }
    }

    // RunAway：朝 currentVanishPos 跑，到了就消失并掉落光晕
    void UpdateRunAway()
    {
        Vector3 pos = transform.position;
        Vector3 target = currentVanishPos;
        Vector3 dir = (target - pos);
        dir.y = 0f;

        float dist = dir.magnitude;
        if (dist < 0.05f)
        {
            // 到达消失点
            OnRunAwayFinished();
            return;
        }

        dir.Normalize();
        transform.position += dir * runSpeed * Time.deltaTime;
        SnapToGround();  // 跑的过程中始终贴地

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            10f * Time.deltaTime
        );
    }

    // Finished 状态时，可能还保留最后一个光晕，需要检测玩家踩掉
    void UpdateFinishedHalo()
    {
        if (currentHaloInstance == null) return;
        if (player == null) return;
        Vector3 a = player.position;
        Vector3 b = lastVanishPos;
        a.y = b.y = 0f;
        float dist = Vector3.Distance(a, b);
        if (dist <= appearRadius)
        {
            RemoveHalo();
        }
    }

    #endregion

    #region 行为方法

    // 在当前路径点出现
    void SpawnAtCurrentPoint(bool firstSpawn)
    {
        if (currentIndex < 0 || currentIndex >= pathPoints.Count)
        {
            currentState = DeerState.Finished;
            SetDeerVisible(false);
            return;
        }

        Transform point = pathPoints[currentIndex];
        transform.position = point.position;
        SnapToGround();  // 出现时先贴地
        transform.rotation = Quaternion.identity;

        SetDeerVisible(true);
        currentState = DeerState.Idle;
        // 出现时切回静止动画
        SetRunAnimation(false);

        // 出现时面向玩家（如果有玩家）
        if (player != null)
        {
            Vector3 toPlayer = transform.position - player.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
        }

        lookTimer = 0f;
        waitingForNextSpawn = false;
        finalSegmentPending = false;
        currentState = DeerState.Idle;

        // 计算这一段的消失位置（朝下一点方向跑 runDistance）
        ComputeCurrentVanishPos();
    }

    // 计算 currentVanishPos：从当前点朝下一个点方向 runDistance
    void ComputeCurrentVanishPos()
    {
        // 最后一个点：没有“下一点”
        if (currentIndex >= pathPoints.Count - 1)
        {
            // 简单处理：沿着当前 forward 跑一小段
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            currentVanishPos = transform.position + forward * runDistance;
        }
        else
        {
            Transform cur = pathPoints[currentIndex];
            Transform next = pathPoints[currentIndex + 1];

            Vector3 dir = next.position - cur.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.forward;
            dir.Normalize();

            currentVanishPos = cur.position + dir * runDistance;
        }

        // 让消失点也贴地（防止高度不对）
        currentVanishPos = SnapPointToGround(currentVanishPos);
    }

    // 从 Idle 切到 RunAway
    void StartRunAway()
    {
        currentState = DeerState.RunAway;
        lookTimer = 0f;
        // 切到跑步动画
        SetRunAnimation(true);
    }

    // 跑到消失点 → 掉落光晕 → 等待玩家踩掉
    void OnRunAwayFinished()
    {
        lastVanishPos = currentVanishPos;

        // 在消失点生成光晕
        SpawnHaloAt(lastVanishPos);

        SetDeerVisible(false);
        // 下次再出现时从静止开始
        SetRunAnimation(false);

        // 如果已经是最后一个路径点：只剩最后一个光晕，不再出现鹿
        if (currentIndex >= pathPoints.Count - 1)
        {
            waitingForNextSpawn = false;
            finalSegmentPending = true;   // 只等玩家来踩掉最后一个光晕
            currentState = DeerState.Finished;
            return;
        }

        // 非最后一个点：等玩家踩光晕，再在下一个点出现
        currentIndex++;
        waitingForNextSpawn = true;
        currentState = DeerState.Hidden;
    }

    #endregion

    #region 光晕相关

    void SpawnHaloAt(Vector3 worldPos)
    {
        if (haloPrefab == null) return;

        Vector3 p = SnapPointToGround(worldPos);
        p.y += haloHeightOffset;

        // 先清掉旧的
        RemoveHalo();

        currentHaloInstance = Instantiate(haloPrefab, p, Quaternion.identity);
    }

    void RemoveHalo()
    {
        if (currentHaloInstance != null)
        {
            Destroy(currentHaloInstance);
            currentHaloInstance = null;
        }
    }

    #endregion

    #region 贴地相关

    void SnapToGround()
    {
        Vector3 pos = transform.position;
        pos = SnapPointToGround(pos);
        transform.position = pos;
    }

    Vector3 SnapPointToGround(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * groundCheckHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckHeight * 2f, groundMask))
        {
            pos.y = hit.point.y + deerHeightOffset;
        }

        return pos;
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        // 可视化消失点
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(currentVanishPos, 0.2f);

        // 可视化光晕/下一次出现判定范围
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(lastVanishPos, appearRadius);
    }
}
