using System.Collections.Generic;
using UnityEngine;

// This controls all menu navigation.
//
// It handles:
// - opening screens
// - closing screens
// - back buttons
// - Esc key back navigation
// - screen history
public class MenuNavigator : MonoBehaviour
{
    public static MenuNavigator Instance;

    [Header("Screens")]
    [SerializeField] private List<MenuScreen> screens = new List<MenuScreen>();

    [Header("Start Screen")]
    [SerializeField] private MenuScreenId startScreen = MenuScreenId.MainMenu;

    [Header("Input")]
    [SerializeField] private bool escapeGoesBack = true;

    [Header("Debug")]
    [SerializeField] private bool logNavigation = false;

    private Dictionary<MenuScreenId, MenuScreen> screenLookup = new Dictionary<MenuScreenId, MenuScreen>();
    private Stack<MenuScreenId> history = new Stack<MenuScreenId>();

    private MenuScreenId currentScreen;
    private bool hasCurrentScreen = false;

    public MenuScreenId CurrentScreen => currentScreen;

    private void Awake()
    {
        Instance = this;

        BuildScreenLookup();
    }

    private void Start()
    {
        OpenScreenWithoutHistory(startScreen);
        ClearHistory();
    }

    private void Update()
    {
        if (!escapeGoesBack)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            GoBack();
    }

    private void BuildScreenLookup()
    {
        screenLookup.Clear();

        foreach (MenuScreen screen in screens)
        {
            if (screen == null)
                continue;

            if (screenLookup.ContainsKey(screen.screenId))
            {
                Debug.LogWarning($"Duplicate menu screen found: {screen.screenId}");
                continue;
            }

            screenLookup.Add(screen.screenId, screen);
        }
    }

    public void OpenScreen(MenuScreenId targetScreen)
    {
        OpenScreen(targetScreen, true);
    }

    public void OpenScreenWithoutHistory(MenuScreenId targetScreen)
    {
        OpenScreen(targetScreen, false);
    }

    private void OpenScreen(MenuScreenId targetScreen, bool addCurrentToHistory)
    {
        if (!screenLookup.ContainsKey(targetScreen))
        {
            Debug.LogError($"No MenuScreen found for {targetScreen}. Add it to MenuNavigator screens list.");
            return;
        }

        if (hasCurrentScreen && currentScreen == targetScreen)
            return;

        if (addCurrentToHistory && hasCurrentScreen)
            history.Push(currentScreen);

        ShowOnly(targetScreen);

        currentScreen = targetScreen;
        hasCurrentScreen = true;

        if (logNavigation)
            Debug.Log($"Opened screen: {targetScreen}");
    }

    public void GoBack()
    {
        if (history.Count <= 0)
        {
            if (hasCurrentScreen && currentScreen != startScreen)
            {
                OpenScreenWithoutHistory(startScreen);
                return;
            }

            return;
        }

        MenuScreenId previousScreen = history.Pop();

        ShowOnly(previousScreen);

        currentScreen = previousScreen;
        hasCurrentScreen = true;

        if (logNavigation)
            Debug.Log($"Went back to screen: {previousScreen}");
    }

    public void ClearHistory()
    {
        history.Clear();
    }

    public void ResetToMainMenu()
    {
        ClearHistory();
        OpenScreenWithoutHistory(startScreen);
    }

    private void ShowOnly(MenuScreenId screenToShow)
    {
        foreach (KeyValuePair<MenuScreenId, MenuScreen> pair in screenLookup)
        {
            bool shouldShow = pair.Key == screenToShow;
            pair.Value.SetVisible(shouldShow);
        }
    }
}