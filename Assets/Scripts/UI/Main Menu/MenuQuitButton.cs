using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Quits the game when clicked.
public class MenuQuitButton : MonoBehaviour
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
            button.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(QuitGame);
    }

    // Quits in build or stops Play Mode in editor.
    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}