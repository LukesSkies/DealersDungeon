using UnityEngine;
using UnityEngine.UI;

// Tabs inside the Game screen.
public enum GameScreenTab
{
    Main,
    Training,
    Endless
}

// Controls the Game screen tabs.
public class GameScreenTabs : MonoBehaviour
{
    [Header("Tab Panels")]

    // Tab panels.
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject trainingPanel;
    [SerializeField] private GameObject endlessPanel;

    [Header("Buttons On Main Tab")]

    // Buttons shown on the Main tab.
    [SerializeField] private Button mainTabTrainingButton;
    [SerializeField] private Button mainTabEndlessButton;

    [Header("Buttons On Training Tab")]

    // Buttons shown on the Training tab.
    [SerializeField] private Button trainingTabMainButton;
    [SerializeField] private Button trainingTabEndlessButton;

    [Header("Buttons On Endless Tab")]

    // Buttons shown on the Endless tab.
    [SerializeField] private Button endlessTabMainButton;
    [SerializeField] private Button endlessTabTrainingButton;

    [Header("Starting Tab")]

    // Tab shown when the screen opens.
    [SerializeField] private GameScreenTab startingTab = GameScreenTab.Main;

    // Current active tab.
    private GameScreenTab currentTab;

    private void OnEnable()
    {
        AddButtonListeners();

        ShowTab(startingTab);
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
    }

    private void Update()
    {
        // Q moves left.
        if (Input.GetKeyDown(KeyCode.Q))
            PreviousTab();

        // E moves right.
        if (Input.GetKeyDown(KeyCode.E))
            NextTab();
    }

    // Adds button click events.
    private void AddButtonListeners()
    {
        if (mainTabTrainingButton != null)
            mainTabTrainingButton.onClick.AddListener(ShowTraining);

        if (mainTabEndlessButton != null)
            mainTabEndlessButton.onClick.AddListener(ShowEndless);

        if (trainingTabMainButton != null)
            trainingTabMainButton.onClick.AddListener(ShowMain);

        if (trainingTabEndlessButton != null)
            trainingTabEndlessButton.onClick.AddListener(ShowEndless);

        if (endlessTabMainButton != null)
            endlessTabMainButton.onClick.AddListener(ShowMain);

        if (endlessTabTrainingButton != null)
            endlessTabTrainingButton.onClick.AddListener(ShowTraining);
    }

    // Removes button click events.
    private void RemoveButtonListeners()
    {
        if (mainTabTrainingButton != null)
            mainTabTrainingButton.onClick.RemoveListener(ShowTraining);

        if (mainTabEndlessButton != null)
            mainTabEndlessButton.onClick.RemoveListener(ShowEndless);

        if (trainingTabMainButton != null)
            trainingTabMainButton.onClick.RemoveListener(ShowMain);

        if (trainingTabEndlessButton != null)
            trainingTabEndlessButton.onClick.RemoveListener(ShowEndless);

        if (endlessTabMainButton != null)
            endlessTabMainButton.onClick.RemoveListener(ShowMain);

        if (endlessTabTrainingButton != null)
            endlessTabTrainingButton.onClick.RemoveListener(ShowTraining);
    }

    // Opens the Main tab.
    public void ShowMain()
    {
        ShowTab(GameScreenTab.Main);
    }

    // Opens the Training tab.
    public void ShowTraining()
    {
        ShowTab(GameScreenTab.Training);
    }

    // Opens the Endless tab.
    public void ShowEndless()
    {
        ShowTab(GameScreenTab.Endless);
    }

    // Moves to the next tab.
    public void NextTab()
    {
        int currentIndex = (int)currentTab;
        int nextIndex = currentIndex + 1;

        if (nextIndex > (int)GameScreenTab.Endless)
            nextIndex = 0;

        ShowTab((GameScreenTab)nextIndex);
    }

    // Moves to the previous tab.
    public void PreviousTab()
    {
        int currentIndex = (int)currentTab;
        int previousIndex = currentIndex - 1;

        if (previousIndex < 0)
            previousIndex = (int)GameScreenTab.Endless;

        ShowTab((GameScreenTab)previousIndex);
    }

    // Shows one tab and hides the others.
    public void ShowTab(GameScreenTab tab)
    {
        currentTab = tab;

        if (mainPanel != null)
            mainPanel.SetActive(tab == GameScreenTab.Main);

        if (trainingPanel != null)
            trainingPanel.SetActive(tab == GameScreenTab.Training);

        if (endlessPanel != null)
            endlessPanel.SetActive(tab == GameScreenTab.Endless);
    }
}