using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EffectIconUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text durationText;

    public void Setup(Sprite sprite, int duration)
    {
        icon.sprite = sprite;

        if (duration > 0)
            durationText.text = duration.ToString();
        else
            durationText.text = "";
    }
}