using System.Collections.Generic;
using UnityEngine;

public static class MazeGeneratorBranching
{
    public static MazeData Generate(MazeConfig config)
    {
        MazeData data = new MazeData(config.width, config.height);

        int width = config.width;
        int height = config.height;

        // 1. 主路径：和 Corridor 一样，从 (0,0) 到 (w-1,h-1)
        int x = 0;
        int y = 0;

        MazeCell start = data.GetCell(x, y);
        MazeCell current = start;

        List<MazeCell> mainPath = new List<MazeCell>();
        mainPath.Add(current);

        int targetX = width - 1;
        int targetY = height - 1;

        while (x < targetX || y < targetY)
        {
            bool canRight = x < targetX;
            bool canUp = y < targetY;

            int choice = 0;
            if (canRight && canUp)
            {
                choice = Random.Range(0, 2); // 0 -> right, 1 -> up
            }
            else if (canRight)
            {
                choice = 0;
            }
            else
            {
                choice = 1;
            }

            int nextX = x;
            int nextY = y;

            if (choice == 0)
                nextX++;
            else
                nextY++;

            MazeCell next = data.GetCell(nextX, nextY);
            RemoveWall(current, next);

            current = next;
            x = nextX;
            y = nextY;
            mainPath.Add(current);
        }

        data.startCell = start;
        data.endCell = current;

        // 2. 在主路径上加“关键岔路”（比 Corridor 的支路更长一些）
        int branchCount = Mathf.Max(2, (width + height) / 3);
        AddBranchingPaths(data, mainPath, branchCount);

        return data;
    }

    private static void AddBranchingPaths(MazeData data, List<MazeCell> mainPath, int branchCount)
    {
        if (mainPath.Count <= 4) return;

        int width = data.width;
        int height = data.height;

        HashSet<MazeCell> mainSet = new HashSet<MazeCell>(mainPath);

        for (int i = 0; i < branchCount; i++)
        {
            // 选一个“关键点”：避开两端，偏中间一点
            int minIdx = mainPath.Count / 4;
            int maxIdx = mainPath.Count * 3 / 4;
            int idx = Random.Range(minIdx, maxIdx);

            MazeCell root = mainPath[idx];
            MazeCell current = root;

            int branchLen = Random.Range(3, 7); // 支路更长，3~6 格

            for (int step = 0; step < branchLen; step++)
            {
                // 尝试四个方向
                List<(int dx, int dy)> dirs = new List<(int dx, int dy)>
                {
                    (0, 1),
                    (1, 0),
                    (0, -1),
                    (-1, 0)
                };

                // 打乱
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

                    // 不要直接往回穿回主路径的多处点，偶尔允许回到主路径形成环
                    bool nextIsMain = mainSet.Contains(next);

                    // 大部分时候避免马上回到主路（否则支路太短）
                    if (nextIsMain && step < branchLen - 2)
                        continue;

                    // 不要无限在已有通路里乱穿
                    if (!nextIsMain && !IsCellMostlyClosed(next))
                        continue;

                    RemoveWall(current, next);
                    current = next;
                    carved = true;

                    // 如果回到主路径，就提前结束这条支路（形成一个小环）
                    if (nextIsMain)
                    {
                        step = branchLen; // break 外层 for
                    }
                    break;
                }

                if (!carved)
                    break;
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
