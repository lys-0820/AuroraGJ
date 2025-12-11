using UnityEngine;

public class TrailParticles : MonoBehaviour
{
    [Header("轨迹粒子")]
    public GameObject trailPrefab;     // 球形光点粒子 prefab
    public float spawnDistance = 0.8f; // 每隔多远生成一个（根据场景调）
    public float lifeTime = 3f;        // 光点存在多久后消失
    public float yOffset = 0.05f;      // 稍微抬高一点，避免埋进地里
    public float backwardOffsetValue = 1.3f; // 0.5f 是向后距离，可调
    private Vector3 lastSpawnPos;
    private bool hasSpawnedOnce = false;

    void Update()
    {
        if (trailPrefab == null) return;

        Vector3 current = transform.position;

        // 第一次直接生成一个
        if (!hasSpawnedOnce)
        {
            SpawnTrailAt(current);
            hasSpawnedOnce = true;
            return;
        }

        // 只看水平距离，避免高度变化影响
        Vector2 currentXZ = new Vector2(current.x, current.z);
        Vector2 lastXZ = new Vector2(lastSpawnPos.x, lastSpawnPos.z);
        float dist = Vector2.Distance(currentXZ, lastXZ);

        if (dist >= spawnDistance)
        {
            SpawnTrailAt(current);
        }
    }

    void SpawnTrailAt(Vector3 worldPos)
    {
    Vector3 backwardOffset = -transform.forward * backwardOffsetValue;
    Vector3 origin = worldPos + Vector3.up * 2f; // 射线起点稍微高一点
    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f))
    {
        worldPos = hit.point;
    }

    Vector3 spawnPos = worldPos + backwardOffset + Vector3.up * yOffset;
    GameObject go = Instantiate(trailPrefab, spawnPos, Quaternion.identity);

    if (lifeTime > 0f)
        Destroy(go, lifeTime);

    lastSpawnPos = worldPos;
    }

}
