using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runs card mini-games and returns a multiplier to the card.
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Slider miniGameSlider;

    [Header("Simon Says")]
    [SerializeField]
    private KeyCode[] sequenceKeys =
    {
        KeyCode.A,
        KeyCode.F,
        KeyCode.K,
        KeyCode.L
    };

    [Header("Result Display")]
    [SerializeField] private float resultDisplayTime = 0.35f;

    private Coroutine currentRoutine;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    // Backwards-compatible old float callback.
    public void PlayMiniGame(CardData cardData, Action<float> onComplete)
    {
        PlayMiniGameResult(cardData, result => onComplete?.Invoke(result.multiplier));
    }

    public void PlayMiniGameResult(CardData cardData, Action<MiniGameResult> onComplete)
    {
        if (cardData == null || !cardData.HasMiniGame())
        {
            onComplete?.Invoke(MiniGameResult.None());
            return;
        }

        if (isPlaying)
        {
            Debug.LogWarning("A mini-game is already running. Returning 1x multiplier.");
            onComplete?.Invoke(MiniGameResult.None());
            return;
        }

        if (rootObject == null)
        {
            Debug.LogWarning("MiniGameManager has no rootObject assigned. Returning 1x multiplier.");
            onComplete?.Invoke(MiniGameResult.None());
            return;
        }

        currentRoutine = StartCoroutine(RunMiniGame(cardData, onComplete));
    }

    private IEnumerator RunMiniGame(CardData cardData, Action<MiniGameResult> onComplete)
    {
        isPlaying = true;
        Show();

        float score = 0f;

        switch (cardData.miniGameType)
        {
            case CardMiniGameType.SimonSays:
                yield return StartCoroutine(SimonSaysGame(cardData, value => score = value));
                break;

            case CardMiniGameType.TimingCircle:
                yield return StartCoroutine(TimingCircleGame(cardData, value => score = value));
                break;

            case CardMiniGameType.TimingBar:
                yield return StartCoroutine(TimingBarGame(cardData, value => score = value));
                break;

            case CardMiniGameType.ButtonMash:
                yield return StartCoroutine(ButtonMashGame(cardData, value => score = value));
                break;

            case CardMiniGameType.HoldRelease:
                yield return StartCoroutine(HoldReleaseGame(cardData, value => score = value));
                break;

            default:
                score = 0f;
                break;
        }

        MiniGameResult result = ScoreToResult(score);

        if (resultText != null)
            resultText.text = result.grade + " - x" + result.multiplier.ToString("0.##");

        yield return new WaitForSecondsRealtime(resultDisplayTime);

        Hide();
        isPlaying = false;
        currentRoutine = null;

        onComplete?.Invoke(result);
    }

    private IEnumerator SimonSaysGame(CardData cardData, Action<float> onComplete)
    {
        SetSliderVisible(false);

        int sequenceLength = Mathf.Max(1, cardData.miniGameSequenceLength);
        List<KeyCode> sequence = GenerateSequence(sequenceLength);

        int currentIndex = 0;
        int mistakes = 0;
        float timeLeft = Mathf.Max(0.5f, cardData.miniGameTimeLimit);

        if (titleText != null)
            titleText.text = "Simon Says";

        while (timeLeft > 0f && currentIndex < sequence.Count)
        {
            timeLeft -= Time.unscaledDeltaTime;

            if (timerText != null)
                timerText.text = timeLeft.ToString("0.0");

            if (instructionText != null)
                instructionText.text = "Press: " + BuildSequenceText(sequence, currentIndex);

            KeyCode pressedKey = GetPressedSequenceKey();

            if (pressedKey != KeyCode.None)
            {
                if (pressedKey == sequence[currentIndex])
                    currentIndex++;
                else
                    mistakes++;
            }

            yield return null;
        }

        float progressScore = (float)currentIndex / sequence.Count;
        float mistakePenalty = mistakes * 0.15f;
        onComplete?.Invoke(Mathf.Clamp01(progressScore - mistakePenalty));
    }

    private IEnumerator TimingCircleGame(CardData cardData, Action<float> onComplete)
    {
        SetSliderVisible(true);

        float timeLeft = Mathf.Max(0.5f, cardData.miniGameTimeLimit);
        float score = 0f;
        bool pressed = false;

        if (titleText != null)
            titleText.text = "Timing Circle";

        if (instructionText != null)
            instructionText.text = "Press Space or Left Click when it reaches the centre.";

        while (timeLeft > 0f && !pressed)
        {
            timeLeft -= Time.unscaledDeltaTime;

            float value = Mathf.PingPong(Time.unscaledTime * cardData.miniGameSliderSpeed, 1f);

            if (miniGameSlider != null)
                miniGameSlider.value = value;

            if (timerText != null)
                timerText.text = timeLeft.ToString("0.0");

            if (PressedMainInput(cardData))
            {
                pressed = true;
                score = GetCentreAccuracy(value);
            }

            yield return null;
        }

        onComplete?.Invoke(score);
    }

    private IEnumerator TimingBarGame(CardData cardData, Action<float> onComplete)
    {
        SetSliderVisible(true);

        float timeLeft = Mathf.Max(0.5f, cardData.miniGameTimeLimit);
        float score = 0f;
        bool pressed = false;

        if (titleText != null)
            titleText.text = "Timing Bar";

        if (instructionText != null)
            instructionText.text = "Stop the slider in the centre.";

        while (timeLeft > 0f && !pressed)
        {
            timeLeft -= Time.unscaledDeltaTime;

            float value = Mathf.PingPong(Time.unscaledTime * cardData.miniGameSliderSpeed, 1f);

            if (miniGameSlider != null)
                miniGameSlider.value = value;

            if (timerText != null)
                timerText.text = timeLeft.ToString("0.0");

            if (PressedMainInput(cardData))
            {
                pressed = true;
                score = GetCentreAccuracy(value);
            }

            yield return null;
        }

        onComplete?.Invoke(score);
    }

    private IEnumerator ButtonMashGame(CardData cardData, Action<float> onComplete)
    {
        SetSliderVisible(true);

        int presses = 0;
        int targetPresses = Mathf.Max(1, cardData.miniGameMashTarget);
        float timeLeft = Mathf.Max(0.5f, cardData.miniGameTimeLimit);

        if (titleText != null)
            titleText.text = "Button Mash";

        while (timeLeft > 0f)
        {
            timeLeft -= Time.unscaledDeltaTime;

            if (PressedMainInput(cardData))
                presses++;

            float progress = Mathf.Clamp01((float)presses / targetPresses);

            if (miniGameSlider != null)
                miniGameSlider.value = progress;

            if (timerText != null)
                timerText.text = timeLeft.ToString("0.0");

            if (instructionText != null)
                instructionText.text = "Mash Space or Left Click! " + presses + "/" + targetPresses;

            yield return null;
        }

        onComplete?.Invoke(Mathf.Clamp01((float)presses / targetPresses));
    }

    private IEnumerator HoldReleaseGame(CardData cardData, Action<float> onComplete)
    {
        SetSliderVisible(true);

        float timeLeft = Mathf.Max(0.5f, cardData.miniGameTimeLimit);
        float charge = 0f;
        float score = 0f;
        bool released = false;

        if (titleText != null)
            titleText.text = "Hold Release";

        if (instructionText != null)
            instructionText.text = "Hold Space or Left Click, release near the centre.";

        while (timeLeft > 0f && !released)
        {
            timeLeft -= Time.unscaledDeltaTime;

            if (HoldingMainInput(cardData))
                charge += Time.unscaledDeltaTime * cardData.miniGameHoldSpeed;

            charge = Mathf.Clamp01(charge);

            if (miniGameSlider != null)
                miniGameSlider.value = charge;

            if (timerText != null)
                timerText.text = timeLeft.ToString("0.0");

            if (ReleasedMainInput(cardData))
            {
                released = true;
                score = GetCentreAccuracy(charge);
            }

            yield return null;
        }

        onComplete?.Invoke(score);
    }

    private List<KeyCode> GenerateSequence(int length)
    {
        List<KeyCode> result = new List<KeyCode>();

        if (sequenceKeys == null || sequenceKeys.Length == 0)
        {
            result.Add(KeyCode.Space);
            return result;
        }

        for (int i = 0; i < length; i++)
        {
            KeyCode key = sequenceKeys[UnityEngine.Random.Range(0, sequenceKeys.Length)];
            result.Add(key);
        }

        return result;
    }

    private string BuildSequenceText(List<KeyCode> sequence, int currentIndex)
    {
        string text = "";

        for (int i = 0; i < sequence.Count; i++)
        {
            if (i < currentIndex)
                text += "[" + sequence[i] + "] ";
            else if (i == currentIndex)
                text += "> " + sequence[i] + " < ";
            else
                text += sequence[i] + " ";
        }

        return text;
    }

    private KeyCode GetPressedSequenceKey()
    {
        if (sequenceKeys == null)
            return KeyCode.None;

        for (int i = 0; i < sequenceKeys.Length; i++)
        {
            if (Input.GetKeyDown(sequenceKeys[i]))
                return sequenceKeys[i];
        }

        return KeyCode.None;
    }

    private bool PressedMainInput(CardData cardData)
    {
        if (Input.GetKeyDown(cardData.miniGameInputKey))
            return true;

        return cardData.allowLeftClickInput && Input.GetMouseButtonDown(0);
    }

    private bool HoldingMainInput(CardData cardData)
    {
        if (Input.GetKey(cardData.miniGameInputKey))
            return true;

        return cardData.allowLeftClickInput && Input.GetMouseButton(0);
    }

    private bool ReleasedMainInput(CardData cardData)
    {
        if (Input.GetKeyUp(cardData.miniGameInputKey))
            return true;

        return cardData.allowLeftClickInput && Input.GetMouseButtonUp(0);
    }

    private float GetCentreAccuracy(float value)
    {
        float distance = Mathf.Abs(value - 0.5f);
        return 1f - Mathf.Clamp01(distance / 0.5f);
    }

    private MiniGameResult ScoreToResult(float score)
    {
        MiniGameResult result = new MiniGameResult();
        result.score = Mathf.Clamp01(score);

        if (score >= 0.95f)
        {
            result.grade = MiniGameGrade.Perfect;
            result.multiplier = 2.5f;
        }
        else if (score >= 0.7f)
        {
            result.grade = MiniGameGrade.Good;
            result.multiplier = 2f;
        }
        else if (score >= 0.4f)
        {
            result.grade = MiniGameGrade.OK;
            result.multiplier = 1.5f;
        }
        else
        {
            result.grade = MiniGameGrade.Bad;
            result.multiplier = 1f;
        }

        return result;
    }

    private void Show()
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (resultText != null)
            resultText.text = "";
    }

    private void Hide()
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }

    private void SetSliderVisible(bool visible)
    {
        if (miniGameSlider != null)
            miniGameSlider.gameObject.SetActive(visible);
    }
}
