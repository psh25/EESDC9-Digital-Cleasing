using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserManager : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject laserPrefab;          // 激光中段 Prefab（默认横向）
    [SerializeField] private GameObject laserOriginPrefab;    // 激光起点 Prefab（默认向左）
    [SerializeField] private int defaultMaxExtension = 12;    // 默认延伸格数
    [SerializeField] private float laserDisplayDuration = 0.25f;  // 激光显示持续时间

    public static LaserManager Instance { get; private set; }

    // 计划中的激光
    private readonly List<ScheduledLaser> scheduledLasers = new List<ScheduledLaser>();
    // 当前场上的激光对象
    private readonly List<GameObject> activeLaserVisuals = new List<GameObject>();

    private class ScheduledLaser
    {
        public Object source;          // 来源（用于覆盖旧计划，可以为null）
        public Vector2Int origin;      // 起点坐标
        public Vector2Int direction;   // 方向（归一化）
        public int warningBeat;        // 预警拍号
        public int executeBeat;        // 结算拍号
        public int maxExtension;       // 最大延伸格数
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }
    }

    private void OnEnable()
    {
        BeatManager.OnBeatStart += OnBeatStart;
    }

    private void OnDisable()
    {
        BeatManager.OnBeatStart -= OnBeatStart;
    }

  
    /// 安排激光发射（统一接口）
    /// <param name="source">来源对象（可为null，同一来源会覆盖旧计划）</param>
    /// <param name="origin">起点坐标</param>
    /// <param name="direction">方向（会自动归一化为轴向）</param>
    /// <param name="executeBeat">结算拍号</param>
    /// <param name="maxExtension">最大延伸格数（-1表示使用默认值）</param>
    /// 
    
    public static bool TryScheduleLaser(Object source, Vector2Int origin, Vector2Int direction, int executeBeat, int maxExtension = -1)
    {
        if (Instance == null)
        {
            return false;
        }

        return Instance.ScheduleLaser(source, origin, direction, executeBeat, maxExtension);
    }

    /// 安排全屏横向激光（某一行）
    public static bool TryScheduleFullRowLaser(int rowY, int executeBeat)
    {
        if (Instance == null)
        {
            return false;
        }

        // 起点在视野外左侧
        Vector2Int origin = new Vector2Int(-20, rowY);
        return Instance.ScheduleLaser(null, origin, Vector2Int.right, executeBeat, 40);
    }


    /// 安排全屏竖向激光（某一列）
    public static bool TryScheduleFullColumnLaser(int columnX, int executeBeat)
    {
        if (Instance == null)
        {
            return false;
        }

        // 起点在视野外下方
        Vector2Int origin = new Vector2Int(columnX, -20);
        return Instance.ScheduleLaser(null, origin, Vector2Int.up, executeBeat, 40);
    }

    /// 按来源取消激光计划
    public static bool TryCancelBySource(Object source)
    {
        if (Instance == null || source == null)
        {
            return false;
        }

        return Instance.CancelBySource(source);
    }

    private bool ScheduleLaser(Object source, Vector2Int origin, Vector2Int direction, int executeBeat, int maxExtension)
    {
        if (gridManager == null)
        {
            return false;
        }

        if (executeBeat <= BeatManager.BeatIndex)
        {
            return false;
        }

        // 归一化方向（仅支持轴向）
        Vector2Int normalizedDirection = NormalizeDirection(direction);
        if (normalizedDirection == Vector2Int.zero)
        {
            return false;
        }

        // 使用默认延伸值
        if (maxExtension <= 0)
        {
            maxExtension = defaultMaxExtension;
        }

        // 同一来源覆盖旧计划
        if (source != null)
        {
            scheduledLasers.RemoveAll(l => l.source == source);
        }

        ScheduledLaser laser = new ScheduledLaser
        {
            source = source,
            origin = origin,
            direction = normalizedDirection,
            warningBeat = executeBeat - 1,  // 结算前一拍显示预警
            executeBeat = executeBeat,
            maxExtension = maxExtension
        };
        scheduledLasers.Add(laser);

        // 如果预警拍号已经是当前或更早，立即上报预警
        if (laser.warningBeat <= BeatManager.BeatIndex)
        {
            ReportWarningsForLaser(laser);
        }
        
        return true;
    }

    private bool CancelBySource(Object source)
    {
        int removed = scheduledLasers.RemoveAll(l => l.source == source);
        return removed > 0;
    }

    private void OnBeatStart()
    {
        int currentBeat = BeatManager.BeatIndex;

        // 第一步：显示本拍的预警（结算前一拍）
        foreach (ScheduledLaser laser in scheduledLasers)
        {
            if (laser.warningBeat == currentBeat)
            {
                ReportWarningsForLaser(laser);
            }
        }

        // 第二步：结算并显示激光
        List<ScheduledLaser> executingLasers = scheduledLasers.FindAll(l => l.executeBeat == currentBeat);
        if (executingLasers.Count > 0)
        {
            ClearLaserVisuals();

            foreach (ScheduledLaser laser in executingLasers)
            {
                List<Vector2Int> executePath = GetExecutePath(laser);
                ResolveLaserDamage(executePath);
                SpawnLaserVisuals(executePath, laser.direction);
            }

            if (activeLaserVisuals.Count > 0)
            {
                StartCoroutine(ClearLaserVisualsAfterDelay());
            }
        }

        // 第三步：清理过期计划
        scheduledLasers.RemoveAll(l => l.executeBeat <= currentBeat);
    }

    // 获取预警路径（仅包含有效格）
    private List<Vector2Int> GetWarningPath(ScheduledLaser laser)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = laser.origin;

        for (int i = 0; i < laser.maxExtension; i++)
        {
            current += laser.direction;
            if (gridManager.IsValidPosition(current))
            {
                path.Add(current);
            }
        }

        return path;
    }

    // 获取结算路径（包含所有格子，不限于有效格）
    private List<Vector2Int> GetExecutePath(ScheduledLaser laser)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = laser.origin;

        for (int i = 0; i < laser.maxExtension; i++)
        {
            current += laser.direction;
            path.Add(current);
        }

        return path;
    }

    // 上报预警给 WarningManager
    private void ReportWarningsForLaser(ScheduledLaser laser)
    {
        List<Vector2Int> warningPath = GetWarningPath(laser);

        if (warningPath.Count == 0)
        {
            return;
        }

        if (laser.source != null)
        {
            WarningManager.TryReportWarnings(laser.source, warningPath, laser.executeBeat);
        }
        else
        {
            // 匿名激光逐格上报
            foreach (Vector2Int cell in warningPath)
            {
                WarningManager.TryReportWarning(cell, laser.executeBeat);
            }
        }
    }

    // 激光结算：对命中格上的敌人造成伤害
    private void ResolveLaserDamage(List<Vector2Int> executePath)
    {
        foreach (Vector2Int cell in executePath)
        {
            // 只对有效格上的实体造成伤害
            if (!gridManager.IsValidPosition(cell))
            {
                continue;
            }

            Entity occupant = gridManager.GetOccupant(cell);

            if (occupant is Player player)
            {
                player.Onhit(Vector2Int.zero);  // 激光不推挤，传零向量
            }
        }
    }

    // 生成激光视觉对象
    private void SpawnLaserVisuals(List<Vector2Int> executePath, Vector2Int direction)
    {
        if (laserPrefab == null || executePath.Count == 0)
        {
            return;
        }

        bool isVertical = (direction == Vector2Int.up || direction == Vector2Int.down);

        for (int i = 0; i < executePath.Count; i++)
        {
            Vector2Int cell = executePath[i];
            Vector3 worldPos = gridManager.GridToWorld(cell);

            // 起点使用专用 Prefab，其他格子使用中段 Prefab
            bool isOrigin = (i == 0);
            GameObject prefabToUse = isOrigin && laserOriginPrefab != null ? laserOriginPrefab : laserPrefab;
            GameObject laser = Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);

            // 根据是否为起点应用不同旋转逻辑
            if (isOrigin && laserOriginPrefab != null)
            {
                // 起点 Prefab 默认向左，根据方向旋转
                laser.transform.rotation = GetOriginRotation(direction);
            }
            else
            {
                // 中段 Prefab 默认横向，竖向旋转90度
                if (isVertical)
                {
                    laser.transform.rotation = Quaternion.Euler(0, 0, 90);
                }
            }

            activeLaserVisuals.Add(laser);
        }
    }

    // 获取激光起点的旋转角度（起点默认向左）
    private Quaternion GetOriginRotation(Vector2Int direction)
    {
        if (direction == Vector2Int.left)
        {
            return Quaternion.identity;  // 默认向左，不旋转
        }
        else if (direction == Vector2Int.right)
        {
            return Quaternion.Euler(0, 0, 180);  // 向右，旋转180度
        }
        else if (direction == Vector2Int.up)
        {
            return Quaternion.Euler(0, 0, -90);  // 向上，旋转90度
        }
        else if (direction == Vector2Int.down)
        {
            return Quaternion.Euler(0, 0, 90);  // 向下，旋转-90度
        }

        return Quaternion.identity;
    }

    private IEnumerator ClearLaserVisualsAfterDelay()
    {
        yield return new WaitForSeconds(laserDisplayDuration);
        ClearLaserVisuals();
    }

    private void ClearLaserVisuals()
    {
        foreach (GameObject laser in activeLaserVisuals)
        {
            if (laser != null)
            {
                Destroy(laser);
            }
        }

        activeLaserVisuals.Clear();
    }

    // 归一化方向为轴向单位向量
    private Vector2Int NormalizeDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return Vector2Int.zero;
        }

        // 只支持轴向（上下左右）
        int x = direction.x == 0 ? 0 : (direction.x > 0 ? 1 : -1);
        int y = direction.y == 0 ? 0 : (direction.y > 0 ? 1 : -1);

        Vector2Int normalized = new Vector2Int(x, y);

        // 拒绝斜向
        if (normalized.x != 0 && normalized.y != 0)
        {
            return Vector2Int.zero;
        }

        return normalized;
    }
}
