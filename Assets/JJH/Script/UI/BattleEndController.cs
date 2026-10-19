using Cysharp.Threading.Tasks;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Constant_JJH;

using SF = UnityEngine.SerializeField;
public class BattleEndController : MonoBehaviour
{
    private GameManager GameManager => GameManager.m_Instance;

    [Header("스크립트")]
    [SF] private TestPortal portal;

    [Header("패널")]
    [SF] private CanvasGroup bttleEndPanel;

    [Header("이미지")]
    [SF] private Image winImage;
    [SF] private Image defeatImage;

    [Header("텍스트")]
    [SF] private TextMeshProUGUI timerText;

    [Header("GUI 직렬화")]
    [SF] private HorizontalLayoutGroup checkList;
    [SF] private List<Toggle> toggles;


    [Header("타이머 수치")]
    [SF] private int BattleTimerNormal = 60;
    [SF] private int BattleTimerSkip = 5;

    private float nowBattleEndTimer;
    private CancellationTokenSource endTimerLoopToken;
    private List<(Toggle toggle, Player player)> toggleGroup;

    private void Awake()
    {
        toggleGroup = new();
    }

    private void OnEnable()
    {
        portal.OnPlayerInPortal += PlayerReadyCheck;
    }

    private void OnDisable()
    {
        portal.OnPlayerInPortal -= PlayerReadyCheck;
    }

    private void PanelOpen()
    {
        //상태 초기화
        toggleGroup.Clear();
        foreach (Toggle toggle in toggles)
            toggle.gameObject.SetActive(false);
        
        for(int i = 0; i < GameManager.Players.Count; i++)
        {
            toggles[i].gameObject.SetActive(true);
            toggleGroup.Add((toggles[i], GameManager.Players[i]));
        }

    }

    private void PlayerReadyCheck(Player player)
    {
        if (player == null) return;
        int insideCount = 0;
        toggleGroup.ForEach(toggle =>
        {
            if (toggle.player == player)
            {
                toggle.toggle.isOn = true;
                insideCount++;
            }
        });

        int requiredCount = Mathf.CeilToInt(GameManager.Players.Count / 2f);

        if (insideCount >= requiredCount)
            TimerStart(BattleTimerNormal);
        
        else if (insideCount == GameManager.Players.Count) 
            TimerStart(BattleTimerSkip);
        
    }

    private void TimerStart(int time)
    {
        StopCooldownSyncLoop();
        endTimerLoopToken = new CancellationTokenSource();
        double endTime = PhotonNetwork.Time + time;

        BattleEndTimer(endTime, endTimerLoopToken.Token).Forget();
    }

    private async UniTaskVoid BattleEndTimer(double time, CancellationToken cancellationToken)
    {
        if (timerText == null) return;

        try
        {
            

            while (!cancellationToken.IsCancellationRequested)
            {
                int remainingSeconds = 
                    Mathf.Max(0, Mathf.CeilToInt((float)(time - PhotonNetwork.Time)));

                timerText.SetText(remainingSeconds.ToString());

                if (remainingSeconds <= 0) break;

                await UniTask.Delay(MILLISECOND_BASE, DelayType.DeltaTime, 
                    PlayerLoopTiming.Update, cancellationToken);
            }

            // 씬 이동 작동
        }
        catch (OperationCanceledException)
        {
            Debug.Log("타이머 종료");
        }
        catch (Exception ex) 
        {
            Debug.LogError($"예기치 못한 오류가 발생했습니다. : {ex.Message}");
        }
    }

    public void StopCooldownSyncLoop()
    {
        if (endTimerLoopToken == null) return;
        endTimerLoopToken.Cancel();
        endTimerLoopToken.Dispose();
        endTimerLoopToken = null;
    }

}
