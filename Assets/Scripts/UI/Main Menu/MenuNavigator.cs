using System.Collections.Generic;
using UnityEngine;

// Controls menu screen navigation.
public class MenuNavigator : MonoBehaviour
{
    public static MenuNavigator Instance;

    [Header("Screens")]

    // All menu screens.
    [SerializeField] private List<MenuScreen> screens = new List<MenuScreen>();

    [Header("Start Screen")]

    // Screen shown first.
    [SerializeField] private MenuScreenId startScreen = MenuScreenId.MainMenu;

    [Header("Input")]

    // If true, Escape goes back.
    [SerializeField] private bool escapeGoesBack = true;

    [Header("Debug")]

    // Logs menu navigation.
    [SerializeField] private bool logNavigation = false;

    // Fast screen lookup.
    private Dictionary<MenuScreenId, MenuScreen> screenLookup = new Dictionary<MenuScreenId, MenuScreen>();

    // Previous screens.
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

    // Builds screen lookup from the list.
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

    // Opens a screen and saves history.
    public void OpenScreen(MenuScreenId targetScreen)
    {
        OpenScreen(targetScreen, true);
    }

    // Opens a screen without saving history.
    public void OpenScreenWithoutHistory(MenuScreenId targetScreen)
    {
        OpenScreen(targetScreen, false);
    }

    // Opens a screen.
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

    // Goes back to the previous screen.
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

    // Clears screen history.
    public void ClearHistory()
    {
        history.Clear();
    }

    // Resets back to the start screen.
    public void ResetToMainMenu()
    {
        ClearHistory();
        OpenScreenWithoutHistory(startScreen);
    }

    // Shows one screen and hides the rest.
    private void ShowOnly(MenuScreenId screenToShow)
    {
        foreach (KeyValuePair<MenuScreenId, MenuScreen> pair in screenLookup)
        {
            bool shouldShow = pair.Key == screenToShow;
            pair.Value.SetVisible(shouldShow);
        }
    }
}