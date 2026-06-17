using UnityEngine;
using UnityEngine.UI;

// Opens a Discord invite link.
public class DiscordButton : MonoBehaviour
{
    [Header("Discord Link")]

    // Discord invite URL.
    [Tooltip("Put your Discord invite link here.")]
    [SerializeField] private string discordInviteURL = "https://discord.gg/JhTryYxMQe";

    [Header("Button Reference")]

    // Button that opens Discord.
    [Tooltip("Optional. If left empty, this script will try to find a Button on this GameObject.")]
    [SerializeField] private Button discordButton;

    private void Awake()
    {
        if (discordButton == null)
        {
            discordButton = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
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
        if (discordButton != null)
        {
            discordButton.onClick.RemoveListener(OpenDiscord);
        }
    }

    // Opens the Discord link in the browser.
    public void OpenDiscord()
    {
        if (string.IsNullOrWhiteSpace(discordInviteURL))
        {
            Debug.LogWarning("DiscordButton: Discord invite URL is empty.");
            return;
        }

        Application.OpenURL(discordInviteURL);
    }
}