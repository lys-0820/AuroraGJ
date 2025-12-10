using System.Collections.Generic;
using UnityEngine;

public static class MazeGeneratorSpiral
{
    public static MazeData Generate(MazeConfig config)
    {
        MazeData data = new MazeData(config.width, config.height);

        int width = config.width;
        int height = config.height;

        if (width <= 1 || height <= 1)
        {
            // 太小，就退化成简单直线
            return MazeGeneratorCorridor.Generate(config);
        }

        int left = 0;
        int right = width - 1;
        int bottom = 0;
        int top = height - 1;

        List<MazeCell> path = new List<MazeCell>();

        // 起点在左下角
        MazeCell current = data.GetCell(left, bottom);
        path.Add(current);

        while (left <= right && bottom <= top)
        {
            // 1. 向右走： (left, bottom) -> (right, bottom)
            for (int x = left + 1; x <= right; x++)
            {
                MazeCell next = data.GetCell(x, bottom);
                RemoveWall(current, next);
                current = next;
                path.Add(current);
            }
            bottom++;
            if (bottom > top) break;

            // 2. 向上走： (right, bottom) -> (right, top)
            for (int y = bottom; y <= top; y++)
            {
                MazeCell next = data.GetCell(right, y);
                RemoveWall(current, next);
                current = next;
                path.Add(current);
            }
            right--;
            if (right < left) break;

            // 3. 向左走： (right, top) -> (left, top)
            for (int x = right; x >= left; x--)
            {
                MazeCell next = data.GetCell(x, top);
                RemoveWall(current, next);
                current = next;
                path.Add(current);
            }
            top--;
            if (top < bottom) break;

            // 4. 向下走： (left, top) -> (left, bottom)
            for (int y = top; y >= bottom; y--)
            {
                MazeCell next = data.GetCell(left, y);
                RemoveWall(current, next);
                current = next;
                path.Add(current);
            }
            left++;
        }

        data.startCell = path[0];
        data.endCell = path[path.Count - 1];

        // 可选：在螺旋路径上加少量短支路
        AddShortBranches(data, path, branchCount: Mathf.Max(1, path.Count / 10));

        return data;
    }

    private static void AddShortBranches(MazeData data, List<MazeCell> mainPath, int branchCount)
    {
        if (mainPath.Count <= 2) return;

        HashSet<MazeCell> mainSet = new HashSet<MazeCell>(mainPath);

        for (int i = 0; i < branchCount; i++)
        {
            int idx = Random.Range(1, mainPath.Count - 1);
            MazeCell root = mainPath[idx];
            MazeCell current = root;

            int branchLen = Random.Range(1, 4);

            for (int step = 0; step < branchLen; step++)
            {
                List<(int dx, int dy)> dirs = new List<(int dx, int dy)>
                {
                    (0, 1),
                    (1, 0),
                    (0, -1),
                    (-1, 0)
                };

                // 打乱方向
                for (int k = 0; k < dirs.Count; k++)
                {
                    int r = Random.Range(k, dirs.Count);
                    var tmp = dirs[k];
                    dirs[k] = dirs[r];
                    dirs[r] = tmp;
                }

                bool carved = false;
                foreach (var (dx, dy) in dirs)
                {
                    int nx = current.x + dx;
                    int ny = current.y + dy;
                    MazeCell next = data.GetCell(nx, ny);
                    if (next == null) continue;
                    if (mainSet.Contains(next)) continue;

                    if (IsCellMostlyClosed(next))
                    {
                        RemoveWall(current, next);
                        current = next;
                        carved = true;
                        break;
                    }
                }

                if (!carved) break;
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
