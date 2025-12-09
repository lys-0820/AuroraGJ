using System.Collections.Generic;
using UnityEngine;

public class MazeBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject exitPrefab;

    [Header("Layout")]
    public float cellSize = 2f;      // 每个格子的间距
    public float wallHeight = 2f;    // 墙多高

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private MazeData currentData;

    public void Build(MazeData data)
    {
        currentData = data;

        // 生成地板和内部墙体
        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                MazeCell cell = data.cells[x, y];

                // 地板
                Vector3 cellWorld = CellToWorld(cell);
                GameObject floor = Instantiate(floorPrefab, cellWorld, Quaternion.identity, transform);
                spawnedObjects.Add(floor);

                // 上墙（北）
                if (cell.walls[0])
                {
                    Vector3 pos = cellWorld + new Vector3(0f, wallHeight / 2f, cellSize / 2f);
                    Quaternion rot = Quaternion.identity; // 面向 Z 轴
                    GameObject wall = Instantiate(wallPrefab, pos, rot, transform);
                    wall.transform.localScale = new Vector3(cellSize, wallHeight, 0.2f);
                    spawnedObjects.Add(wall);
                }

                // 右墙（东）
                if (cell.walls[1])
                {
                    Vector3 pos = cellWorld + new Vector3(cellSize / 2f, wallHeight / 2f, 0f);
                    Quaternion rot = Quaternion.Euler(0f, 90f, 0f); // 旋转 90 度
                    GameObject wall = Instantiate(wallPrefab, pos, rot, transform);
                    wall.transform.localScale = new Vector3(cellSize, wallHeight, 0.2f);
                    spawnedObjects.Add(wall);
                }

                // 下墙（南）—— 只在最底一行生成，避免重复
                if (y == 0 && cell.walls[2])
                {
                    Vector3 pos = cellWorld + new Vector3(0f, wallHeight / 2f, -cellSize / 2f);
                    Quaternion rot = Quaternion.identity;
                    GameObject wall = Instantiate(wallPrefab, pos, rot, transform);
                    wall.transform.localScale = new Vector3(cellSize, wallHeight, 0.2f);
                    spawnedObjects.Add(wall);
                }

                // 左墙（西）—— 只在最左一列生成，避免重复
                if (x == 0 && cell.walls[3])
                {
                    Vector3 pos = cellWorld + new Vector3(-cellSize / 2f, wallHeight / 2f, 0f);
                    Quaternion rot = Quaternion.Euler(0f, 90f, 0f);
                    GameObject wall = Instantiate(wallPrefab, pos, rot, transform);
                    wall.transform.localScale = new Vector3(cellSize, wallHeight, 0.2f);
                    spawnedObjects.Add(wall);
                }
            }
        }

        // 出口
        if (exitPrefab != null && data.endCell != null)
        {
            Vector3 exitPos = CellToWorld(data.endCell) + Vector3.up * 0.5f;
            GameObject exitObj = Instantiate(exitPrefab, exitPos, Quaternion.identity, transform);
            spawnedObjects.Add(exitObj);
        }
    }

    public void ClearMaze()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        currentData = null;
    }

    public Vector3 GetWorldPosOfCell(MazeCell cell)
    {
        return CellToWorld(cell);
    }

    private Vector3 CellToWorld(MazeCell cell)
    {
        // 把迷宫中心放在 (0,0,0) 附近
        float originOffsetX = -(currentData.width - 1) * cellSize * 0.5f;
        float originOffsetZ = -(currentData.height - 1) * cellSize * 0.5f;

        return new Vector3(
            originOffsetX + cell.x * cellSize,
            0f,
            originOffsetZ + cell.y * cellSize
        );
    }
}
