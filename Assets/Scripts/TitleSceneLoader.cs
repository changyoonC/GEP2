using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneLoader : MonoBehaviour
{
    public Button startButton;
    public string nextSceneName = "TestMain";

    void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(LoadNextScene);
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}