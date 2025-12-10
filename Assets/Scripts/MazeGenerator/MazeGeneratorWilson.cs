using System.Collections.Generic;
using UnityEngine;

public static class MazeGeneratorWilson
{
    public static MazeData Generate(MazeConfig config)
    {
        MazeData data = new MazeData(config.width, config.height);

        int width = config.width;
        int height = config.height;

        // 记录已经在迷宫中的 cell
        HashSet<MazeCell> inMaze = new HashSet<MazeCell>();

        // 1. 随机选一个起点，直接加入迷宫
        int startX = Random.Range(0, width);
        int startY = Random.Range(0, height);
        MazeCell startCell = data.GetCell(startX, startY);
        inMaze.Add(startCell);

        int totalCells = width * height;

        // 2. 直到所有格子都加入
        while (inMaze.Count < totalCells)
        {
            // 2.1 随机选一个尚未在迷宫里的格子作为游走起点
            MazeCell walkStart = GetRandomCellNotInMaze(data, inMaze);

            // 2.2 loop-erased random walk
            List<MazeCell> path = new List<MazeCell>();
            Dictionary<MazeCell, int> pathIndex = new Dictionary<MazeCell, int>();

            MazeCell current = walkStart;
            path.Add(current);
            pathIndex[current] = 0;

            while (!inMaze.Contains(current))
            {
                MazeCell next = GetRandomNeighbor(data, current);

                if (pathIndex.TryGetValue(next, out int idx))
                {
                    // 出现 loop，擦掉 idx 后面的部分
                    for (int i = path.Count - 1; i > idx; i--)
                    {
                        pathIndex.Remove(path[i]);
                        path.RemoveAt(i);
                    }
                    // 当前就等于 loop 里那个老节点
                    current = next;
                }
                else
                {
                    // 正常前进
                    path.Add(next);
                    pathIndex[next] = path.Count - 1;
                    current = next;
                }
            }

            // 2.3 当前 path 的最后一个节点已经在迷宫里
            // 把整条 path carve 进迷宫
            for (int i = 0; i < path.Count - 1; i++)
            {
                MazeCell a = path[i];
                MazeCell b = path[i + 1];
                RemoveWallBetween(a, b);
                inMaze.Add(a);
            }
            // 不要忘记最后一个
            inMaze.Add(path[path.Count - 1]);
        }

        data.startCell = startCell;
        data.endCell = FindFarthestCellByPathLength(data, startCell);

        return data;
    }

    /// <summary>
    /// 从还没在迷宫里的格子中随机选一个
    /// </summary>
    private static MazeCell GetRandomCellNotInMaze(MazeData data, HashSet<MazeCell> inMaze)
    {
        int width = data.width;
        int height = data.height;

        while (true)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);
            MazeCell cell = data.GetCell(x, y);
            if (!inMaze.Contains(cell))
                return cell;
        }
    }

    /// <summary>
    /// 从当前 cell 的四个方向中随机选一个存在的邻居
    /// </summary>
    private static MazeCell GetRandomNeighbor(MazeData data, MazeCell cell)
    {
        List<MazeCell> neighbors = new List<MazeCell>();

        int[][] dirs = new int[][]
        {
            new int[]{ 0, 1 },   // 上
            new int[]{ 1, 0 },   // 右
            new int[]{ 0, -1 },  // 下
            new int[]{ -1, 0 }   // 左
        };

        foreach (var d in dirs)
        {
            int nx = cell.x + d[0];
            int ny = cell.y + d[1];
            MazeCell n = data.GetCell(nx, ny);
            if (n != null)
                neighbors.Add(n);
        }

        if (neighbors.Count == 0)
        {
            // 理论上不会发生，因为在网格内
            return cell;
        }

        return neighbors[Random.Range(0, neighbors.Count)];
    }

    /// <summary>
    /// 打通两格之间的墙
    /// </summary>
    private static void RemoveWallBetween(MazeCell a, MazeCell b)
    {
        int dx = b.x - a.x;
        int dy = b.y - a.y;

        int dirFromA = -1;

        if (dx == 1 && dy == 0) dirFromA = 1;        // 右
        else if (dx == -1 && dy == 0) dirFromA = 3;  // 左
        else if (dx == 0 && dy == 1) dirFromA = 0;   // 上
        else if (dx == 0 && dy == -1) dirFromA = 2;  // 下

        if (dirFromA < 0) return;

        a.walls[dirFromA] = false;
        int opposite = (dirFromA + 2) % 4;
        b.walls[opposite] = false;
    }

    /// <summary>
    /// 同 Prim 版里那套 BFS，找距离 start 最远的格子
    /// </summary>
    private static MazeCell FindFarthestCellByPathLength(MazeData data, MazeCell start)
    {
        int width = data.width;
        int height = data.height;

        int[,] dist = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                dist[x, y] = -1;
            }
        }

        Queue<MazeCell> q = new Queue<MazeCell>();
        dist[start.x, start.y] = 0;
        q.Enqueue(start);

        MazeCell farthest = start;

        while (q.Count > 0)
        {
            MazeCell c = q.Dequeue();
            int cd = dist[c.x, c.y];

            if (cd > dist[farthest.x, farthest.y])
                farthest = c;

            foreach (MazeCell n in GetOpenNeighbors(data, c))
            {
                if (dist[n.x, n.y] < 0)
                {
                    dist[n.x, n.y] = cd + 1;
                    q.Enqueue(n);
                }
            }
        }

        return farthest;
    }

    private static List<MazeCell> GetOpenNeighbors(MazeData data, MazeCell cell)
    {
        List<MazeCell> result = new List<MazeCell>();

        int[][] dirs = new int[][]
        {
            new int[]{ 0, 1 },   // 上
            new int[]{ 1, 0 },   // 右
            new int[]{ 0, -1 },  // 下
            new int[]{ -1, 0 }   // 左
        };

        for (int dir = 0; dir < 4; dir++)
        {
            if (!cell.walls[dir])
            {
                int nx = cell.x + dirs[dir][0];
                int ny = cell.y + dirs[dir][1];
                MazeCell n = data.GetCell(nx, ny);
                if (n != null)
                    result.Add(n);
            }
        }

        return result;
    }
}
