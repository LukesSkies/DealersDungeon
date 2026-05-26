using UnityEngine;
using UnityEngine.UI;

public class DiscordButton : MonoBehaviour
{
    [Header("Discord Link")]
    [Tooltip("Put your Discord invite link here.")]
    [SerializeField] private string discordInviteURL = "https://discord.gg/2g4r3ZC3h2";

    [Header("Button Reference")]
    [Tooltip("Optional. If left empty, this script will try to find a Button on this GameObject.")]
    [SerializeField] private Button discordButton;

    private void Awake()
    {
        // If no button was assigned in the Inspector, try to get one from this GameObject
        if (discordButton == null)
        {
            discordButton = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        // Add the click listener when this object becomes active
        if (discordButton != null)
        {
            discordButton.onClick.AddListener(OpenDiscord);
        }
        else
        {
            Debug.LogWarning("DiscordButton: No Button component found or assigned.");
        }
    }

    private void OnDisable()
    {
        // Remove the click listener to avoid duplicate calls or memory issues
        if (discordButton != null)
        {
            discordButton.onClick.RemoveListener(OpenDiscord);
        }
    }

    public void OpenDiscord()
    {
        // Make sure the link is not empty
        if (string.IsNullOrWhiteSpace(discordInviteURL))
        {
            Debug.LogWarning("DiscordButton: Discord invite URL is empty.");
            return;
        }

        // Opens the Discord invite in the player's default browser
        Application.OpenURL(discordInviteURL);
    }
}