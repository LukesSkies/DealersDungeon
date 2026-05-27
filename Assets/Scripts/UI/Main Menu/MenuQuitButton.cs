using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Attach this to the Quit button.
//
// In the Unity Editor, it stops Play Mode.
// In a build, it closes the game.
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

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}