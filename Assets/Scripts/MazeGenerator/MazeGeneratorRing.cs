using System.Collections.Generic;
using UnityEngine;

public static class MazeGeneratorRing
{
    public static MazeData Generate(MazeConfig config)
    {
        MazeData data = new MazeData(config.width, config.height);

        int width = config.width;
        int height = config.height;

        // 收集边界上的一圈格子，按顺序排列成 ring
        List<MazeCell> ring = new List<MazeCell>();

        if (width <= 1 || height <= 1)
        {
            // 太小，就退化成简单直线
            return MazeGeneratorCorridor.Generate(config);
        }

        // 下边一行：从左到右 (x, 0)
        for (int x = 0; x < width; x++)
        {
            ring.Add(data.GetCell(x, 0));
        }

        // 右边一列：从下到上 (width-1, y)（从 1 开始，避免重复右下角）
        for (int y = 1; y < height; y++)
        {
            ring.Add(data.GetCell(width - 1, y));
        }

        // 上边一行：从右到左 (x, height-1)（从 width-2 开始，避免重复右上角）
        for (int x = width - 2; x >= 0; x--)
        {
            ring.Add(data.GetCell(x, height - 1));
        }

        // 左边一列：从上到下 (0, y)（从 height-2 到 1，避免重复两个角）
        for (int y = height - 2; y >= 1; y--)
        {
            ring.Add(data.GetCell(0, y));
        }

        // 按顺序把 ring 中的格子连成一个闭合环
        for (int i = 0; i < ring.Count; i++)
        {
            MazeCell a = ring[i];
            MazeCell b = ring[(i + 1) % ring.Count];
            RemoveWall(a, b);
        }

        // 起点在 (0,0)（下边左角）
        data.startCell = ring[0];

        // 终点在环的对面位置
        int endIndex = ring.Count / 2;
        data.endCell = ring[endIndex];

        // 可选：加一点从环向内的短支路
        AddRadialBranches(data, ring, branchCount: Mathf.Max(1, ring.Count / 8));

        return data;
    }

    private static void AddRadialBranches(MazeData data, List<MazeCell> ring, int branchCount)
    {
        int width = data.width;
        int height = data.height;

        for (int i = 0; i < branchCount; i++)
        {
            MazeCell root = ring[Random.Range(0, ring.Count)];
            MazeCell current = root;

            int maxSteps = Random.Range(1, 4); // 1~3 格

            for (int step = 0; step < maxSteps; step++)
            {
                // 朝着地图中心大致方向前进
                int cx = current.x;
                int cy = current.y;

                int centerX = width / 2;
                int centerY = height / 2;

                List<(int dx, int dy)> dirs = new List<(int dx, int dy)>();

                if (centerX > cx) dirs.Add((1, 0));
                if (centerX < cx) dirs.Add((-1, 0));
                if (centerY > cy) dirs.Add((0, 1));
                if (centerY < cy) dirs.Add((0, -1));

                if (dirs.Count == 0) break;

                // 随机选一个
                var choice = dirs[Random.Range(0, dirs.Count)];

                int nx = cx + choice.dx;
                int ny = cy + choice.dy;

                MazeCell next = data.GetCell(nx, ny);
                if (next == null) break;

                // 如果这格子几乎没被打开过，就 carve
                if (IsCellMostlyClosed(next))
                {
                    RemoveWall(current, next);
                    current = next;
                }
                else
                {
                    break;
                }
            }
        }
    }

    private static bool IsCellMostlyClosed(MazeCell cell)
    {
        int closed = 0;
        for (int i = 0; i < 4; i++)
        {
            if (cell.walls[i]) closed++;
        }
        return closed >= 3;
    }

    private static void RemoveWall(MazeCell a, MazeCell b)
    {
        int dx = b.x - a.x;
        int dy = b.y - a.y;

        int dirFromA = -1;

        if (dx == 1 && dy == 0) dirFromA = 1;      // 右
        else if (dx == -1 && dy == 0) dirFromA = 3; // 左
        else if (dx == 0 && dy == 1) dirFromA = 0;  // 上
        else if (dx == 0 && dy == -1) dirFromA = 2; // 下

        if (dirFromA < 0) return;

        a.walls[dirFromA] = false;
        int opposite = (dirFromA + 2) % 4;
        b.walls[opposite] = false;
    }
}
