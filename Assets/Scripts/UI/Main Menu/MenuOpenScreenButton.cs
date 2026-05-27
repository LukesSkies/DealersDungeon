using UnityEngine;
using UnityEngine.UI;

// Attach this to a UI Button when clicking it should open another menu screen.
public class MenuOpenScreenButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Navigation")]
    [SerializeField] private MenuScreenId screenToOpen;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(OpenScreen);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OpenScreen);
    }

    private void OpenScreen()
    {
        if (MenuNavigator.Instance == null)
        {
            Debug.LogError("No MenuNavigator found.");
            return;
        }

        MenuNavigator.Instance.OpenScreen(screenToOpen);
    }
}