using UnityEngine;
using TMPro;
using UnityEngine.UI;

// This script controls one effect icon in the UI.
//
// It shows:
// - the effect's icon sprite
// - the effect's remaining duration
public class EffectIconUI : MonoBehaviour
{
    // UI Image that displays the effect icon.
    [SerializeField] private Image icon;

    // Text that displays how many turns/duration the effect has left.
    [SerializeField] private TMP_Text durationText;

    // Sets up the icon display.
    //
    // sprite = icon image for the effect.
    // duration = how long the effect has left.
    public void Setup(Sprite sprite, int duration)
    {
        // Set the icon image.
        icon.sprite = sprite;

        // Show duration only if it is greater than 0.
        //
        // Instant effects can leave this blank.
        if (duration > 0)
            durationText.text = duration.ToString();
        else
            durationText.text = "";
    }
}