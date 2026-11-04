using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the main menu: remembers the player's name and starts the game.
/// </summary>
public class MainMenu : MonoBehaviour
{
    private const string PlayerNameKey = "PlayerName";
    private const string DefaultPlayerName = "Pilot";

    [SerializeField] private InputField nameField;
    [SerializeField] private Button playButton;
    [SerializeField] private string gameSceneName = "TestTrack";

    /// <summary>The name the player entered, or "Pilot" if they left it blank.</summary>
    public static string PlayerName => PlayerPrefs.GetString(PlayerNameKey, DefaultPlayerName);

    private void Awake()
    {
        nameField.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        playButton.onClick.AddListener(Play);
        nameField.onSubmit.AddListener(OnNameSubmitted);
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(Play);
        }

        if (nameField != null)
        {
            nameField.onSubmit.RemoveListener(OnNameSubmitted);
        }
    }

    private void OnNameSubmitted(string _)
    {
        Play();
    }

    private void Play()
    {
        string enteredName = nameField.text.Trim();
        PlayerPrefs.SetString(PlayerNameKey, string.IsNullOrEmpty(enteredName) ? DefaultPlayerName : enteredName);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }
}
