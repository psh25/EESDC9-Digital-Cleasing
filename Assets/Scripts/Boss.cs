using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class Boss : Enemy
{
    public Image healthBarFill;
    public Color normalColor = Color.red;
    public Color invincibleColor = Color.yellow;  //无敌状态下的血条颜色
    private int maxHealth;
    private float healthPercentage;
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private GameObject firewallPrefab;
    [SerializeField] private GameObject portalPrefab;
    private int skillCd=10;
    private int laserCd=8;
    private int rageCd=4;        //反转技能最小冷却
    private int maxEnergy=5;           //能量满时使用终结技能
    private int Ymax=5;     // 地图的Y轴范围，假设地图中心为(0,0)，则范围为[-Ymax, Ymax-1]
    List<Vector2Int> safePositions = new List<Vector2Int>();
    List<Vector2Int> dangerPositions = new List<Vector2Int>();
    List<int> remainingLasers = new List<int>();  // 用于存储当前阶段剩余的随机激光模式
    private Tilemap tilemap;  // 用于标记危险区域和安全区域的Tilemap
    public TileBase dangerTile;  // 用于标记危险区域的Tile
    public TileBase safeTile;    // 用于标记安全区域的Tile
    private int skillBeat;
    private int laserBeat;
    private int rageCount;   //反转技能冷却计数
    private int energy;
    private int rageDuration = 10; // 反转持续时间，单位为拍
    private int rageBeat;
    private bool isRage;
    private int ultimateWarnDuration = 10; // 终极技能警告持续时间，单位为拍
    private int ultimateDuration = 10; // 终极技能持续时间，单位为拍
    private int startUltimateBeat;
    private int currentBeat;
    int period1health = 20;
    int period2health = 10;
    int period = 1;
    bool invincible = false;
    private bool isDead = false;
    private Player player;
    private Dictionary<Vector2Int, TileBase>
    originalTiles = new Dictionary<Vector2Int, TileBase>();
    private Camera mainCamera;
    private Color originalCameraColor;

    public new void Awake()
    {
        tilemap = GridManager.walkableTilemap;
        base.Awake();
        health=30;
        maxHealth=health;
        skillBeat = BeatManager.BeatIndex + 1;
        laserBeat = BeatManager.BeatIndex + 1;
        energy = 0;
        isDead = false;
        player = FindObjectOfType<Player>();
        mainCamera = Camera.main;
        originalCameraColor = mainCamera.backgroundColor;
        isRage = false;

    }

    private new void Start()
    {
        base.Start();
        SetBossOccupant(); // 占领格子
        if(healthBarFill!=null)
        {
            healthBarFill.fillAmount = 1f; // 初始化血条为满
            healthBarFill.color = normalColor; // 设置初始颜色
            Debug.Log("Boss 血条已初始化为满");
        }
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

    private void UpdateHealthBar()         // 更新血条显示
    {
        if (healthBarFill != null)
        {
            healthPercentage = (float)health / maxHealth;
            healthBarFill.fillAmount = Mathf.Clamp01(healthPercentage);   // 确保填充量在0到1之间
        }
    }

    private void UpdateInvincibleVisual()   // 更新无敌状态的视觉效果，例如改变血条颜色
    {
        healthBarFill.color = invincible ? invincibleColor : normalColor;
    }

    private IEnumerator WaitForBeats(int beats)    //等待节拍
    {
        int startBeat = BeatManager.BeatIndex;
        while (BeatManager.BeatIndex - startBeat < beats)
            yield return null;
    }

    private List<int> GenerateRandomOrder()  //生成随机激光顺序
    {
        List<int> order = new List<int> { };
        switch (period)
        {
            case 1:
                order.AddRange(new int[] { 0, 1 });  // 第1阶段只有前两种激光模式
                break;
            case 2:
                order.AddRange(new int[] { 0, 1, 2, 3 });  // 第2阶段增加第四种激光模式
                break;
            case 3:
                order.AddRange(new int[] { 0, 1, 2, 3, 4 });  // 第3阶段增加第五种激光模式
                break;
        }
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = order[i];
            order[i] = order[j];
            order[j] = temp;
        }
        return order;
    }

    private void Laser()
    {
        if(remainingLasers.Count==0)     //如果当前激光模式列表已空，生成新的随机激光顺序
        {
            remainingLasers = GenerateRandomOrder();
        }
        int nextLaser = remainingLasers[0];    //获取下一个激光模式
        remainingLasers.RemoveAt(0);    //从列表中移除已使用的
        switch (nextLaser)
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
    }

    private IEnumerator LaserType0()          //脉冲激光：选定2行2列，连续放5次激光
    {
        try
        {
            int[] rand = new int[4];
            while (true)
            {
                rand[0] = Random.Range(-Ymax, Ymax);
                if ((rand[0] - player.GridPosition.y) * (rand[0] - player.GridPosition.y) > 1) break;    //确保激光不生成在玩家脸上
            }
            for (int i = 1; i < 4; i++)
            {
                while (true)
                {
                    rand[i] = Random.Range(-Ymax, Ymax);
                    if (rand[i] != rand[i - 1])
                    {
                        if (i == 1 && (rand[i] - player.GridPosition.y) * (rand[i] - player.GridPosition.y) > 1) break;
                        if (i == 2 && (rand[i] - player.GridPosition.x) * (rand[i] - player.GridPosition.x) > 1) break;
                        if (i == 3 && (rand[i] - player.GridPosition.x) * (rand[i] - player.GridPosition.x) > 1) break;
                    }
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
            for (int i = 0; i < 5; i++)
            {
                int rand=Random.Range(0, 2);
                if (rand == 0)
                    LaserManager.TryScheduleFullRowLaser(player.GridPosition.y, BeatManager.BeatIndex + 1);
                else
                    LaserManager.TryScheduleFullColumnLaser(player.GridPosition.x, BeatManager.BeatIndex + 1);
                yield return WaitForBeats(2);

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
        if (isDead) return;    // 如果Boss已经死亡，跳过行动
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

    private void Rage()
    {
        mainCamera.backgroundColor = new Color(148, 0, 211); //深紫罗兰
        foreach (MeleeEnemy enemy in FindObjectsOfType<MeleeEnemy>())
        {
            rageBeat=BeatManager.BeatIndex;
            enemy.actionCd = 1;    //所有小怪行动速度加快
            isRage = true;
        }
    }

    private void EndRage()
    {
        mainCamera.backgroundColor = originalCameraColor; // 恢复原始背景色
        foreach (MeleeEnemy enemy in FindObjectsOfType<MeleeEnemy>())
        {
            enemy.actionCd = 3;    //所有小怪行动速度恢复正常
            isRage = false;
        }
    }


    private void TryRandomBasicSkill()   // 在反转技能冷却期间，优先召唤小怪；反转技能冷却结束后，随机选择召唤小怪或反转玩家移动方向
    {
        if (rageCount > 0)     //如果反转技能还在冷却中，优先召唤小怪
        {
            Summon("enemy");
            rageCount--;
            skillBeat = currentBeat + skillCd;
        }

        else   //如果反转技能冷却结束，随机选择召唤小怪或反转玩家移动方向
        {
                Rage();
                skillBeat = currentBeat + skillCd;
                rageCount=rageCd; // 重置反转技能冷却
        }


    }

    private void UltimateWarn()
    {
        HashSet<Vector2Int> tempSet = new HashSet<Vector2Int>(); // 用于临时存储安全区域，避免重复
        Debug.Log("Boss 发出终极技能警告！");    //Todo:在Boss上显示一个明显的视觉提示，告诉玩家即将使用终极技能
        mainCamera.backgroundColor = new Color(255, 69, 0); //橙红色
        startUltimateBeat= currentBeat + ultimateWarnDuration; // 设置终极技能开始的拍数
        for (int i = 0; i < 2; i++)
        {
            Vector2Int? centerPos = FindRandomCenterPositionOnMap();
            for (int x = -1; x <= 1; x++)       // 以随机中心点为中心，标记一个3*3的安全区域
            {
                for(int y = -1; y <= 1; y++)
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
        mainCamera.backgroundColor = new Color(255, 0, 0); //红色
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
        mainCamera.backgroundColor = originalCameraColor; // 恢复原始背景色
        dangerPositions.Clear(); // 清空危险区域列表，准备下一次使用
        RestoreOriginalTiles(); // 恢复之前的瓦片，清除危险区域标
        player.actCooldown = 0.1f; // 恢复玩家的正常行动速度
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
        UpdateInvincibleVisual(); // 更新无敌状态的视觉效果
        for (int i = 0; i < 3; i++)
        {
            Summon("firewall"); // 召唤 Firewall
        }
        if (period == 2)
        {
            rageCount = rageCd;
            skillCd-=3;
            laserCd-=2;
        }
        if (period == 3)
        {
            energy = 0;
            skillCd-=3;
            laserCd-=2;
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
        UpdateInvincibleVisual(); // 更新无敌状态的视觉效果

    }

    private Vector2Int? FindRandomEmptyPositionOnMap()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();
        foreach (var pos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(pos) == null&& Vector2Int.Distance(pos, GridPosition) > 1)
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
        if(isDead) return;    // 如果Boss已经死亡，跳过行动
        currentBeat = BeatManager.BeatIndex;  // 获取当前拍数
        // 每拍行动（AI 决策）

        if (currentBeat==laserBeat)
        {
            Laser();
        }

        if (period == 1)
        {
            if (currentBeat == skillBeat)
            {
                Summon("enemy"); // 召唤小怪
                skillBeat = currentBeat + skillCd; // 重置召唤冷却
            }
        }


        Debug.Log("Boss 行动节拍");
        Debug.Log($"Boss 当前生命值: {health}, 当前阶段: {period}, 无敌状态: {invincible}");

        if (invincible)
        {
            SwitchPeriodCheck(); // 检查是否可以解除无敌状态
        }

        if (period == 2)
        {
            if (currentBeat == skillBeat)          //释放技能
            {
                TryRandomBasicSkill();
            }


            //Todo:增加Boss的攻击行为，例如发射子弹等
        }

        if (period == 3)
        {
            if (currentBeat == skillBeat)
            {
                if(energy<maxEnergy)        //如果能量未满，随机选择一个基本技能；如果能量已满，优先使用终极技能
                {
                    TryRandomBasicSkill();
                    energy++;
                }
                else
                {
                    UltimateWarn(); // 在技能冷却期间持续发出终极技能警告，提醒玩家躲避
                    skillBeat = BeatManager.BeatIndex + ultimateWarnDuration + ultimateDuration + skillCd;
                }
            }
            //Todo:Boss进入第3阶段，增加更强的攻击行为，例如发射更多子弹、增加移动速度等
        }

        if (currentBeat == rageBeat+rageDuration)      //反转技能逻辑
        {
            EndRage();
        }

        if (currentBeat == startUltimateBeat)      //终极技能逻辑
        {
            Ultimate();
        }

        if (currentBeat > startUltimateBeat && currentBeat < startUltimateBeat + ultimateDuration)
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
            energy = 0;   // 重置能量
        }

    }

    public override void Onhit(Vector2Int attackDirection)
    {
        return; // Boss不受普通攻击伤害
    }

    public override void BossGotHit()
    {
        if (isRage)
        {
            player.isProtected = true;
            EndRage();
            //Todo:获得护盾的特效
        }

        if (invincible)
        {
            Debug.Log("Boss 处于无敌状态，未受伤");
            return;
        }

        health--;
        UpdateHealthBar();  // 更新血条显示
        Debug.Log($"Boss 受到攻击，当前生命值: {health}");

        if (health == period1health || health == period2health)
        {
            SwitchPeriod();
            Debug.Log($"Boss 进入第{period}阶段，暂时无敌");
            return;
        }

        if (health <= 0)
        {
            OnDisable(); // 禁用Boss的行为
            StopAllCoroutines();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int pos = GridPosition + new Vector2Int(x, y);
                    GridManager.ClearOccupant(pos);
                }
            }
            isDead = true;
            Debug.Log("Boss 被击败！");
            foreach (Vector2Int checkPos in GridManager.GetValidPositions())
            {
                Enemy enemy = GridManager.GetOccupant(checkPos) as Enemy;

                if (enemy != null && enemy != this)
                {
                    enemy.Die();
                }
            }
            Die();
            Vector2Int? spawnPos = new Vector2Int(0, 0);
            GameObject newMinionObj = Instantiate(portalPrefab);
            Portal newMinion = newMinionObj.GetComponent<Portal>();
            newMinion.autoRegisterOnStart = false;
            newMinion.SetGridPosition(spawnPos.Value);
        }

    }



}




