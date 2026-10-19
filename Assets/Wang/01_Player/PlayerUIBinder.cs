using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerUIBinder : MonoBehaviour
{
    [SerializeField] private UISlider m_refPlayerHPSlider;
    private Player m_refPlayer;

    private async UniTaskVoid Start()
    {
        if (await UniTask.WaitUntil(() =>
        {
            if (GameManager.m_Instance == null)
                return false;

            m_refPlayer = GameManager.m_Instance.GetLocalPlayer();
            return m_refPlayer != null;
        }, cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow())
            return;
    }
}
