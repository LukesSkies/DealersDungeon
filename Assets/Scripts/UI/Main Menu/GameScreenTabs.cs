using UnityEngine;
using UnityEngine.UI;

// These are the tabs inside the Game Screen.
public enum GameScreenTab
{
    Main,
    Training,
    Endless
}

// This script controls the Game Screen.
//
// Setup is:
// - Each tab panel has its own copy of the tab buttons.
//
// This script lets you reference those separate buttons directly in the Inspector.
//
// It also lets the player press:
// - Q to move one tab left
// - E to move one tab right
public class GameScreenTabs : MonoBehaviour
{
    [Header("Tab Panels")]

    // The Main tab panel.
    //
    // This should be the whole GameObject that contains the Main tab UI.
    // It will be enabled when the Main tab is active.
    [SerializeField] private GameObject mainPanel;

    // The Training tab panel.
    //
    // This should be the whole GameObject that contains the Training tab UI.
    // It will be enabled when the Training tab is active.
    [SerializeField] private GameObject trainingPanel;

    // The Endless tab panel.
    //
    // This should be the whole GameObject that contains the Endless tab UI.
    // It will be enabled when the Endless tab is active.
    [SerializeField] private GameObject endlessPanel;

    [Header("Buttons On Main Tab")]

    // This is the Training button inside the Main tab.
    //
    // Clicking this button opens the Training tab.
    [SerializeField] private Button mainTabTrainingButton;

    // This is the Endless button inside the Main tab.
    //
    // Clicking this button opens the Endless tab.
    [SerializeField] private Button mainTabEndlessButton;

    [Header("Buttons On Training Tab")]

    // This is the Main button inside the Training tab.
    //
    // Clicking this button opens the Main tab.
    [SerializeField] private Button trainingTabMainButton;

    // This is the Endless button inside the Training tab.
    //
    // Clicking this button opens the Endless tab.
    [SerializeField] private Button trainingTabEndlessButton;

    [Header("Buttons On Endless Tab")]

    // This is the Main button inside the Endless tab.
    //
    // Clicking this button opens the Main tab.
    [SerializeField] private Button endlessTabMainButton;

    // This is the Training button inside the Endless tab.
    //
    // Clicking this button opens the Training tab.
    [SerializeField] private Button endlessTabTrainingButton;

    [Header("Starting Tab")]

    // The tab that should be shown whenever the Game Screen opens.
    //
    // Usually this should be Main.
    [SerializeField] private GameScreenTab startingTab = GameScreenTab.Main;

    // Stores which tab is currently active.
    //
    // This is used by Q/E navigation so the script knows which tab comes next.
    private GameScreenTab currentTab;

    private void OnEnable()
    {
        // Add button click events when this Game Screen becomes active.
        //
        // OnEnable is used because the Game Screen may be turned on/off
        // by the menu navigation system.
        AddButtonListeners();

        // Show the starting tab whenever the Game Screen opens.
        ShowTab(startingTab);
    }

    private void OnDisable()
    {
        // Remove button click events when this Game Screen is disabled.
        //
        // This prevents duplicate listeners from being added if the screen
        // is opened and closed multiple times.
        RemoveButtonListeners();
    }

    private void Update()
    {
        // Q moves one tab left.
        if (Input.GetKeyDown(KeyCode.Q))
            PreviousTab();

        // E moves one tab right.
        if (Input.GetKeyDown(KeyCode.E))
            NextTab();
    }

    // Adds click events to every tab navigation button.
    //
    // These are separate because each tab has its own button copies.
    private void AddButtonListeners()
    {
        // Main tab buttons.
        if (mainTabTrainingButton != null)
            mainTabTrainingButton.onClick.AddListener(ShowTraining);

        if (mainTabEndlessButton != null)
            mainTabEndlessButton.onClick.AddListener(ShowEndless);

        // Training tab buttons.
        if (trainingTabMainButton != null)
            trainingTabMainButton.onClick.AddListener(ShowMain);

        if (trainingTabEndlessButton != null)
            trainingTabEndlessButton.onClick.AddListener(ShowEndless);

        // Endless tab buttons.
        if (endlessTabMainButton != null)
            endlessTabMainButton.onClick.AddListener(ShowMain);

        if (endlessTabTrainingButton != null)
            endlessTabTrainingButton.onClick.AddListener(ShowTraining);
    }

    // Removes click events from every tab navigation button.
    //
    // This keeps the script clean and prevents click events from stacking up.
    private void RemoveButtonListeners()
    {
        // Main tab buttons.
        if (mainTabTrainingButton != null)
            mainTabTrainingButton.onClick.RemoveListener(ShowTraining);

        if (mainTabEndlessButton != null)
            mainTabEndlessButton.onClick.RemoveListener(ShowEndless);

        // Training tab buttons.
        if (trainingTabMainButton != null)
            trainingTabMainButton.onClick.RemoveListener(ShowMain);

        if (trainingTabEndlessButton != null)
            trainingTabEndlessButton.onClick.RemoveListener(ShowEndless);

        // Endless tab buttons.
        if (endlessTabMainButton != null)
            endlessTabMainButton.onClick.RemoveListener(ShowMain);

        if (endlessTabTrainingButton != null)
            endlessTabTrainingButton.onClick.RemoveListener(ShowTraining);
    }

    // Opens the Main tab.
    //
    // This can be called by:
    // - Training tab's Main button
    // - Endless tab's Main button
    // - Q/E navigation
    public void ShowMain()
    {
        ShowTab(GameScreenTab.Main);
    }

    // Opens the Training tab.
    //
    // This can be called by:
    // - Main tab's Training button
    // - Endless tab's Training button
    // - Q/E navigation
    public void ShowTraining()
    {
        ShowTab(GameScreenTab.Training);
    }

    // Opens the Endless tab.
    //
    // This can be called by:
    // - Main tab's Endless button
    // - Training tab's Endless button
    // - Q/E navigation
    public void ShowEndless()
    {
        ShowTab(GameScreenTab.Endless);
    }

    // Moves one tab to the right.
    //
    // Order:
    // Main -> Training -> Endless -> Main
    public void NextTab()
    {
        int currentIndex = (int)currentTab;
        int nextIndex = currentIndex + 1;

        // If we go past the last tab, wrap back to the first tab.
        if (nextIndex > (int)GameScreenTab.Endless)
            nextIndex = 0;

        ShowTab((GameScreenTab)nextIndex);
    }

    // Moves one tab to the left.
    //
    // Order:
    // Main <- Training <- Endless <- Main
    public void PreviousTab()
    {
        int currentIndex = (int)currentTab;
        int previousIndex = currentIndex - 1;

        // If we go before the first tab, wrap to the last tab.
        if (previousIndex < 0)
            previousIndex = (int)GameScreenTab.Endless;

        ShowTab((GameScreenTab)previousIndex);
    }

    // Shows one tab panel and hides the others.
    //
    // This is the core function that actually swaps tabs.
    public void ShowTab(GameScreenTab tab)
    {
        // Store the current tab.
        currentTab = tab;

        // Show only the Main panel when Main is selected.
        if (mainPanel != null)
            mainPanel.SetActive(tab == GameScreenTab.Main);

        // Show only the Training panel when Training is selected.
        if (trainingPanel != null)
            trainingPanel.SetActive(tab == GameScreenTab.Training);

        // Show only the Endless panel when Endless is selected.
        if (endlessPanel != null)
            endlessPanel.SetActive(tab == GameScreenTab.Endless);
    }
}