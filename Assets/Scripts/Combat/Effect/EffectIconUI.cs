using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Controls one effect icon in the UI.
public class EffectIconUI : MonoBehaviour
{
    // Icon image.
    [SerializeField] private Image icon;

    // Duration text.
    [SerializeField] private TMP_Text durationText;

    // Sets the icon and duration.
    public void Setup(Sprite sprite, int duration)
    {
        icon.sprite = sprite;

        if (duration > 0)
            durationText.text = duration.ToString();
        else
            durationText.text = "";
    }
}