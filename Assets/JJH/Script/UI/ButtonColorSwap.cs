using UnityEngine;
using UnityEngine.UI;

using SF = UnityEngine.SerializeField;

public class ButtonColorSwap : ClickButton
{

    [SF] private Image readyHighlight;

    [SF] private Sprite readyHighlightOnImage;
    [SF] private Sprite readyHighlightOffImage;

    protected override void BtnColorChange()
    {
        base.BtnColorChange();

        if (btn.image.sprite == NormalColor)
        {
            readyHighlight.sprite = readyHighlightOffImage;
        }

        else if (btn.image.sprite == PressedColor)
        {
            readyHighlight.sprite = readyHighlightOnImage;
        }
    }
}
