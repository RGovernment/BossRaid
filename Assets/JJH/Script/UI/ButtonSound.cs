using UnityEngine;
using UnityEngine.EventSystems;
using SF = UnityEngine.SerializeField;
public class ButtonSound : MonoBehaviour, IPointerUpHandler, IPointerEnterHandler
{
    [SF] private SOAudio mouseClick;
    [SF] private SOAudio mouseHover;


    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.m_Instance.PlaySfx(mouseHover);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SoundManager.m_Instance.PlaySfx(mouseClick);
    }
}
