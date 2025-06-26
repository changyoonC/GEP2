using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingSceneUI : MonoBehaviour
{
    private string clearText = "게임 클리어!";
    private string gameOverText = "게임 오버";
    private string restartButton = "게임 재시작";
    private string mainMenuButton = "메인 메뉴";
    private Rect windowRect = new Rect(0, 0, 500, 300);
    private GUIStyle style;
    private GUIStyle buttonStyle;
    private GUIStyle titleStyle;

    void Start()
    {
        windowRect.x = (Screen.width - windowRect.width) / 2;
        windowRect.y = (Screen.height - windowRect.height) / 2;
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 24;
        }
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 40;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = GameResult.isClear ? Color.green : Color.red;
        }
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 24;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
        }
        windowRect = GUI.ModalWindow(0, windowRect, DrawWindow, "");
    }

    void DrawWindow(int windowID)
    {
        GUILayout.Space(20);
        GUILayout.Label(GameResult.isClear ? clearText : gameOverText, titleStyle, GUILayout.Height(60));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(restartButton, buttonStyle, GUILayout.Height(50)))
        {
            SceneManager.LoadScene("TestMain");
        }
        GUILayout.Space(20);
        if (GUILayout.Button(mainMenuButton, buttonStyle, GUILayout.Height(50)))
        {
            SceneManager.LoadScene("Title");
        }
    }
}