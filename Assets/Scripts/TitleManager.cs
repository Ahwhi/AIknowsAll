using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public static TitleManager Instance { get; private set; }

    public AudioSource _audio;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        _audio = GetComponent<AudioSource>();
        _audio.Play();
    }

    public void OnStartButton() {
        SceneManager.LoadScene("MainScene");
    }
}
