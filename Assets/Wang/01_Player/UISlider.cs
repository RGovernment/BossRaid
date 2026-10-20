using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Slider))]

public class UISlider : MonoBehaviour
{
    private Slider m_refSlider;
    [SerializeField] private TextMeshProUGUI m_refCurHPText;
    //[SerializeField] private TextMeshProUGUI m_refMaxHPText;
    
    [SerializeField, Min(0f)] private float m_fLerpDuration = 0.2f;

    private void Awake()
    {
        m_refSlider = GetComponent<Slider>();
    }
   
    public void OnChangeValue(float _fCurrentHP, float _fMaxHP)
    {
        float fRatio = _fMaxHP > 0f ? Mathf.Clamp01(_fCurrentHP / _fMaxHP) : 0f;
        float fTargetValue = Mathf.Lerp(m_refSlider.minValue, m_refSlider.maxValue, fRatio);
        m_refSlider.DOKill();
        m_refSlider.DOValue(fTargetValue, m_fLerpDuration).SetEase(Ease.Linear).SetLink(gameObject);

        m_refCurHPText.text = _fCurrentHP.ToString();
    }

}
