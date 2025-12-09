using System.Collections.Generic;
using UnityEngine;

public enum MazeType
{
    Grid
}

public class MazeConfig
{
    public MazeType type = MazeType.Grid;
    public int width = 10;
    public int height = 10;
}

public class MazeCell
{
    // 网格坐标
    public int x;
    public int y;

    // 是否访问过（生成算法用）
    public bool visited;

    // 四面墙：0 上 / 北，1 右 / 东，2 下 / 南，3 左 / 西
    public bool[] walls = new bool[4] { true, true, true, true };

    public MazeCell(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class MazeData
{
    public int width;
    public int height;
    public MazeCell[,] cells;

    public MazeCell startCell;
    public MazeCell endCell;

    public MazeData(int width, int height)
    {
        this.width = width;
        this.height = height;
        cells = new MazeCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new MazeCell(x, y);
            }
        }
    }

    public MazeCell GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return cells[x, y];
    }
}
