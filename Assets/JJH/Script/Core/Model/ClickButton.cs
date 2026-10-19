using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using SF = UnityEngine.SerializeField;

[RequireComponent(typeof(Button))]
public abstract class ClickButton : MonoBehaviour
{
    [Header("헤더용")]
    [SF] protected Sprite PressedColor;
    [SF] protected Sprite NormalColor;
    [SF] protected Color pressColorToPressed;
    [SF] protected Color pressColorToNormal;

    protected Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
    }

    private void OnEnable()
    {
        btn.onClick.AddListener(BtnColorChange);
    }

    private void OnDisable()
    {
        btn.onClick.RemoveAllListeners();
    }

    protected virtual void BtnColorChange()
    {
        if (btn.image.sprite == NormalColor)
        {
            btn.image.sprite = PressedColor;
            ColorBlock colors = btn.colors;
            colors.pressedColor = pressColorToPressed;
            btn.colors = colors;
        }

        else if (btn.image.sprite == PressedColor)
        {
            btn.image.sprite = NormalColor;
            ColorBlock colors = btn.colors;
            colors.pressedColor = pressColorToNormal;
            btn.colors = colors;
        }

    }
}
