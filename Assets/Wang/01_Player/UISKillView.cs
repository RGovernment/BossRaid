using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UISKillView : MonoBehaviour
{
    private sealed class SkillView
    {
        public Image m_refCoolView;
        public float m_fMaxCoolTime;
    }

    private readonly Dictionary<int, SkillView> m_hashCoolTime = new();

    [SerializeField] private List<Image> m_listCoolView = new();

    private Player m_refLocalPlayer = null;
    private async UniTaskVoid Start()
    {
        if (await UniTask.WaitUntil(() =>
        {
            if (GameManager.m_Instance == null || SkillDataLoader.Instance == null ||
                SkillDataLoader.Instance.SkillData == null)
                return false;

            m_refLocalPlayer = GameManager.m_Instance.GetLocalPlayer();
            return m_refLocalPlayer != null &&
                m_refLocalPlayer.SkillSpawner.localCooldownList.Count > 0;
        }, cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow())
            return;

        int[] arrSkillIDs = { 1001, 1002, 1000, 1003, 1004, 1005 };
        int iCount = Mathf.Min(m_listCoolView.Count, arrSkillIDs.Length);
        for (int i = 0; i < iCount; ++i)
        {
            int iID = arrSkillIDs[i];
            Image refImage = m_listCoolView[i];
            if (refImage == null || !SkillDataLoader.Instance.SkillData.TryGetValue(iID, out SkillData refData))
            {
                Debug.LogWarning($"스킬 {iID}의 쿨타임 이미지 또는 데이터가 연결되지 않았습니다.", this);
                continue;
            }

            m_hashCoolTime.Add(iID, new SkillView
            {
                m_refCoolView = refImage,
                m_fMaxCoolTime = refData.Cooldown
            });
            refImage.fillAmount = 0f;
        }
    }

    private void Update()
    {
        if (m_refLocalPlayer == null || m_hashCoolTime.Count == 0)
            return;

        var hashSkillCool = m_refLocalPlayer.SkillSpawner.localCooldownList;
        foreach (var tValue in m_hashCoolTime)
        {
            SkillView refView = tValue.Value;
            float fRemainTime = hashSkillCool.TryGetValue(tValue.Key, out var tCooldown) ? tCooldown.cooldown : 0f;

            refView.m_refCoolView.fillAmount = Mathf.Clamp01(fRemainTime / refView.m_fMaxCoolTime);
        }
    }

}
