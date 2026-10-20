using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerUIBinder : MonoBehaviour
{
    [SerializeField] private UISlider m_refPlayerHPSlider;

    //[SerializeField] private 
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

        m_refPlayer.OnTakeDamage += OnChangeValue;
        OnChangeValue(m_refPlayer.ObjectInfo.CurrentHP, m_refPlayer.ObjectInfo.MaxHP);
    }

    private void OnDestroy()
    {
        if (m_refPlayer != null)
            m_refPlayer.OnTakeDamage -= OnChangeValue;
    }

    private void OnChangeValue(float _fCurrentHP, float _fMaxHP)
    {
        m_refPlayerHPSlider.OnChangeValue(_fCurrentHP, _fMaxHP);
    }
}
