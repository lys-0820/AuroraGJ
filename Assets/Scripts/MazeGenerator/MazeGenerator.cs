using UnityEngine;

/// <summary>
/// 统一入口：根据 MazeConfig.type 调用对应的生成器。
/// </summary>
public static class MazeGenerator
{
    public static MazeData Generate(MazeConfig config)
    {
        if (config == null)
        {
            Debug.LogError("MazeGenerator: config is null!");
            return null;
        }

        MazeData data = null;

        switch (config.type)
        {
            case MazeType.DFS:
                data = MazeGeneratorDFS.Generate(config);
                break;

            case MazeType.Corridor:
                data = MazeGeneratorCorridor.Generate(config);
                break;

            case MazeType.Ring:
                data = MazeGeneratorRing.Generate(config);
                break;

            case MazeType.Spiral:
                data = MazeGeneratorSpiral.Generate(config);
                break;

            case MazeType.Branching:
                data = MazeGeneratorBranching.Generate(config);
                break;

            case MazeType.Prim:
                data = MazeGeneratorPrim.Generate(config);
                break;

            case MazeType.Wilson:
                data = MazeGeneratorWilson.Generate(config);
                break;

            default:
                Debug.LogWarning($"MazeGenerator: unknown type {config.type}, fallback to DFS.");
                data = MazeGeneratorDFS.Generate(config);
                break;
        }

        return data;
    }
}
