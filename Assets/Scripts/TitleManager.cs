using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneUI : MonoBehaviour
{
    private string goal = "";
    private string control = "";
    private string resolution = "";
    private string author = "";

    // 버튼 상태
    private int hoveredButton = -1; // -1: 없음, 0: 게임시작, 1: 게임설명

    // 스타일
    private GUIStyle buttonStyle;
    private GUIStyle buttonHoverStyle;
    private GUIStyle infoStyle;

    // 배경 이미지 (Inspector에서 할당)
    [Header("배경 설정")]
    public Texture2D backgroundImage; // Inspector에서 1920x1080 이미지 할당

    // 텍스처들
    private Texture2D fallbackBackground; // 배경 이미지가 없을 때 사용
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;

    void Start()
    {
        CreateTextures();
    }

    void CreateTextures()
    {
        // 기본 배경 텍스처 (배경 이미지가 없을 때 사용)
        fallbackBackground = new Texture2D(1, 1);
        fallbackBackground.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.2f, 1f));
        fallbackBackground.Apply();

        // 일반 버튼 텍스처
        buttonTexture = new Texture2D(1, 1);
        buttonTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.5f, 0.8f));
        buttonTexture.Apply();

        // 호버 버튼 텍스처 (밝은 보라색)
        buttonHoverTexture = new Texture2D(1, 1);
        buttonHoverTexture.SetPixel(0, 0, new Color(0.5f, 0.4f, 0.7f, 0.9f));
        buttonHoverTexture.Apply();
    }

    void OnGUI()
    {
        InitializeStyles();

        // 배경 이미지 (Inspector에서 할당한 이미지 또는 기본 배경)
        Texture2D currentBackground = backgroundImage != null ? backgroundImage : fallbackBackground;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), currentBackground, ScaleMode.StretchToFill);

        // 버튼 영역 (좌측)
        DrawButtons();

        // 정보 영역 (우측 하단)
        DrawInfo();
    }

    void InitializeStyles()
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fontSize = 20;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.normal.background = buttonTexture;
            buttonStyle.fixedHeight = 70;
            buttonStyle.margin = new RectOffset(10, 10, 5, 5);
        }

        if (buttonHoverStyle == null)
        {
            buttonHoverStyle = new GUIStyle(buttonStyle);
            buttonHoverStyle.normal.background = buttonHoverTexture;
            buttonHoverStyle.normal.textColor = Color.yellow;
            buttonHoverStyle.fontSize = 22;
        }

        if (infoStyle == null)
        {
            infoStyle = new GUIStyle(GUI.skin.label);
            infoStyle.alignment = TextAnchor.UpperLeft;
            infoStyle.fontSize = 16;
            infoStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            infoStyle.wordWrap = true;
        }
    }

    void DrawButtons()
    {
        float buttonWidth = 200;
        float buttonX = 50;
        float startY = Screen.height * 0.35f;
        float spacing = 100;

        // 게임 시작 버튼
        Rect startButtonRect = new Rect(buttonX, startY, buttonWidth, 70);
        if (startButtonRect.Contains(Event.current.mousePosition))
        {
            hoveredButton = 0;
        }

        bool startClicked = GUI.Button(startButtonRect, "게임 시작",
            hoveredButton == 0 ? buttonHoverStyle : buttonStyle);

        if (startClicked)
        {
            SceneManager.LoadScene("TestMain");
        }

        // 게임 설명 버튼
        Rect helpButtonRect = new Rect(buttonX, startY + spacing, buttonWidth, 70);
        if (helpButtonRect.Contains(Event.current.mousePosition))
        {
            hoveredButton = 1;
        }

        bool helpClicked = GUI.Button(helpButtonRect, "게임 설명",
            hoveredButton == 1 ? buttonHoverStyle : buttonStyle);

        if (helpClicked)
        {
            SceneManager.LoadScene("GameExplanation");
        }

        // 마우스가 버튼 영역 밖에 있으면 호버 상태 리셋
        if (!startButtonRect.Contains(Event.current.mousePosition) &&
            !helpButtonRect.Contains(Event.current.mousePosition))
        {
            hoveredButton = -1;
        }
    }

    void DrawInfo()
    {
        float infoWidth = 500;
        float infoX = Screen.width - infoWidth - 30;
        float infoY = Screen.height * 0.5f;

        // 게임 정보
        Rect goalRect = new Rect(infoX, infoY, infoWidth, 100);
        GUI.Label(goalRect, goal, infoStyle);

        Rect controlRect = new Rect(infoX, infoY + 120, infoWidth, 100);
        GUI.Label(controlRect, control, infoStyle);

        // 하단 정보
        Rect resolutionRect = new Rect(infoX, Screen.height - 100, infoWidth, 30);
        GUI.Label(resolutionRect, resolution, infoStyle);

        Rect authorRect = new Rect(infoX, Screen.height - 65, infoWidth, 30);
        GUI.Label(authorRect, author, infoStyle);
    }

    void OnDestroy()
    {
        // 텍스처 정리
        if (fallbackBackground != null) DestroyImmediate(fallbackBackground);
        if (buttonTexture != null) DestroyImmediate(buttonTexture);
        if (buttonHoverTexture != null) DestroyImmediate(buttonHoverTexture);
    }
}