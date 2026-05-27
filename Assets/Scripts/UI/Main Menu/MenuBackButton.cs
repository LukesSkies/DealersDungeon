using UnityEngine;
using UnityEngine.UI;

// Attach this to any Back button.
//
// It uses the same navigation history as the Esc key.
public class MenuBackButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(GoBack);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(GoBack);
    }

    private void GoBack()
    {
        if (MenuNavigator.Instance == null)
        {
            Debug.LogError("No MenuNavigator found.");
            return;
        }

        MenuNavigator.Instance.GoBack();
    }
}