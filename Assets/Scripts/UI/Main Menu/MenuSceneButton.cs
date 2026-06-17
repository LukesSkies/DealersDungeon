using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Loads a scene when clicked.
public class MenuSceneButton : MonoBehaviour
{
    [Header("Button")]

    [SerializeField] private Button button;

    [Header("Scene")]

    [SerializeField] private string sceneName;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(LoadScene);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(LoadScene);
    }

    // Loads the selected scene.
    private void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}