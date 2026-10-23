using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject inputPanel;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverScreen;

    private bool isPaused;

    private void Start()
    {
        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }

        // On ne relance le temps que si le joueur
        // n'est pas déjà sur l'écran de mort.
        if (!IsGameOver())
        {
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (
            GameInput.Instance != null &&
            GameInput.Instance.PausePressedThisFrame
        )
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }

        /*
         * Très important :
         * si l'écran de mort est actif,
         * on NE relance PAS le temps.
         */
        if (IsGameOver())
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    public void ShowInputPanel()
    {
        if (inputPanel != null)
        {
            inputPanel.SetActive(true);
        }
    }

    public void HideInputPanel()
    {
        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private bool IsGameOver()
    {
        return
            gameOverScreen != null &&
            gameOverScreen.activeInHierarchy;
    }
}