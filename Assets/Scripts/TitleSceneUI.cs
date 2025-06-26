using UnityEngine;
using UnityEngine.SceneManagement;

public class GameExplanationUI : MonoBehaviour
{
    private string titleText = "게임 설명";
    private string gameTitle = "";

    // 설명 내용들
    private string[] explanationSections = {
        "🎯 게임 목표",
        "• 5분 안에 드래곤의 만족도를 100까지 채우세요\n• 드래곤의 인내심이 0이 되지 않도록 주의하세요\n• 드래곤이 원하는 음식을 빠르게 가져다주세요",

        "🎮 조작법",
        "• 방향키: 캐릭터 이동\n• 스페이스바: 상호작용 (음식 줍기/놓기)\n• F키: 음식 던지기\n• 쉬프트: 대쉬 (빠른 이동)",

        "🍖 게임 플레이",
        "• 드래곤 위에 표시되는 음식 아이콘을 확인하세요\n• 해당 음식을 찾아서 드래곤에게 가져다주세요\n• 시간이 오래 걸려도 인내심이 감소합니다 \n 주민을 들어서 구역에다가 던지면 일을 자동으로 합니다",

        "⚠️ 주의사항",
        "• 드래곤의 인내심이 0이 되면 게임 오버\n• 시간 제한 5분을 넘기면 게임 오버\n• 음식을 정확하고 빠르게 가져다주는 것이 핵심!"
    };

    private int hoveredButton = -1; // 0: 뒤로가기
    private Vector2 scrollPosition = Vector2.zero;

    // 스타일
    private GUIStyle titleStyle;
    private GUIStyle sectionTitleStyle;
    private GUIStyle contentStyle;
    private GUIStyle buttonStyle;
    private GUIStyle buttonHoverStyle;

    // 텍스처
    private Texture2D backgroundTexture;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;

    void Start()
    {
        CreateTextures();
    }

    void CreateTextures()
    {
        // 배경 텍스처 (어두운 청색)
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.2f, 1f));
        backgroundTexture.Apply();

        // 패널 텍스처 (반투명 회색)
        panelTexture = new Texture2D(1, 1);
        panelTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.3f, 0.9f));
        panelTexture.Apply();

        // 일반 버튼 텍스처
        buttonTexture = new Texture2D(1, 1);
        buttonTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.5f, 0.8f));
        buttonTexture.Apply();

        // 호버 버튼 텍스처
        buttonHoverTexture = new Texture2D(1, 1);
        buttonHoverTexture.SetPixel(0, 0, new Color(0.5f, 0.4f, 0.7f, 0.9f));
        buttonHoverTexture.Apply();
    }

    void OnGUI()
    {
        InitializeStyles();

        // 전체 화면 배경
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture);

        // 메인 패널
        float panelWidth = Screen.width * 0.8f;
        float panelHeight = Screen.height * 0.8f;
        float panelX = (Screen.width - panelWidth) / 2;
        float panelY = (Screen.height - panelHeight) / 2;

        Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
        GUI.DrawTexture(panelRect, panelTexture);

        // 타이틀
        Rect titleRect = new Rect(panelX, panelY + 20, panelWidth, 60);
        GUI.Label(titleRect, titleText, titleStyle);

        // 게임 제목
        Rect gameTitleRect = new Rect(panelX, panelY + 80, panelWidth, 40);
        GUI.Label(gameTitleRect, gameTitle, sectionTitleStyle);

        // 스크롤 영역
        Rect scrollViewRect = new Rect(panelX + 20, panelY + 140, panelWidth - 40, panelHeight - 220);
        Rect scrollContentRect = new Rect(0, 0, panelWidth - 60, CalculateContentHeight());

        scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, scrollContentRect);

        // 설명 내용 그리기
        float yOffset = 0;
        for (int i = 0; i < explanationSections.Length; i += 2)
        {
            // 섹션 제목
            Rect sectionTitleRect = new Rect(10, yOffset, scrollContentRect.width - 20, 40);
            GUI.Label(sectionTitleRect, explanationSections[i], sectionTitleStyle);
            yOffset += 50;

            // 섹션 내용
            if (i + 1 < explanationSections.Length)
            {
                float contentHeight = contentStyle.CalcHeight(new GUIContent(explanationSections[i + 1]), scrollContentRect.width - 40);
                Rect contentRect = new Rect(20, yOffset, scrollContentRect.width - 40, contentHeight);
                GUI.Label(contentRect, explanationSections[i + 1], contentStyle);
                yOffset += contentHeight + 30;
            }
        }

        GUI.EndScrollView();

        // 뒤로가기 버튼
        DrawBackButton(panelX, panelY, panelWidth, panelHeight);
    }

    void InitializeStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 36;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
        }

        if (sectionTitleStyle == null)
        {
            sectionTitleStyle = new GUIStyle(GUI.skin.label);
            sectionTitleStyle.alignment = TextAnchor.MiddleLeft;
            sectionTitleStyle.fontSize = 24;
            sectionTitleStyle.fontStyle = FontStyle.Bold;
            sectionTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.6f); // 연한 황색
        }

        if (contentStyle == null)
        {
            contentStyle = new GUIStyle(GUI.skin.label);
            contentStyle.alignment = TextAnchor.UpperLeft;
            contentStyle.fontSize = 18;
            contentStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            contentStyle.wordWrap = true;
            contentStyle.richText = true;
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fontSize = 20;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.normal.background = buttonTexture;
        }

        if (buttonHoverStyle == null)
        {
            buttonHoverStyle = new GUIStyle(buttonStyle);
            buttonHoverStyle.normal.background = buttonHoverTexture;
            buttonHoverStyle.normal.textColor = Color.yellow;
            buttonHoverStyle.fontSize = 22;
        }
    }

    void DrawBackButton(float panelX, float panelY, float panelWidth, float panelHeight)
    {
        float buttonWidth = 150;
        float buttonHeight = 50;
        float buttonX = panelX + panelWidth - buttonWidth - 20;
        float buttonY = panelY + panelHeight - buttonHeight - 20;

        Rect backButtonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);

        if (backButtonRect.Contains(Event.current.mousePosition))
        {
            hoveredButton = 0;
        }
        else
        {
            hoveredButton = -1;
        }

        bool backClicked = GUI.Button(backButtonRect, "뒤로 가기",
            hoveredButton == 0 ? buttonHoverStyle : buttonStyle);

        if (backClicked)
        {
            // 타이틀 씬으로 돌아가기
            // 타이틀 씬 이름에 맞게 수정하세요 (예: "TitleScene", "MainMenu", "Title" 등)
            SceneManager.LoadScene(0); // 빌드 인덱스 0번 씬으로 (보통 타이틀 씬)
            // 또는 씬 이름으로: SceneManager.LoadScene("YourTitleSceneName");
        }
    }

    float CalculateContentHeight()
    {
        float totalHeight = 0;

        for (int i = 0; i < explanationSections.Length; i += 2)
        {
            // 섹션 제목 높이
            totalHeight += 50;

            // 섹션 내용 높이
            if (i + 1 < explanationSections.Length)
            {
                float contentHeight = contentStyle.CalcHeight(new GUIContent(explanationSections[i + 1]), Screen.width * 0.8f - 100);
                totalHeight += contentHeight + 30;
            }
        }

        return totalHeight + 50; // 여유 공간
    }

    void OnDestroy()
    {
        // 텍스처 정리
        if (backgroundTexture != null) DestroyImmediate(backgroundTexture);
        if (panelTexture != null) DestroyImmediate(panelTexture);
        if (buttonTexture != null) DestroyImmediate(buttonTexture);
        if (buttonHoverTexture != null) DestroyImmediate(buttonHoverTexture);
    }
}