using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从 MazeData 中“解迷宫”，得到一条从 startCell 到 endCell 的路径，
/// 并提供一个简化成“拐角点”的函数给鹿用。
/// </summary>
public static class MazeSolver
{
    /// <summary>
    /// 用 BFS 在格子图上找一条从 startCell 到 endCell 的合法路径。
    /// 路径里的元素是 MazeCell。
    /// </summary>
    public static List<MazeCell> FindPath(MazeData data)
    {
        if (data == null)
        {
            Debug.LogError("MazeSolver.FindPath: data 为 null");
            return null;
        }

        if (data.startCell == null || data.endCell == null)
        {
            Debug.LogError("MazeSolver.FindPath: startCell 或 endCell 为 null");
            return null;
        }

        MazeCell start = data.startCell;
        MazeCell goal = data.endCell;

        var queue = new Queue<MazeCell>();
        var cameFrom = new Dictionary<MazeCell, MazeCell>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal)
                break;

            foreach (var neighbor in GetNeighborsThroughOpenWalls(data, current))
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            Debug.LogWarning("MazeSolver.FindPath: 没有找到从起点到终点的路径。");
            return null;
        }

        // 回溯路径
        var path = new List<MazeCell>();
        var temp = goal;
        while (temp != null)
        {
            path.Add(temp);
            temp = cameFrom[temp];
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 只保留“转弯点”：起点 + 每个拐角 + 终点。
    /// 鹿的必经点就用这个简化后的路径。
    /// </summary>
    public static List<MazeCell> ExtractCorners(List<MazeCell> fullPath)
    {
        if (fullPath == null)
            return null;
        if (fullPath.Count <= 2)
            return new List<MazeCell>(fullPath);

        var result = new List<MazeCell>();
        result.Add(fullPath[0]); // 起点

        int prevDx = fullPath[1].x - fullPath[0].x;
        int prevDy = fullPath[1].y - fullPath[0].y;

        for (int i = 1; i < fullPath.Count - 1; i++)
        {
            int dx = fullPath[i + 1].x - fullPath[i].x;
            int dy = fullPath[i + 1].y - fullPath[i].y;

            // 方向变了 → 拐角
            if (dx != prevDx || dy != prevDy)
            {
                result.Add(fullPath[i]);
            }

            prevDx = dx;
            prevDy = dy;
        }

        result.Add(fullPath[fullPath.Count - 1]); // 终点
        return result;
    }

    /// <summary>
    /// 通过“没有墙”的方向找相邻格子。
    /// 0 上 (y+1), 1 右 (x+1), 2 下 (y-1), 3 左 (x-1)
    /// </summary>
    private static IEnumerable<MazeCell> GetNeighborsThroughOpenWalls(MazeData data, MazeCell cell)
    {
        // 0: up (y+1)
        if (!cell.walls[0])
        {
            var up = data.GetCell(cell.x, cell.y + 1);
            if (up != null) yield return up;
        }

        // 1: right (x+1)
        if (!cell.walls[1])
        {
            var right = data.GetCell(cell.x + 1, cell.y);
            if (right != null) yield return right;
        }

        // 2: down (y-1)
        if (!cell.walls[2])
        {
            var down = data.GetCell(cell.x, cell.y - 1);
            if (down != null) yield return down;
        }

        // 3: left (x-1)
        if (!cell.walls[3])
        {
            var left = data.GetCell(cell.x - 1, cell.y);
            if (left != null) yield return left;
        }
    }
}
