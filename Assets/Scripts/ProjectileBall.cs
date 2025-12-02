using UnityEngine;

public class ProjectileBall : MonoBehaviour
{
    [Header("Prefab设置")]
    [SerializeField] private GameObject projectilePrefab; // 要发射的Prefab（发射器模式需要设置）
    
    [Header("发射设置")]
    [SerializeField] private float speed = 10f;       // 发射速度
    [SerializeField] private float returnSpeed = 10f; // 返回速度
    [SerializeField] private float maxLifetime = 5f;  // 最大生存时间（秒）
    
    [Header("发光设置")]
    [SerializeField] private Color emissionColor = Color.white; // 发光颜色
    [SerializeField] private float emissionIntensity = 2f;      // 发光强度
    
    // 公开属性，允许外部访问和修改
    public float Speed { get => speed; set => speed = value; }
    public float ReturnSpeed { get => returnSpeed; set => returnSpeed = value; }
    
    [Header("触发设置")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q; // 触发按键（仅发射器模式使用）
    
    // 球体模式使用的变量
    private Vector3 originalPosition;   // 原始位置（发射位置）
    private Vector3 forwardDirection;   // 发射方向
    private bool isLaunched = false;    // 是否已经发射
    private bool isReturning = false;   // 是否正在返回
    private Rigidbody rb;
    private float launchTime = 0f;      // 发射时间
    private Material material;          // 材质引用
    private Renderer renderer;          // 渲染器引用

    void Start()
    {
        // 如果设置了prefab，则是发射器模式
        if (projectilePrefab != null)
        {
            // 发射器模式：不需要初始化球体相关组件
            return;
        }
        
        // 球体模式：初始化球体相关组件和逻辑
        InitializeProjectile();
    }
    
    void Update()
    {
        // 发射器模式：只处理按键和实例化
        if (projectilePrefab != null)
        {
            // 如果按下触发键，则实例化prefab并发射
            if (Input.GetKeyDown(triggerKey))
            {
                SpawnAndLaunchProjectile();
            }
            return;
        }
        
        // 球体模式：处理移动、碰撞和销毁逻辑
        if (isLaunched)
        {
            // 检查是否超过最大生存时间（仅在未返回时检查）
            if (!isReturning && Time.time - launchTime >= maxLifetime)
            {
                // 超过5秒未碰到障碍物，销毁物体
                Destroy(gameObject);
                return;
            }

            if (isReturning)
            {
                // 返回模式：向原始位置移动
                Vector3 directionToOrigin = (originalPosition - transform.position).normalized;
                rb.velocity = directionToOrigin * returnSpeed;

                // 检查是否回到原始位置
                float distanceToOrigin = Vector3.Distance(transform.position, originalPosition);
                if (distanceToOrigin < 0.1f)
                {
                    // 回到原始位置，销毁物体
                    Destroy(gameObject);
                }
            }
            else
            {
                // 发射模式：保持向前移动
                rb.velocity = forwardDirection * speed;
            }
        }
    }
    
    /// <summary>
    /// 初始化球体（球体模式使用）
    /// </summary>
    private void InitializeProjectile()
    {
        // 保存原始位置（发射位置）
        originalPosition = transform.position;
        
        // 获取或添加 Rigidbody 组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 设置 Rigidbody 属性
        rb.useGravity = false; // 禁用重力，让物体直线飞行
        rb.isKinematic = false;
        
        // 设置初始方向为物体的 forward 方向
        forwardDirection = transform.forward;
        
        // 初始化发光效果
        InitializeEmission();
    }
    
    /// <summary>
    /// 实例化prefab并发射（发射器模式使用）
    /// </summary>
    private void SpawnAndLaunchProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("ProjectileBall: Prefab未设置！");
            return;
        }
        
        // 实例化prefab
        GameObject projectile = Instantiate(projectilePrefab, transform.position, transform.rotation);
        
        // 获取ProjectileBall组件（prefab上必须有这个脚本）
        ProjectileBall projectileScript = projectile.GetComponent<ProjectileBall>();
        if (projectileScript == null)
        {
            Debug.LogWarning("ProjectileBall: Prefab上未找到ProjectileBall组件！");
            return;
        }
        
        // 传递发射参数
        projectileScript.Speed = this.speed;
        projectileScript.ReturnSpeed = this.returnSpeed;
        projectileScript.maxLifetime = this.maxLifetime;
        
        // 发射新实例化的物体
        projectileScript.Launch();
    }
    
    /// <summary>
    /// 发射球体（球体模式使用）
    /// </summary>
    public void Launch()
    {
        if (isLaunched) return;
        
        // 确保有Rigidbody组件
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.isKinematic = false;
        }
        
        isLaunched = true;
        isReturning = false;
        originalPosition = transform.position; // 记录发射位置
        forwardDirection = transform.forward;
        launchTime = Time.time; // 记录发射时间
        
        // 设置初始速度
        rb.velocity = forwardDirection * speed;
    }
    
    /// <summary>
    /// 开始返回（球体模式使用）
    /// </summary>
    private void StartReturn()
    {
        if (isReturning) return;
        
        isReturning = true;
        // 停止当前速度，准备返回
        rb.velocity = Vector3.zero;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 只在球体模式下处理碰撞
        if (projectilePrefab != null) return;
        
        // 如果碰到障碍物（非发射器），开始返回
        if (isLaunched && !isReturning)
        {
            // 检查是否击中可阻挡物体（可根据 tag 或 layer 来判断）
            // 这里假设所有非自身的碰撞体都是障碍物
            if (collision.gameObject != gameObject)
            {
                StartReturn();
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 只在球体模式下处理触发器
        if (projectilePrefab != null) return;
        
        // 如果使用触发器检测障碍物
        if (isLaunched && !isReturning)
        {
            // 检查是否击中障碍物（可根据 tag 判断）
            if (other.gameObject != gameObject && other.CompareTag("Obstacle"))
            {
                StartReturn();
            }
        }
    }
    
    
    /// <summary>
    /// 初始化自发光效果
    /// </summary>
    private void InitializeEmission()
    {
        // 获取渲染器组件
        renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // 创建材质实例（避免修改原始材质）
            material = renderer.material;
            
            // 启用Emission
            material.EnableKeyword("_EMISSION");
            
            // 设置发光颜色和强度
            SetEmission(emissionColor, emissionIntensity);
        }
    }
    
    /// <summary>
    /// 设置自发光效果
    /// </summary>
    private void SetEmission(Color color, float intensity)
    {
        if (material != null)
        {
            // 将颜色转换为HDR颜色（乘以强度）
            Color finalColor = color * intensity;
            material.SetColor("_EmissionColor", finalColor);
            
            // 对于Standard Shader，还需要设置全局光照标志
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }
    
    /// <summary>
    /// 动态改变发光强度（可选，用于特效）
    /// </summary>
    public void SetEmissionIntensity(float intensity)
    {
        emissionIntensity = intensity;
        SetEmission(emissionColor, emissionIntensity);
    }
    
    /// <summary>
    /// 动态改变发光颜色（可选，用于特效）
    /// </summary>
    public void SetEmissionColor(Color color)
    {
        emissionColor = color;
        SetEmission(emissionColor, emissionIntensity);
    }
}
