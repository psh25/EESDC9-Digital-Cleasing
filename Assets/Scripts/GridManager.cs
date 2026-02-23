using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap walkableTilemap;

    [Header("Gizmos")]
    public bool showSpawnPoints = true;
    public Color spawnPointColor = new Color(0.2f, 0.9f, 0.4f, 0.9f);
    public float spawnPointRadius = 0.08f;

    // 记录有效位置集合
    private readonly HashSet<Vector2Int> validPositions = new HashSet<Vector2Int>();
    // 记录每个格子上的当前实体
    private readonly Dictionary<Vector2Int, Entity> occupants = new Dictionary<Vector2Int, Entity>();

    private void Awake()
    {
        if (walkableTilemap == null)
        {
            walkableTilemap = FindObjectOfType<Tilemap>();
        }

        BuildGridFromTilemap();
    }

    // 根据Tilemap重建可用格子集合
    private void BuildGridFromTilemap()
    {
        validPositions.Clear();
        occupants.Clear();

        if (walkableTilemap == null)
        {
            return;
        }

        BoundsInt bounds = walkableTilemap.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                if (!walkableTilemap.HasTile(cellPos))
                {
                    continue;
                }

                validPositions.Add(new Vector2Int(x, y));
            }
        }
    }

    // 将网格坐标转换为世界坐标
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        if (walkableTilemap == null)
        {
            return transform.position;
        }

        return walkableTilemap.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
    }

    // 将世界坐标转换为网格坐标
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (walkableTilemap == null)
        {
            return Vector2Int.zero;
        }

        Vector3Int cellPos = walkableTilemap.WorldToCell(worldPos);
        return new Vector2Int(cellPos.x, cellPos.y);
    }

    // 判断坐标是否有效
    public bool IsValidPosition(Vector2Int gridPos)
    {
        return validPositions.Contains(gridPos);
    }

    // 获取指定坐标上的物体
    public Entity GetOccupant(Vector2Int gridPos)
    {
        occupants.TryGetValue(gridPos, out Entity entity);
        return entity;
    }

    // 设置指定坐标上的物体
    public void SetOccupant(Vector2Int gridPos, Entity entity)
    {
        if (!IsValidPosition(gridPos))
        {
            return;
        }

        if (entity == null)
        {
            occupants.Remove(gridPos);
            return;
        }

        occupants[gridPos] = entity;
    }

    // 清空指定坐标上的物体
    public void ClearOccupant(Vector2Int gridPos)
    {
        occupants.Remove(gridPos);
    }

    // 获取所有有效格子（供外部遍历）
    public IEnumerable<Vector2Int> GetValidPositions()
    {
        return validPositions;
    }

    // 当地图在编辑器中被修改时可手动调用此方法刷新缓存
    public void RefreshFromTilemap()
    {
        BuildGridFromTilemap();
    }


    // 寻找距离给定世界坐标最近的可用位置
    public bool TryFindNearestAvailablePosition(Vector3 worldPosition, out Vector2Int nearestPosition)
    {
        nearestPosition = default;

        if (validPositions.Count == 0)
        {
            return false;
        }

        bool found = false;
        float bestDistanceSqr = float.MaxValue;

        foreach (Vector2Int gridPos in validPositions)
        {
            if (GetOccupant(gridPos) != null)
            {
                continue;
            }

            Vector3 cellWorldPos = GridToWorld(gridPos);
            float distanceSqr = (worldPosition - cellWorldPos).sqrMagnitude;

            if (!found || distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                nearestPosition = gridPos;
                found = true;
            }
        }

        return found;
    }

    // 在Scene视图中绘制有效位置
    private void OnDrawGizmos()
    {
        if (!showSpawnPoints || spawnPointRadius <= 0f)
        {
            return;
        }

        if (walkableTilemap == null)
        {
            walkableTilemap = GetComponentInChildren<Tilemap>();
        }

        if (walkableTilemap == null)
        {
            return;
        }

        Gizmos.color = spawnPointColor;

        BoundsInt bounds = walkableTilemap.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                if (!walkableTilemap.HasTile(cellPos))
                {
                    continue;
                }

                Vector3 worldPos = walkableTilemap.GetCellCenterWorld(cellPos);
                Gizmos.DrawSphere(worldPos, spawnPointRadius);
            }
        }
    }

}