using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 空のGameObject（例: "GameManager"）に付けるスクリプト
public class SortingGameManager : MonoBehaviour
{
    public static SortingGameManager Instance;

    [Header("タイマー設定")]
    [Tooltip("制限時間（秒）。スクショの3.7sのようにここを5にすると5秒制限になる")]
    public float timeLimit = 5f;
    [Tooltip("上部のゲージ。ImageのType=Filledに設定したものをアサイン")]
    public Image timerGaugeFill;

    [Header("ミス設定")]
    public int maxMiss = 2;
    public TMP_Text missText;

    [Header("素材リスト")]
    [Tooltip("シーン上の全DraggableItemをここに登録")]
    public List<DraggableItem> items;

    [Header("結果パネル（任意・無ければ空でOK）")]
    public GameObject clearPanel;
    public GameObject failPanel;

    private float currentTime;
    private int missCount;
    private int remainingItems;
    private bool isPlaying;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        currentTime = timeLimit;
        missCount = 0;
        remainingItems = items.Count;
        isPlaying = true;

        UpdateMissUI();
        if (clearPanel) clearPanel.SetActive(false);
        if (failPanel) failPanel.SetActive(false);

        foreach (var item in items)
        {
            item.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        currentTime -= Time.deltaTime;

        if (timerGaugeFill != null)
        {
            timerGaugeFill.fillAmount = Mathf.Clamp01(currentTime / timeLimit);
        }

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            GameOver(false);
        }
    }

    // DropBoxから呼ばれる：正解した時
    public void OnCorrectMatch(GameObject item)
    {
        if (!isPlaying) return;

        item.SetActive(false); // マッチ成功した素材を非表示に
        remainingItems--;

        if (remainingItems <= 0)
        {
            GameOver(true); // 全部合わさったらクリア
        }
    }

    // DropBoxから呼ばれる：不正解だった時
    public void OnMissMatch()
    {
        if (!isPlaying) return;

        missCount++;
        UpdateMissUI();

        if (missCount >= maxMiss)
        {
            GameOver(false);
        }
    }

    void UpdateMissUI()
    {
        if (missText != null)
        {
            missText.text = $"ミス {missCount}/{maxMiss}";
        }
    }

    void GameOver(bool cleared)
    {
        isPlaying = false;

        if (cleared && clearPanel) clearPanel.SetActive(true);
        if (!cleared && failPanel) failPanel.SetActive(true);
    }
}