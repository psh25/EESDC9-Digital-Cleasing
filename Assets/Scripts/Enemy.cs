using JetBrains.Annotations;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Enemy : Entity
{
    public int health = 1;

    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectDuration = 0.25f;

        private void OnEnable()
        {
            // 订阅节拍事件
            BeatManager.OnBeat += OnBeatTriggered;
        }

        private void OnDisable()
        {
            // 取消订阅，防止内存泄漏或错误调用
            BeatManager.OnBeat -= OnBeatTriggered;
        }

        private void OnBeatTriggered()
        {
            // 这个方法在每个节拍被调用
            // 在这里执行敌人的行动逻辑（例如之前的 PerformTurn 或 PerformAction）
            PerformAction(); // 假设你有一个执行行动的方法
        }
        public virtual void PerformAction()
        {
            // 这里放置敌人每个节拍要执行的逻辑
            // 例如，随机选择一个方向移动，或者攻击玩家等
        }

    // 使用 Enemy/子类调用 TryMove 时可收到移动回调
    public new bool TryMove(Vector2Int direction)
    {
        Vector2Int oldPos = GridPosition;
        bool moved = base.TryMove(direction);
        if (moved)
        {
            OnMovedByTryMove(oldPos, GridPosition);
        }

        return moved;
    }

    protected virtual void OnMovedByTryMove(Vector2Int oldPos, Vector2Int newPos)
    {
    }

    public virtual void BossGotHit()
    {
        // Boss特有的受击逻辑
    }

    public override void Die()
    {
        Vector3 deathPosition = transform.position;
        Quaternion deathRotation = transform.rotation;

        base.Die();

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, deathPosition, deathRotation);
            if (deathEffectDuration > 0f)
            {
                Destroy(effect, deathEffectDuration);
            }
        }
    }

    // 被攻击时的响应
    public override void Onhit(Vector2Int attackDirection)
    {
        if (GridManager == null)
        {
            return;
        }

        Vector2Int targetPos = GridPosition + attackDirection;
        if (!GridManager.IsValidPosition(targetPos))
        {
            return;
        }

        Entity target = GridManager.GetOccupant(targetPos);
        if (target is Box || target is Player || target is Firewall)
        {
            return;
        }


        if (target is Enemy enemy)
        {
            if (enemy is Boss boss)
            {
                boss.BossGotHit();
            }
            else
            {
                enemy.Die();
            }
            Die();
                return;
        }

        TryMove(attackDirection);
    }
}
