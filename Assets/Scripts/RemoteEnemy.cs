using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteEnemy : Enemy
{
    [SerializeField] private int laserExtension = 12;  // 激光延伸格数

    private int turnCounter = 0;               // 0:抬手回合, 1:行动回合
    private Vector2Int? pendingDirection = null;
    private int pendingLaserExecuteBeat = -1;
    private bool attacking = false;
    private Animator animator;

    public override void Awake()
    {
        base.Awake();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public override void PerformAction()
    {
        if (GridManager == null)
            return;

        if (turnCounter == 0)  //抬手回合
        {
            attacking = false;
            ChooseDirection();
            if (pendingDirection.HasValue)
            {
                //调整朝向
                if (pendingDirection.Value.x < 0)
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else if (pendingDirection.Value.x > 0)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
                // 两拍后结算激光（当前拍抬手 → 下一拍预警 → 再下一拍结算）
                animator.SetBool("attacking", attacking);
                pendingLaserExecuteBeat = BeatManager.BeatIndex + 1;
                LaserManager.TryScheduleLaser(
                    source: this,
                    origin: GridPosition,
                    direction: pendingDirection.Value,
                    executeBeat: pendingLaserExecuteBeat,
                    maxExtension: laserExtension
                );
            }
            turnCounter = 1;
        }
        else  //行动回合（激光已在 LaserManager 中结算，这里只重置状态）
        {
            attacking = true;
            animator.SetBool("attacking", attacking);
            pendingLaserExecuteBeat = -1;
            pendingDirection = null;
            turnCounter = 0;
        }
    }


    protected override void OnMovedByTryMove(Vector2Int oldPos, Vector2Int newPos)
    {
        base.OnMovedByTryMove(oldPos, newPos);

        // 抬手后被迫移动：刷新激光起点到新位置，结算拍号保持不变
        if (turnCounter != 1 || !pendingDirection.HasValue)
        {
            return;
        }

        if (pendingLaserExecuteBeat <= BeatManager.BeatIndex)
        {
            return;
        }

        // 重新上报会自动覆盖该来源的旧激光计划
        LaserManager.TryScheduleLaser(
            source: this,
            origin: GridPosition,  // 使用新位置
            direction: pendingDirection.Value,
            executeBeat: pendingLaserExecuteBeat,
            maxExtension: laserExtension
        );
    }

    // 重写死亡方法：清除未结算的激光
    public override void Die()
    {
        // 清除该敌人调度的所有未结算激光（激光和预警一起清除）
        LaserManager.TryCancelBySource(this);
        
        // 调用基类的死亡逻辑
        base.Die();
    }

    private void ChooseDirection()
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        System.Collections.Generic.List<Vector2Int> validDirs = new System.Collections.Generic.List<Vector2Int>();  // 存储有效方向

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = GridPosition + dir;
            if (GridManager.IsValidPosition(neighbor) && (GridManager.GetOccupant(neighbor) == null || GridManager.GetOccupant(neighbor) is Player))  // 只有空格或玩家才算有效方向
                validDirs.Add(dir);
        }

        if (validDirs.Count > 0)
        {
            pendingDirection = validDirs[Random.Range(0, validDirs.Count)];  // 从有效方向中随机选择一个
        }
        else
        {
            pendingDirection = null;  // 没有有效方向，不攻击
        }
    }
} 