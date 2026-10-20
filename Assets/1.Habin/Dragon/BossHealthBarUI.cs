using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private DragonBoss boss;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text hpText;


    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (healthSlider == null)
            return;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.wholeNumbers = false;
        healthSlider.interactable = false;
    }

    private void OnEnable()
    {
        if (boss == null)
            return;

        boss.OnHpChanged += Refresh;
        Refresh();
    }

    private void Start()
    {
        if (boss == null || healthSlider == null)
        {
            Debug.LogError("Boss와 Health Slider를 연결하세요.", this);
            enabled = false;
            return;
        }

        // 보스의 Awake 초기화가 끝난 후 최초 표시합니다.
        Refresh();
    }

    private void OnDisable()
    {
        if (boss != null)
            boss.OnHpChanged -= Refresh;
    }

    private void Refresh()
    {
        if (boss == null || healthSlider == null || boss.MaxHp <= 0f)
            return;

        float healthRatio = Mathf.Clamp01(boss.CurrentHp / boss.MaxHp);
        healthSlider.SetValueWithoutNotify(healthRatio);

        if (hpText != null)
            hpText.text = $"Dragon {boss.CurrentHp:0} / {boss.MaxHp:0}";
    }
}