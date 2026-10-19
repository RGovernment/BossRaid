using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Slider))]

public class UISlider : MonoBehaviour
{
    //여기서 넣어줘야하나
    private Slider m_refSlider;
    private float m_fLerpDuration;

    private CancellationToken m_ctsLerp;
    private void Awake()
    {
        m_refSlider = GetComponent<Slider>();
    }
   
    private void OnChangeValue(float _fCurRatio)
    {
        float fMaxValue = m_refSlider.maxValue;
        LerpSliderAsync(_fCurRatio, fMaxValue, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid LerpSliderAsync(float _fStartFill, float _fEndFill, CancellationToken _ct)
    {
        float fElapsed = 0f;

        while (fElapsed < m_fLerpDuration)
        {
            fElapsed += Time.deltaTime;

            // 0에서 1 사이의 진행 비율 계산 (Clamped)
            float fProgress = Mathf.Clamp01(fElapsed / m_fLerpDuration);

            // 시작 fillAmount에서 목표 fillAmount까지 보간
            m_refSlider.value = Mathf.Lerp(_fStartFill, _fEndFill, fProgress);

            await UniTask.Yield(_ct);
        }

        m_refSlider.value = _fEndFill;
    }

}
