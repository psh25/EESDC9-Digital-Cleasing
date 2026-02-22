using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : Enemy
{
    private int turnCounter = 0;               // 0:抬手回合, 1:行动回合
    private Vector2Int? pendingDirection = null;
    private int pendingWarningExecuteBeat = -1;

    private Animator animator;

    // 获取组件
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

        if (turnCounter == 0)
        {
            ChooseDirection();  //抬手阶段
            if (pendingDirection.HasValue)
            {
                if (pendingDirection.Value.x < 0)
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else if (pendingDirection.Value.x > 0)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
                ReportNextBeatWarning(); // 抬手后立刻上报下一拍预警
            }
            turnCounter = 1;
        }
        else  //行动阶段
        {
            if (pendingDirection.HasValue)
            {
                Vector2Int targetPos = GridPosition + pendingDirection.Value;

                if (!GridManager.IsValidPosition(targetPos))
                {
                    pendingDirection = null;
                    turnCounter = 0;
                    return;
                }

                Entity occupant = GridManager.GetOccupant(targetPos);
                if (occupant is Player player)
                {
                    player.Onhit(pendingDirection.Value);
                }
                else if (occupant == null)  // 没有障碍，可以移动
                {
                    TryMove(pendingDirection.Value);
                }
                else
                {
                    // 前方有障碍，可能什么都不做
                }

                pendingDirection = null;
                pendingWarningExecuteBeat = -1;
            }
            turnCounter = 0;
        }
    }

    protected override void OnMovedByTryMove(Vector2Int oldPos, Vector2Int newPos)
    {
        base.OnMovedByTryMove(oldPos, newPos);

        // 抬手阶段内被迫移动：立即将预警刷新到新目标格，结算拍号保持不变
        if (turnCounter != 1 || !pendingDirection.HasValue)
        {
            return;
        }

        if (pendingWarningExecuteBeat <= BeatManager.BeatIndex)
        {
            return;
        }

        ReportPendingWarningAtCurrentPosition();
    }

    // 上报近战敌人下一拍的危险格（正前方一格）
    private void ReportNextBeatWarning()
    {
        pendingWarningExecuteBeat = BeatManager.BeatIndex + 1;
        ReportPendingWarningAtCurrentPosition();
    }

    private void ReportPendingWarningAtCurrentPosition()
    {
        if (GridManager == null || !pendingDirection.HasValue)
        {
            return;
        }

        Vector2Int targetPos = GridPosition + pendingDirection.Value;
        if (!GridManager.IsValidPosition(targetPos))
        {
            return;
        }

        WarningManager.TryReportWarning(this, targetPos, pendingWarningExecuteBeat);
    }

    // 重写死亡方法：清除未结算的预警
    public new void Die()
    {
        // 清除该敌人的所有未结算预警（通过传入空列表触发清除）
        if (pendingDirection.HasValue && pendingWarningExecuteBeat > BeatManager.BeatIndex)
        {
            WarningManager.TryReportWarnings(this, new List<Vector2Int>(), pendingWarningExecuteBeat);
        }
        
        // 调用基类的死亡逻辑
        base.Die();
    }

    private void ChooseDirection()  //选择方向
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        System.Collections.Generic.List<Vector2Int> validDirs = new System.Collections.Generic.List<Vector2Int>();  //存储有效方向

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = GridPosition + dir;  //计算邻居位置
            if (GridManager.IsValidPosition(neighbor) && (GridManager.GetOccupant(neighbor) == null||GridManager.GetOccupant(neighbor) is Player))  // 只有空格或玩家才算有效方向
                validDirs.Add(dir);  //将有效方向添加到列表
        }

        if (validDirs.Count > 0)
        {
            pendingDirection = validDirs[Random.Range(0, validDirs.Count)];  //随机选择一个有效方向
            
        }
        else
        {
            pendingDirection = null;  //没有有效方向，保持原地不动
        }
    }
}
