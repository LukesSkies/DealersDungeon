using UnityEngine;
using UnityEngine.UI;

// Makes a button go back in the menu.
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

    // Goes back using MenuNavigator.
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