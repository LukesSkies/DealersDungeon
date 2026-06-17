using UnityEngine;

// Marks a UI panel as a menu screen.
public class MenuScreen : MonoBehaviour
{
    [Header("Screen")]

    public MenuScreenId screenId;

    [Tooltip("Optional. If empty, this GameObject will be enabled/disabled.")]
    [SerializeField] private GameObject rootObject;

    private void Awake()
    {
        if (rootObject == null)
            rootObject = gameObject;
    }

    // Shows or hides this screen.
    public void SetVisible(bool visible)
    {
        if (rootObject != null)
            rootObject.SetActive(visible);
    }

    // Checks if this screen is visible.
    public bool IsVisible()
    {
        return rootObject != null && rootObject.activeSelf;
    }
}