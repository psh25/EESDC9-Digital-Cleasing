using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Boss : Enemy
{
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private GameObject firewallPrefab;
    [SerializeField] private int summonCd;
    [SerializeField] private int laserCd;
    [SerializeField] private int reverseDirectionCd;
    [SerializeField]private int ultimateCd;
    [SerializeField] private int Ymax;     // 地图的Y轴范围，假设地图中心为(0,0)，则范围为[-Ymax, Ymax-1]
    [SerializeField] private int maxEnemyNumber;     
    List<Vector2Int> safePositions = new List<Vector2Int>();
    List<Vector2Int> dangerPositions = new List<Vector2Int>();
    private Tilemap tilemap;  // 用于标记危险区域和安全区域的Tilemap
    public TileBase dangerTile;  // 用于标记危险区域的Tile
    public TileBase safeTile;    // 用于标记安全区域的Tile
    private int enemyCount;    //敌人数量
    private int summonBeat;
    private int laserBeat;
    private int reverseWarnDuration = 3; // 反转警告持续时间，单位为拍
    private int startReverseWarnBeat;
    private int reverseDirectionDuration = 5; // 反转持续时间，单位为拍
    private int startReverseDirectionBeat;
    private int ultimateWarnDuration = 10; // 终极技能警告持续时间，单位为拍
    private int startUltimateWarnBeat;
    private int ultimateDuration = 10; // 终极技能持续时间，单位为拍
    private int startUltimateBeat;
    private int currentBeat;
    int period1health = 20;
    int period2health = 10;
    int period = 1;
    int laserCount = 0;
    int maxLaserCount = 1;
    bool invincible = false;
    private Player player;
    private Dictionary<Vector2Int, TileBase>
    originalTiles = new Dictionary<Vector2Int, TileBase>();

    public new void Awake()
    {
        tilemap = GridManager.walkableTilemap;
        base.Awake();
        health = 30;
        enemyCount = 0;
        laserBeat = 1;
        summonBeat = 1;
        laserCount = 0;
        maxLaserCount = 1;
        startReverseWarnBeat = 1;
        startUltimateWarnBeat = 5;
        player = FindObjectOfType<Player>();

    }
    public void SetBossOccupant()
    { // 占领以Boss为中心的3x3格子
        Vector2Int[] occupantOffset = new Vector2Int[9];
        int index = 0;
        for (int i = 1; i >= -1; i--)
        {
            for (int j = -1; j <= 1; j++)
            {
                occupantOffset[index++] = new Vector2Int(i, j);
            }
        }
        foreach (var offset in occupantOffset)
        {
            GridManager.SetOccupant(GridPosition + offset, this);
            Debug.Log($"Boss 占领格子: {GridPosition + offset}");
        }
    }

    private IEnumerator WaitForBeats(int beats)    //等待节拍
    {
        int startBeat = BeatManager.BeatIndex;
        while (BeatManager.BeatIndex - startBeat < beats)
            yield return null;
    }


    private void Laser()
    {
        switch (laserCount)
        {
            case (0):
                StartCoroutine(LaserType0());
                Debug.Log("释放激光0");
                break;
            case (1):
                StartCoroutine(LaserType1());
                Debug.Log("释放激光1");
                break;
            case (2):
                StartCoroutine(LaserType2());
                Debug.Log("释放激光2");
                break;
            case (3):
                StartCoroutine(LaserType3());
                Debug.Log("释放激光3");
                break;
            case (4):
                StartCoroutine(LaserType4());
                Debug.Log("释放激光4");
                break;
        }
        laserCount++;
        if (laserCount > maxLaserCount) laserCount = 0;
    }

    private IEnumerator LaserType0()          //脉冲激光：选定2行2列，连续放5次激光
    {
        try
        {
            int[] rand = new int[4];
            rand[0] = Random.Range(-Ymax, Ymax);
            for (int i = 1; i < 4; i++)
            {
                while (true)
                {
                    rand[i] = Random.Range(-Ymax, Ymax);
                    if (rand[i] != rand[i - 1]) break;
                }

            }
            for (int i = 0; i < 5; i++)
            {
                LaserManager.TryScheduleFullRowLaser(rand[0], BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullRowLaser(rand[1], BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullColumnLaser(rand[2], BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullColumnLaser(rand[3], BeatManager.BeatIndex + 1);
                yield return WaitForBeats(1);
            }
        }
        finally
        {
            laserBeat = BeatManager.BeatIndex + laserCd;
        }
    }

    private IEnumerator LaserType1()    //收缩激光：由内向外，四个方向放激光，延伸至四角四个2*2区域
    {
        try
        {
            for (int i = 0; i < 3; i++)
            {
                LaserManager.TryScheduleFullRowLaser(i, BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullRowLaser(-i-1, BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullColumnLaser(i, BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullColumnLaser(-i-1, BeatManager.BeatIndex + 1);
                yield return WaitForBeats(1);
            }
        }
        finally
        {
            laserBeat = BeatManager.BeatIndex + laserCd;
        }
    }

    private IEnumerator LaserType2()             //跟踪激光：跟踪玩家位置交替释放行或列激光，持续10次
    {
        try
        {
            for (int i = 0; i < 10; i++)
            {
                int rand=Random.Range(0, 2);
                if (rand == 0)
                    LaserManager.TryScheduleFullRowLaser(player.GridPosition.y, BeatManager.BeatIndex + 1);
                else
                    LaserManager.TryScheduleFullColumnLaser(player.GridPosition.x, BeatManager.BeatIndex + 1);
                yield return WaitForBeats(1);

            }
        }
        finally
        {
            laserBeat = BeatManager.BeatIndex + laserCd;
        }
    }

    private IEnumerator LaserType3()         //遍历激光：从左到右，从上到下，释放十字激光，可卡一拍间隔穿过   
    {
        try
        {
            for (int i = -Ymax; i < Ymax; i++)
            {
                LaserManager.TryScheduleFullRowLaser(i, BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullColumnLaser(i, BeatManager.BeatIndex + 1);
                yield return WaitForBeats(1);
            }
            for (int i = Ymax - 1; i >= -Ymax; i--)
            {
                LaserManager.TryScheduleFullRowLaser(i, BeatManager.BeatIndex + 1);
                LaserManager.TryScheduleFullColumnLaser(i, BeatManager.BeatIndex + 1);
                yield return WaitForBeats(1);
            }
        }
        finally
        {
            laserBeat = BeatManager.BeatIndex + laserCd;
        }
    }

    private IEnumerator LaserType4()         //交替激光：奇数行->奇数列->偶数行->偶数列
    {
        try
        {
            for (int j = 0; j < 2; j++)
            {
                for (int i = -Ymax; i < Ymax; i++)
                {
                    if (i % 2 != 0)
                        LaserManager.TryScheduleFullRowLaser(i, BeatManager.BeatIndex + 1);
                }
                yield return WaitForBeats(1);
                for (int i = -Ymax; i < Ymax; i++)
                {
                    if (i % 2 != 0)
                        LaserManager.TryScheduleFullColumnLaser(i, BeatManager.BeatIndex + 1);
                }
                yield return WaitForBeats(1);
                for (int i = -Ymax; i < Ymax; i++)
                {
                    if (i % 2 == 0)
                        LaserManager.TryScheduleFullRowLaser(i, BeatManager.BeatIndex + 1);
                }
                yield return WaitForBeats(1);
                for (int i = -Ymax; i < Ymax; i++)
                {
                    if (i % 2 == 0)
                        LaserManager.TryScheduleFullColumnLaser(i, BeatManager.BeatIndex + 1);
                }
                yield return WaitForBeats(1);
            }
        }
        finally
        {
            laserBeat = BeatManager.BeatIndex + laserCd;
        }
    }

    private void Summon(string name)
    {
        Vector2Int? spawnPos = FindRandomEmptyPositionOnMap();
        if (spawnPos.HasValue)
        {
            if (name == "enemy")
            {
                GameObject newMinionObj = Instantiate(minionPrefab);
                MeleeEnemy newMinion = newMinionObj.GetComponent<MeleeEnemy>();
                newMinion.autoRegisterOnStart = false;
                newMinion.SetGridPosition(spawnPos.Value);
            }
            else if (name == "firewall")
            {
                GameObject newMinionObj = Instantiate(firewallPrefab);
                Firewall newMinion = newMinionObj.GetComponent<Firewall>();
                if (newMinion == null)
                {
                    Debug.LogError("Firewall prefab 上没有 Firewall 脚本组件！");
                    return;
                }
                newMinion.autoRegisterOnStart = false;
                newMinion.SetGridPosition(spawnPos.Value);

                // 可选：添加防火墙的额外初始化逻辑，确保它被正确注册到网格中
                //GridManager.SetOccupant(spawnPos.Value, newMinion);
            }

        }
        else
        {
            Debug.Log("没有可用的空位置召唤小怪");
        }

    }

    private void ReverseWarn()
    {
        Debug.Log("Boss 发出反转警告！");    //Todo:在Boss上显示一个明显的视觉提示，告诉玩家即将反转移动方向
    }

    private void ReverseDirection()
    {
        if (player != null)
        {
            player.isReverseDirection = true;
            Debug.Log("Boss 反转了玩家的移动方向！");
        }
    }

    private void EndReverseDirection()
    {
        if (player != null)
        {
            player.isReverseDirection = false;
            Debug.Log("Boss 结束了玩家的移动方向反转！");
        }
    }

    private void UltimateWarn()
    {
        HashSet<Vector2Int> tempSet = new HashSet<Vector2Int>(); // 用于临时存储安全区域，避免重复
        Debug.Log("Boss 发出终极技能警告！");    //Todo:在Boss上显示一个明显的视觉提示，告诉玩家即将使用终极技能
        for(int i = 0; i < 2; i++)
        {
            Vector2Int? centerPos = FindRandomCenterPositionOnMap();
            for (int x = -2; x <= 2; x++)       // 以随机中心点为中心，标记一个5x5的安全区域
            {
                for(int y = -2; y <= 2; y++)
                {
                    Vector2Int checkPos = centerPos.Value + new Vector2Int(x, y);
                    if (GridManager.GetOccupant(checkPos) is not Boss)
                    {
                        tempSet.Add(checkPos);
                    }
                }
            }
        }
        safePositions = new List<Vector2Int>(tempSet); // 将HashSet转换为List
        SetTiles(safePositions, safeTile); // 将安全区域标记为 SafeTile
    }

    private void Ultimate()
    {
        RestoreOriginalTiles(); // 恢复之前的瓦片，清除安全区域标记
        Debug.Log("Boss 使用了终极技能！");    //Todo:Boss执行终极技能，例如发射大量子弹、造成范围伤害等
        foreach (Vector2Int checkPos in GridManager.GetValidPositions())       // 将非安全区域标记为危险区域
        {
            if(!safePositions.Contains(checkPos))
            {
                dangerPositions.Add(checkPos);
            }
        }
        safePositions.Clear(); // 清空安全区域列表，准备下一次使用
        SetTiles(dangerPositions, dangerTile); // 将危险区域标记为 DangerTile
        foreach(Vector2Int pos in dangerPositions)       // 对危险区域内的玩家造成伤害
        {
            if (GridManager.GetOccupant(pos) is Player player)
            {
                player.Die(); // 直接死亡，或根据需要改为减少生命值等
            }
        }
    }

    private void EndUltimate()
    {
        Debug.Log("Boss 结束了终极技能！");    //Todo:结束终极技能的效果，例如停止发射子弹、移除范围伤害标记等
        dangerPositions.Clear(); // 清空危险区域列表，准备下一次使用
        RestoreOriginalTiles(); // 恢复之前的瓦片，清除危险区域标
        player.actCooldown = 0.2f; // 恢复玩家的正常行动速度
    }

    private void SwitchPeriod()
    {
        foreach (Vector2Int checkPos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(checkPos) is MeleeEnemy)
            {
                GridManager.GetOccupant(checkPos).Die(); // 清除场上所有 MeleeEnemy
            }
        }
        period++;
        invincible = true; // 进入新阶段后暂时无敌
        for (int i = 0; i < 3; i++)
        {
            Summon("firewall"); // 召唤 Firewall
        }
        if (period == 2)
        {
            startReverseWarnBeat = BeatManager.BeatIndex + 2; // 设置第一次反转警告时间
            summonCd--;
            laserCd--;
            maxLaserCount = 3;
        }
        if (period == 3)
        {
            startUltimateWarnBeat = BeatManager.BeatIndex + 10; // 设置第一次终极技能警告时间
            summonCd--;
            laserCd--;
            maxLaserCount = 4;
        }
    }

    private void SwitchPeriodCheck()
    {
        foreach (Vector2Int checkPos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(checkPos) is Firewall)
            {
                return;
            }
        }
        invincible = false;  // 如果没有 Firewall 了，解除无敌状态

    }

    private Vector2Int? FindRandomEmptyPositionOnMap()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();

        foreach (var pos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(pos) == null)
            {
                emptyPositions.Add(pos);
            }
        }

        if (emptyPositions.Count == 0)
            return null;

        return emptyPositions[Random.Range(0, emptyPositions.Count)];
    }

    private Vector2Int? FindRandomCenterPositionOnMap()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();

        foreach (var pos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(pos) is not Boss&&pos.y<=2&&pos.y>=-3&&pos.x<=6&&pos.x>=-7)  // 确保不选择Boss所在的格子
            {
                emptyPositions.Add(pos);
            }
        }

        if (emptyPositions.Count == 0)
            return null;

        return emptyPositions[Random.Range(0, emptyPositions.Count)];
    }

    private void SetTiles(List<Vector2Int> positions, TileBase Tile)
    {
        if (tilemap == null) return;
        originalTiles.Clear();  // 如果之前有标记，先清空（可根据需要调整）
        foreach (Vector2Int pos in positions)
        {
            Vector3Int cellPos = new Vector3Int(pos.x, pos.y, 0);
            TileBase current = tilemap.GetTile(cellPos);
            originalTiles[pos] = current;           // 保存原始瓦片
            tilemap.SetTile(cellPos, Tile);     // 改为安全瓦片
        }
    }

    // 恢复原始瓦片
    private void RestoreOriginalTiles()
    {
        if (tilemap == null) return;
        foreach (var kvp in originalTiles)
        {
            Vector3Int cellPos = new Vector3Int(kvp.Key.x, kvp.Key.y, 0);
            tilemap.SetTile(cellPos, kvp.Value);
        }
        originalTiles.Clear();
    }

    public override void PerformAction()
    {
        currentBeat = BeatManager.BeatIndex;  // 获取当前拍数
        enemyCount = 0;
        // 每拍行动（AI 决策）
        foreach (Vector2Int checkPos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(checkPos) is MeleeEnemy)
                enemyCount++;
                
        }
        if (enemyCount < maxEnemyNumber)    //敌人数量有上限
        {
            if (currentBeat >= summonBeat)
            {
                Summon("enemy"); // 召唤小怪
                summonBeat += summonCd; // 重置召唤冷却
            }
        }

        if (currentBeat==laserBeat)
        {
            Laser();
        }



            Debug.Log("Boss 行动节拍");
        Debug.Log($"Boss 当前生命值: {health}, 当前阶段: {period}, 无敌状态: {invincible}");

        if (invincible)
        {
            SwitchPeriodCheck(); // 检查是否可以解除无敌状态
        }

        if (period >= 2)
        {
            if (currentBeat == startReverseWarnBeat)
            {
                ReverseWarn();
                startReverseDirectionBeat = currentBeat + reverseWarnDuration; // 设置反转持续时间
            }

            if (currentBeat == startReverseDirectionBeat)
            {
                ReverseDirection();
            }

            if (currentBeat == startReverseDirectionBeat + reverseDirectionDuration)
            {
                EndReverseDirection();
                startReverseWarnBeat = currentBeat + reverseDirectionCd; // 设置下一次反转警告时间
            }
            //Todo:增加Boss的攻击行为，例如发射子弹等
        }

        if (period == 3)
        {
            if(currentBeat == startUltimateWarnBeat)
            {
                UltimateWarn();
                startUltimateBeat = currentBeat + ultimateWarnDuration; // 设置终极技能执行时间
            }

            if(currentBeat == startUltimateBeat)
            {
                Ultimate();
            }

            if(currentBeat> startUltimateBeat && currentBeat < startUltimateBeat + ultimateDuration)
            { 
                foreach (Vector2Int pos in dangerPositions)       
                {
                    if (GridManager.GetOccupant(pos) is Player player)
                    {
                        player.actCooldown = 0.4f; //缓慢玩家的行动速度，增加挑战性
                    }
                }

                Debug.Log("Boss 终极技能持续中，对玩家造成持续伤害！");
            }

            if (currentBeat == startUltimateBeat + ultimateDuration)
            {
                EndUltimate();
                startUltimateWarnBeat = currentBeat + ultimateCd; // 设置下一次终极技能警告时间
            }
            //Todo:Boss进入第3阶段，增加更强的攻击行为，例如发射更多子弹、增加移动速度等
        }
    }

    public override void Onhit(Vector2Int attackDirection)
    {
        return; // Boss不受普通攻击伤害
    }

    public override void BossGotHit()
    {
        if (invincible)
        {
            Debug.Log("Boss 处于无敌状态，未受伤");
            return;
        }

        health--;
        Debug.Log($"Boss 受到攻击，当前生命值: {health}");

        if (health == period1health || health == period2health)
        {
            SwitchPeriod();
            Debug.Log($"Boss 进入第{period}阶段，暂时无敌");
            return;
        }

        if (health <= 0)
        {
            Debug.Log("Boss 被击败！");
            Die();         //清除场上所有敌人
            foreach (var pos in GridManager.GetValidPositions())
            {
                if (GridManager.GetOccupant(pos) is Enemy enemy)
                {
                    enemy.Die();
                }
            }

        }
    }
}




