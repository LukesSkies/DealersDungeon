using UnityEngine;

// This script marks a UI panel as a navigable menu screen.
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

    public void SetVisible(bool visible)
    {
        if (rootObject != null)
            rootObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return rootObject != null && rootObject.activeSelf;
    }
}