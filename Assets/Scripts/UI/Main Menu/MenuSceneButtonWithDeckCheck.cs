using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Loads a scene only if the player has selected cards.
public class MenuSceneButtonWithDeckCheck : MonoBehaviour
{
    [Header("Button")]

    [SerializeField] private Button button;

    [Header("Scene")]

    // Scene to load.
    [SerializeField] private string sceneName;

    [Header("Deck Requirement")]

    // If true, the deck must be full before loading.
    [SerializeField] private bool requireFullDeck = false;

    [Header("Optional Warning Text")]

    // Text used to show warning messages.
    [SerializeField] private TMP_Text warningText;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(TryLoadScene);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(TryLoadScene);
    }

    // Checks deck rules, then loads the scene.
    private void TryLoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            ShowWarning("Scene name is empty.");
            return;
        }

        if (DeckRuntimeManager.Instance == null)
        {
            ShowWarning("No DeckRuntimeManager found.");
            return;
        }

        int selectedCount = DeckRuntimeManager.Instance.GetSelectedCount();

        if (selectedCount <= 0)
        {
            ShowWarning("Choose at least 1 card first.");
            return;
        }

        if (requireFullDeck && selectedCount < DeckRuntimeManager.Instance.MaxDeckSize)
        {
            ShowWarning("Choose 5 cards first.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // Shows a warning message.
    private void ShowWarning(string message)
    {
        if (warningText != null)
            warningText.text = message;

        Debug.LogWarning(message);
    }
}