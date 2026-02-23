using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : Entity
{
    [SerializeField]private string nextSceneName;  //下一关的场景名称
    private bool active = false;

    private void Update()
    {
        CheckCompletion();       //每帧检查关卡是否完成
    }

    private void CheckCompletion()       //检查是否完成关卡
    {
        if (GridManager == null)
        {
            return;
        }

        // 遍历Tilemap上的有效格子
        foreach (Vector2Int checkPos in GridManager.GetValidPositions())
        {
            if (GridManager.GetOccupant(checkPos) is Boss || GridManager.GetOccupant(checkPos) is Firewall)
            {
                active = false;  //如果还有Boss或Firewall，关卡未完成
                return;
            }
        }
        active = true;
    }


    public override void Onhit(Vector2Int attackDirection)
    {
        if (active == true)
        {
            SceneManager.LoadSceneAsync(nextSceneName,LoadSceneMode.Single);  //加载下一关场景
        }
        else
        {
            Debug.Log("关卡未完成");          //关卡未完成，不能进入下一关
                                              //Todo:可以考虑添加一些提示UI
            return;
        }
    }
}
