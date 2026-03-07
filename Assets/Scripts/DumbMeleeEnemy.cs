using UnityEngine;

public class DumbMeleeEnemy : MeleeEnemy
{
    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [SerializeField] public MoveDirection moveDirection = MoveDirection.Left;

    public override void Awake()
    {
        base.Awake();
        if (pendingDirection.HasValue)
        {
            if (moveDirection == MoveDirection.Left)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (moveDirection == MoveDirection.Right)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }


    protected override void ChooseDirection()
    {
        // 固定单向：每次抬手都选择同一方向
        switch (moveDirection)
        {
            case MoveDirection.Up:
                pendingDirection = Vector2Int.up;
                break;
            case MoveDirection.Down:
                pendingDirection = Vector2Int.down;
                break;
            case MoveDirection.Right:
                pendingDirection = Vector2Int.right;
                break;
            default:
                pendingDirection = Vector2Int.left;
                break;
        }
    }

    protected override void OnInvalidTargetDuringAction(Vector2Int targetPos, Vector2Int direction)
    {
        Die();
    }

    protected override void OnBlockedByNonPlayerDuringAction(Entity occupant, Vector2Int direction)
    {
        Die();
    }
}
