using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the overall game state, including score, lives, and game over conditions.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [Tooltip("The player's current score.")]
    public int score = 0;
    [Tooltip("The player's current number of lives.")]
    public int lives = 3;

    [Header("UI Elements")]
    [Tooltip("The Text component to display the score.")]
    public Text scoreText;
    [Tooltip("The Text component to display the number of lives.")]
    public Text livesText;
    [Tooltip("The panel that appears on game over.")]
    public GameObject gameOverPanel;
    [Tooltip("The Text component to display the game over message.")]
    public Text gameOverMessage;

    // --- Private Fields ---
    private ObjectSpawner objectSpawner;

    /// <summary>
    /// Singleton instance of the GameManager.
    /// </summary>
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        // Set up the singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Find the ObjectSpawner in the scene
        objectSpawner = FindObjectOfType<ObjectSpawner>();
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    void Start()
    {
        // Initialize the UI
        UpdateUI();
        gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Increases the score by a specified amount.
    /// </summary>
    /// <param name="amount">The amount to increase the score by.</param>
    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    /// <summary>
    /// Decreases the number of lives by one.
    /// </summary>
    public void LoseLife()
    {
        lives--;
        UpdateUI();

        if (lives <= 0)
        {
            GameOver("You ran out of lives!");
        }
    }

    /// <summary>
    /// Ends the game and displays the game over panel.
    /// </summary>
    /// <param name="message">The message to display on the game over screen.</param>
    public void GameOver(string message)
    {
        // Stop the object spawner
        if (objectSpawner != null)
        {
            objectSpawner.enabled = false;
        }

        // Show the game over panel
        gameOverMessage.text = message;
        gameOverPanel.SetActive(true);
    }

    /// <summary>
    /// Restarts the game by reloading the current scene.
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Updates the score and lives UI elements.
    /// </summary>
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (livesText != null)
        {
            livesText.text = "Lives: " + lives;
        }
    }
}
