using Assets.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// UI panel for pause/resume and simulation speed control.
    /// Wire up the references in the Inspector and drop this onto a Canvas GameObject.
    /// </summary>
    public class SimulationControlPanel : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The GameManager in the scene.")]
        [SerializeField] private GameManager gameManager;

        [Header("Controls")]
        [Tooltip("Button shown while the simulation is running. Clicking pauses it.")]
        [SerializeField] private Button pauseButton;

        [Tooltip("Button shown while the simulation is paused. Clicking resumes it.")]
        [SerializeField] private Button playButton;

        [Tooltip("Slider controlling simulation speed. Left = slow, right = fast.")]
        [SerializeField] private Slider speedSlider;

        [Tooltip("Editable text field showing the current action delay in seconds.")]
        [SerializeField] private TMP_InputField delayInputField;

        [Header("Speed Range")]
        [Tooltip("Action delay (seconds) when the slider is at its slowest (leftmost) position.")]
        [SerializeField] private float slowestDelay = 2.0f;

        [Tooltip("Action delay (seconds) when the slider is at its fastest (rightmost) position.")]
        [SerializeField] private float fastestDelay = 0.05f;

        // Tracks the current delay so the slider and input field stay in sync.
        private float currentDelay;
        private bool isPaused = true;

        private void Start()
        {
            currentDelay = GetStartingDelay();

            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (speedSlider != null)
            {
                speedSlider.minValue = 0f;
                speedSlider.maxValue = 1f;
                speedSlider.value = DelayToSlider(currentDelay);
                speedSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (delayInputField != null)
            {
                delayInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                delayInputField.text = FormatDelay(currentDelay);
                delayInputField.onEndEdit.AddListener(OnDelayInputSubmitted);
            }

            RefreshButtons();
        }

        private void OnPauseClicked()
        {
            isPaused = true;
            gameManager.SetPaused(true);
            RefreshButtons();
        }

        private void OnPlayClicked()
        {
            isPaused = false;
            gameManager.SetPaused(false);
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            // Only the relevant button is visible at any time.
            if (pauseButton != null)
                pauseButton.gameObject.SetActive(!isPaused);

            if (playButton != null)
                playButton.gameObject.SetActive(isPaused);
        }

        private void OnSliderChanged(float value)
        {
            currentDelay = SliderToDelay(value);
            gameManager.SetActionDelay(currentDelay);
            RefreshInputField();
        }

        private void OnDelayInputSubmitted(string text)
        {
            if (!float.TryParse(text, out float parsed))
            {
                // Input was not a valid number — restore the field to the last known value.
                RefreshInputField();
                return;
            }

            currentDelay = Mathf.Clamp(parsed, fastestDelay, slowestDelay);
            gameManager.SetActionDelay(currentDelay);
            RefreshInputField();
            RefreshSlider();
        }

        private void RefreshInputField()
        {
            if (delayInputField != null)
                delayInputField.text = FormatDelay(currentDelay);
        }

        private void RefreshSlider()
        {
            if (speedSlider != null)
            {
                // Remove the listener temporarily to avoid the slider firing OnSliderChanged.
                speedSlider.onValueChanged.RemoveListener(OnSliderChanged);
                speedSlider.value = DelayToSlider(currentDelay);
                speedSlider.onValueChanged.AddListener(OnSliderChanged);
            }
        }

        // Maps a 0..1 slider value to an action delay in seconds.
        // Slider 0 = fastestDelay (right feels fast), slider 1 = slowestDelay.
        private float SliderToDelay(float sliderValue)
        {
            return Mathf.Lerp(fastestDelay, slowestDelay, sliderValue);
        }

        // Maps an action delay back to a 0..1 slider value.
        private float DelayToSlider(float delay)
        {
            if (Mathf.Approximately(fastestDelay, slowestDelay)) return 0.5f;
            return Mathf.InverseLerp(fastestDelay, slowestDelay, delay);
        }

        private float GetStartingDelay()
        {
            if (gameManager != null)
                return Mathf.Clamp(gameManager.GetActionDelay(), fastestDelay, slowestDelay);
            return Mathf.Lerp(slowestDelay, fastestDelay, 0.5f);
        }

        private static string FormatDelay(float delay)
        {
            return delay.ToString("0.00");
        }
    }
}
