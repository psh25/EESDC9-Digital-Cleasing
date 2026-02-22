using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Boss : Enemy
{
    public void SetBossOccupant()
    { // 占领以Boss为中心的3x3格子
        Vector2Int[] occupantOffset=new Vector2Int[9];
          int index = 0;
          for(int i = 1; i >= -1; i--) {
              for(int j = -1; j <= 1; j++) { 
                  occupantOffset[index++] = new Vector2Int(i, j);
              }
          }
          foreach(var offset in occupantOffset) {
                GridManager.SetOccupant(GridPosition + offset, this);
                Debug.Log($"Boss 占领格子: {GridPosition + offset}");
        }
    }
    public override void Onhit(Vector2Int attackDirection)
    {
        health--;
        Debug.Log($"Boss 受到伤害，剩余血量: {health}");
        if (health <= 0) Die();
    }

    public override void PerformAction()
    {
        // 每拍行动（AI 决策）
        Debug.Log("Boss 行动节拍");
    }
}
