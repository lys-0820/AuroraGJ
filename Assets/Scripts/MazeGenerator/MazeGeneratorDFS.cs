using System.Collections.Generic;
using UnityEngine;

public static class MazeGeneratorDFS
{
    // 主入口：给一个 config，返回 MazeData
    public static MazeData Generate(MazeConfig config)
    {
        MazeData data = new MazeData(config.width, config.height);

        // 1. 从随机起点开始
        int startX = Random.Range(0, config.width);
        int startY = Random.Range(0, config.height);
        MazeCell startCell = data.GetCell(startX, startY);

        DepthFirstCarve(data, startCell);

        // 2. 设置起点终点（简单：起点 = 左下角，终点 = 右上角）
        data.startCell = data.GetCell(0, 0);
        data.endCell = data.GetCell(config.width - 1, config.height - 1);

        return data;
    }

    // 用 stack 做非递归 DFS，避免递归过深
    private static void DepthFirstCarve(MazeData data, MazeCell start)
    {
        Stack<MazeCell> stack = new Stack<MazeCell>();
        start.visited = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            MazeCell current = stack.Peek();

            // 找当前 cell 没访问过的邻居
            List<(MazeCell cell, int dir)> neighbors = GetUnvisitedNeighbors(data, current);

            if (neighbors.Count == 0)
            {
                // 没有邻居了，回退
                stack.Pop();
            }
            else
            {
                // 随机选一个邻居
                var (nextCell, dir) = neighbors[Random.Range(0, neighbors.Count)];

                // 打通 current 与 next 之间的墙
                RemoveWall(current, nextCell, dir);

                nextCell.visited = true;
                stack.Push(nextCell);
            }
        }
    }

    // 获取未访问的邻居，返回 cell + 它相对 current 的方向
    private static List<(MazeCell, int)> GetUnvisitedNeighbors(MazeData data, MazeCell cell)
    {
        List<(MazeCell, int)> result = new List<(MazeCell, int)>();

        // 0 上（y+1）
        MazeCell up = data.GetCell(cell.x, cell.y + 1);
        if (up != null && !up.visited) result.Add((up, 0));

        // 1 右（x+1）
        MazeCell right = data.GetCell(cell.x + 1, cell.y);
        if (right != null && !right.visited) result.Add((right, 1));

        // 2 下（y-1）
        MazeCell down = data.GetCell(cell.x, cell.y - 1);
        if (down != null && !down.visited) result.Add((down, 2));

        // 3 左（x-1）
        MazeCell left = data.GetCell(cell.x - 1, cell.y);
        if (left != null && !left.visited) result.Add((left, 3));

        return result;
    }

    // dir 是相对于 current 的方向（0 上，1 右，2 下，3 左）
    private static void RemoveWall(MazeCell current, MazeCell next, int dir)
    {
        current.walls[dir] = false;

        int oppositeDir = (dir + 2) % 4;
        next.walls[oppositeDir] = false;
    }
}
