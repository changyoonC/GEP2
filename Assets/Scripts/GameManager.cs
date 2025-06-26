using UnityEngine;
using System.Collections;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        [Header("드래곤 시스템")]
        public int dragonPatience = 100;
        public int dragonSatisfaction = 0;
        public float patienceDecreaseInterval = 5f;
        public int patienceDecreaseAmount = 10;

        [Header("게임 타이머")]
        public float gameTimeLimit = 300f; // 5분
        public float recipeChangeTime = 45f; // 45초마다 레시피 변경

        [Header("점수 시스템")]
        public int totalScore = 0;
        public int completedRecipes = 0;

        [Header("GUI 설정")]
        public Font mainFont;
        public int mainFontSize = 20;
        public int titleFontSize = 26;
        public Color mainTextColor = Color.white;
        public Color valueTextColor = Color.yellow;
        public Color patienceColor = new Color(1f, 0.4f, 0.4f);
        public Color satisfactionColor = new Color(0.4f, 1f, 0.4f);

        // GUI 스타일
        private GUIStyle mainLabelStyle;
        private GUIStyle valueLabelStyle;
        private GUIStyle titleLabelStyle;
        private GUIStyle backgroundBoxStyle;
        private GUIStyle gameOverTitleStyle;
        private GUIStyle gameOverLabelStyle;

        private float currentGameTime;
        public float CurrentGameTime => currentGameTime;
        private float lastPatienceDecrease;
        private float lastRecipeChange;
        private bool gameOver = false;
        private bool gameWon = false;

        private CookingPot cookingPot;
        private DragonMoodChangeNotification dragonNotification;

        public bool isMemoryLostEvent = false;
        private float memoryLostEventEndTime = 0f;
        private bool phase2NotificationShown = false;
        private bool phase3NotificationShown = false;

        void Start()
        {
            currentGameTime = gameTimeLimit;
            lastPatienceDecrease = Time.time;
            lastRecipeChange = Time.time;

            cookingPot = FindObjectOfType<CookingPot>();
            dragonNotification = FindObjectOfType<DragonMoodChangeNotification>();

            InitializeGUIStyles();
            phase2NotificationShown = false;
            phase3NotificationShown = false;

            Debug.Log("게임 시작! 드래곤을 만족시켜주세요!");
        }

        void InitializeGUIStyles()
        {
            // 배경 박스 스타일
            backgroundBoxStyle = new GUIStyle(GUI.skin.box);
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.75f));
            bgTex.Apply();
            backgroundBoxStyle.normal.background = bgTex;
            backgroundBoxStyle.border = new RectOffset(10, 10, 10, 10);

            // 타이머/제목 스타일
            titleLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = mainFont,
                fontSize = titleFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            // 기본 라벨 스타일 (예: "드래곤 인내심")
            mainLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = mainFont,
                fontSize = mainFontSize,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = mainTextColor }
            };

            // 값 라벨 스타일 (예: "100")
            valueLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = mainFont,
                fontSize = mainFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = valueTextColor }
            };
            
            // 게임 오버 타이틀 스타일
            gameOverTitleStyle = new GUIStyle(titleLabelStyle)
            {
                fontSize = 40
            };
            
            // 게임 오버 라벨 스타일
            gameOverLabelStyle = new GUIStyle(mainLabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24
            };
        }

        void Update()
        {
            if (gameOver || gameWon) return;

            UpdateGameTimer();
            UpdateDragonPatience();
            CheckGameEndConditions();
            HandleMemoryLostEvent();
            CheckPhaseNotifications();
        }

        void UpdateGameTimer()
        {
            currentGameTime -= Time.deltaTime;

            if (currentGameTime <= 0)
            {
                currentGameTime = 0;
                EndGame(false);
            }
        }

        void UpdateDragonPatience()
        {
            if (Time.time - lastRecipeChange >= recipeChangeTime)
            {
                if (Time.time - lastPatienceDecrease >= patienceDecreaseInterval)
                {
                    dragonPatience -= patienceDecreaseAmount;
                    lastPatienceDecrease = Time.time;

                    Debug.Log($"드래곤 인내심 감소! 현재: {dragonPatience}");

                    if (dragonPatience <= 0)
                    {
                        EndGame(false);
                    }
                }
            }
        }

        void CheckGameEndConditions()
        {
            if (dragonSatisfaction >= 100)
            {
                EndGame(true);
            }
        }

        public void OnRecipeCompleted(int points)
        {
            completedRecipes++;
            totalScore += points;
            dragonSatisfaction += 20;
            dragonPatience = Mathf.Min(100, dragonPatience + 30);
            lastRecipeChange = Time.time;

            Debug.Log($"레시피 완성! 점수: +{points}, 총점: {totalScore}");
        }

        void EndGame(bool won)
        {
            gameWon = won;
            gameOver = true;

            if (won)
            {
                Debug.Log("게임 승리! 드래곤이 만족했습니다!");
            }
            else
            {
                Debug.Log("게임 오버! 드래곤의 인내심이 바닥나거나 시간이 초과되었습니다.");
            }
            
            Time.timeScale = 1f;
            //GameResult.isClear = won;
            //UnityEngine.SceneManagement.SceneManager.LoadScene("Ending");
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void DrawProgressBar(Rect position, float progress, Color fillColor, Color bgColor)
        {
            GUI.DrawTexture(position, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, bgColor, 0, 0);
            Rect fillRect = new Rect(position.x, position.y, position.width * Mathf.Clamp01(progress), position.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, fillColor, 0, 0);
        }

        void OnGUI()
        {
            if (backgroundBoxStyle == null) InitializeGUIStyles();

            // --- 좌측 정보 패널 ---
            // 1. 패널의 전체 높이를 먼저 계산합니다.
            float panelX = 20f;
            float panelY = 20f;
            float panelWidth = 320f;
            float contentPadding = 15f;
            float lineSpacing = 8f;
            float sectionSpacing = 16f;
            float heightCalculationY = panelY + contentPadding;

            heightCalculationY += titleFontSize + lineSpacing; // Timer
            heightCalculationY += 15 + sectionSpacing; // Time Bar
            heightCalculationY += mainFontSize + lineSpacing; // Patience Label
            heightCalculationY += mainFontSize + sectionSpacing; // Satisfaction Label
            heightCalculationY += mainFontSize + lineSpacing; // Score Label
            heightCalculationY += mainFontSize + lineSpacing; // Recipes Label

            float patienceBarValue = Mathf.Max(0, recipeChangeTime - (Time.time - lastRecipeChange));
            if (patienceBarValue > 0.01f)
            {
                heightCalculationY += sectionSpacing;
                heightCalculationY += mainFontSize + lineSpacing; // Patience bar label
                heightCalculationY += 8; // Patience bar
            }
            float panelHeight = heightCalculationY - panelY + contentPadding;

            // 2. 계산된 높이로 배경 박스를 먼저 그립니다.
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "", backgroundBoxStyle);

            // 3. 박스 위에 콘텐츠를 그립니다.
            float contentWidth = panelWidth - (contentPadding * 2);
            float currentY = panelY + contentPadding;

            // 타이머
            int minutes = Mathf.FloorToInt(currentGameTime / 60f);
            int seconds = Mathf.FloorToInt(currentGameTime % 60f);
            GUI.Label(new Rect(panelX, currentY, panelWidth, titleFontSize), $"{minutes:00}:{seconds:00}", titleLabelStyle);
            currentY += titleFontSize + lineSpacing;

            // 게임 시간 바
            DrawProgressBar(new Rect(panelX + contentPadding, currentY, contentWidth, 15), currentGameTime / gameTimeLimit, Color.green, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            currentY += 15 + sectionSpacing;

            // 드래곤 정보
            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), "드래곤 인내심", mainLabelStyle);
            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), $"{dragonPatience}", valueLabelStyle);
            currentY += mainFontSize + lineSpacing;

            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), "드래곤 만족도", mainLabelStyle);
            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), $"{dragonSatisfaction}", valueLabelStyle);
            currentY += mainFontSize + sectionSpacing;

            // 점수 정보
            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), "총 점수", mainLabelStyle);
            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), $"{totalScore}", valueLabelStyle);
            currentY += mainFontSize + lineSpacing;

            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), "완성한 레시피", mainLabelStyle);
            GUI.Label(new Rect(panelX + contentPadding, currentY, contentWidth, mainFontSize), $"{completedRecipes}", valueLabelStyle);
            
            // 인내심 감소 바
            if (patienceBarValue > 0.01f)
            {
                currentY += sectionSpacing;
                var centeredLabel = new GUIStyle(mainLabelStyle) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(panelX, currentY, panelWidth, mainFontSize), $"인내심 감소까지: {patienceBarValue:F0}초", centeredLabel);
                currentY += mainFontSize + lineSpacing;
                DrawProgressBar(new Rect(panelX + contentPadding, currentY, contentWidth, 8), patienceBarValue / recipeChangeTime, patienceColor, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            }

            // 이벤트 알림
            if (isMemoryLostEvent)
            {
                var eventStyle = new GUIStyle(mainLabelStyle) { normal = { textColor = patienceColor }, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                GUI.Box(new Rect(panelX, panelY + panelHeight + 10, panelWidth, 40), "", backgroundBoxStyle);
                GUI.Label(new Rect(panelX, panelY + panelHeight + 18, panelWidth, mainFontSize), "이벤트: 기억이 안나!", eventStyle);
            }

            // --- 게임 종료 UI ---
            if (gameOver)
            {
                float boxWidth = 500f;
                float boxHeight = 250f;
                float centerX = Screen.width / 2f;
                float centerY = Screen.height / 2f;

                GUI.Box(new Rect(centerX - boxWidth / 2, centerY - boxHeight / 2, boxWidth, boxHeight), "", backgroundBoxStyle);
                
                currentY = centerY - boxHeight/2 + 20f;
                
                GUI.Label(new Rect(centerX - boxWidth / 2, currentY, boxWidth, gameOverTitleStyle.fontSize), gameWon ? "게임 승리!" : "게임 오버", gameOverTitleStyle);
                currentY += gameOverTitleStyle.fontSize + 20f;

                string reason = "";
                if (!gameWon)
                {
                    reason = dragonPatience <= 0 ? "드래곤의 인내심이 바닥났습니다!" : "시간이 초과되었습니다!";
                }
                else
                {
                    reason = "드래곤이 아주 만족했습니다!";
                }
                GUI.Label(new Rect(centerX - boxWidth / 2, currentY, boxWidth, gameOverLabelStyle.fontSize), reason, gameOverLabelStyle);
                currentY += gameOverLabelStyle.fontSize + 10f;
                GUI.Label(new Rect(centerX - boxWidth / 2, currentY, boxWidth, gameOverLabelStyle.fontSize), $"최종 점수: {totalScore}", gameOverLabelStyle);
                
                if (GUI.Button(new Rect(centerX - 100, centerY + boxHeight / 2 - 60, 200, 50), "다시 시작"))
                {
                    RestartGame();
                }
            }
        }

        void HandleMemoryLostEvent()
        {
            // 5분 이하 남았을 때, 이벤트가 발생하지 않은 상태라면
            if (!isMemoryLostEvent && currentGameTime <= 200f)
            {
                // 70% 확률로 이벤트 발생
                if (Random.value < 0.3f)
                {
                    isMemoryLostEvent = true;
                    memoryLostEventEndTime = Time.time + 30f; // 30초간 유지
                    Debug.Log("이벤트 발생: 기억기 안나!");
                }
            }

            // 이벤트가 발생 중이고, 시간이 끝났으면 해제
            if (isMemoryLostEvent && Time.time >= memoryLostEventEndTime)
            {
                isMemoryLostEvent = false;
                Debug.Log("이벤트 종료: 기억기 안나!");
            }
        }

        void CheckPhaseNotifications()
        {
            if (cookingPot == null) return;

            // 3분(180초) 남았을 때
            if (!phase2NotificationShown && currentGameTime <= 180f)
            {
                cookingPot.ShowNotification("2페이즈 시작!", null, 3.0f);
                phase2NotificationShown = true;
                Debug.Log("알림: 2페이즈 시작! (시간: 3분)");
            }

            // 1분 40초(100초) 남았을 때
            if (!phase3NotificationShown && currentGameTime <= 100f)
            {
                cookingPot.ShowNotification("2페이즈 시작!", null, 3.0f);
                phase3NotificationShown = true;
                Debug.Log("알림: 2페이즈 시작! (시간: 1분 40초)");
            }
        }

        /// <summary>
        /// 새 게임이나 레시피 완성 시 드래곤 변심 이벤트 플래그를 리셋하는 메서드
        /// </summary>
        public void ResetDragonMoodChangeEvent()
        {
            // 더 이상 필요 없음 (CookingPot에서만 관리)
        }
    }
}