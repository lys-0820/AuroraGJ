using System.Collections.Generic;
using UnityEngine;

public static class MazeGeneratorCorridor
{
    public static MazeData Generate(MazeConfig config)
    {
        MazeData data = new MazeData(config.width, config.height);

        int width = config.width;
        int height = config.height;

        // 主路径：从 (0,0) 走到 (width-1, height-1)，只向右或向上
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
                // 随机决定先右还是先上
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
        data.endCell = current;  // 终点在右上角

        // 可选：在主路径上加一些短支路，让迷宫不会太无聊
        AddShortBranches(data, mainPath, branchCount: Mathf.Max(1, (width + height) / 4));

        return data;
    }

    /// <summary>
    /// 在主路径上的随机若干格子，生成长度 1~3 的短支路（死胡同）
    /// </summary>
    private static void AddShortBranches(MazeData data, List<MazeCell> mainPath, int branchCount)
    {
        if (mainPath.Count <= 2) return;

        int width = data.width;
        int height = data.height;

        HashSet<MazeCell> mainSet = new HashSet<MazeCell>(mainPath);

        for (int i = 0; i < branchCount; i++)
        {
            // 不在起点 / 终点生成分支
            int idx = Random.Range(1, mainPath.Count - 1);
            MazeCell root = mainPath[idx];

            MazeCell current = root;
            int branchLen = Random.Range(1, 4); // 1~3 格

            for (int step = 0; step < branchLen; step++)
            {
                // 随机方向尝试
                List<(int dx, int dy)> dirs = new List<(int dx, int dy)>
                {
                    (0, 1),   // up
                    (1, 0),   // right
                    (0, -1),  // down
                    (-1, 0)   // left
                };
                // 打乱一下顺序
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
                    if (mainSet.Contains(next)) continue; // 不要打通回主路径

                    // 如果这格子四周墙基本都还在（“未使用”），就拿来当支路
                    if (IsCellMostlyClosed(next))
                    {
                        RemoveWall(current, next);
                        current = next;
                        carved = true;
                        break;
                    }
                }

                if (!carved)
                {
                    // 该步找不到合适方向，就结束这条支路
                    break;
                }
            }
        }
    }

    private static bool IsCellMostlyClosed(MazeCell cell)
    {
        // 简单判断：大部分墙都是关着的
        int closed = 0;
        for (int i = 0; i < 4; i++)
        {
            if (cell.walls[i]) closed++;
        }
        return closed >= 3;
    }

    /// <summary>
    /// 根据相邻两格子的位置，打通墙（0上1右2下3左）
    /// </summary>
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
