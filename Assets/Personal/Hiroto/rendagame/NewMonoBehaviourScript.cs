using UnityEngine;
using UnityEngine.InputSystem;

public class RandomClickTarget : MonoBehaviour
{
    ////////////////////////////////
    // 最小クリック数
    ////////////////////////////////

    public int minClick = 10;


    ////////////////////////////////
    // 最大クリック数
    ////////////////////////////////

    public int maxClick = 100;


    ////////////////////////////////
    // ランダムで決まった目標数
    ////////////////////////////////

    private int targetClick;


    ////////////////////////////////
    // 現在のクリック数
    ////////////////////////////////

    private int currentClick = 0;


    ////////////////////////////////
    // クリア状態
    ////////////////////////////////

    private bool clear = false;


    ////////////////////////////////
    // 開始時
    ////////////////////////////////

    void Start()
    {
        ////////////////////////////////
        // 目標クリック数を作成
        ////////////////////////////////

        targetClick = Random.Range(minClick, maxClick + 1);


        Debug.Log("目標クリック数：" + targetClick);
    }


    ////////////////////////////////
    // 毎フレーム確認
    ////////////////////////////////

    void Update()
    {
        ////////////////////////////////
        // クリックまたはスペース検知
        ////////////////////////////////

        if (Mouse.current.leftButton.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ////////////////////////////////
            // クリック数増加
            ////////////////////////////////

            currentClick++;

            Debug.Log("現在：" + currentClick + "/" + targetClick);


            ////////////////////////////////
            // 目標達成
            ////////////////////////////////

            if (currentClick >= targetClick)
            {
                clear = true;
                Debug.Log("閉じる");
            }
        }
    }
}