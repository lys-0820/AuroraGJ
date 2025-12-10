using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 继承自 MainPath，但不依赖手动摆子物体，
/// 而是根据迷宫生成完后的 cell 路径在运行时生成子物体作为 waypoint。
/// </summary>
public class RuntimeMainPath : MainPath
{
    /// <summary>
    /// 用迷宫中的 cell 路径构建一条主路线。
    /// </summary>
    public void BuildFromCellPath(List<MazeCell> cellPath, MazeBuilder builder)
    {
        // 清空旧的子物体
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
        }

        // 清空原有 waypoint 列表
        waypoints.Clear();

        if (cellPath == null || cellPath.Count == 0)
            return;

        if (builder == null)
        {
            Debug.LogError("RuntimeMainPath.BuildFromCellPath: MazeBuilder 为 null");
            return;
        }

        // 根据 cell → 世界坐标，生成一串空物体
        foreach (var cell in cellPath)
        {
            Vector3 pos = builder.GetWorldPosOfCell(cell);

            GameObject wp = new GameObject($"WP_{cell.x}_{cell.y}");
            wp.transform.SetParent(transform);
            wp.transform.position = pos;

            waypoints.Add(wp.transform);
        }
    }
}
