using UnityEngine;
using System.Collections;

namespace GameCore
{
    public class DragonMoodChangeNotification : MonoBehaviour
    {
        [Header("드래곤 알림 설정")]
        public Texture2D dragonImage;
        public Font notificationFont;
        public float animationDuration = 3f; // 전체 애니메이션 시간
        public float stayDuration = 2f; // 화면에 머무는 시간
        public float boxWidth = 400f;
        public float boxHeight = 120f;
        public Color boxColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public Color borderColor = Color.white;
        public Color textColor = Color.white;
        public int fontSize = 18;

        [Header("애니메이션 설정")]
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private bool isAnimating = false;
        private float animationTime = 0f;
        private float currentY = 0f;
        private string notificationText = "음... 다른게 땡기네...";

        // 계산된 위치들
        private float hiddenY;
        private float visibleY;
        private float centerX;

        private GUIStyle textStyle;
        private Texture2D boxTexture;
        private Texture2D borderTexture;

        void Start()
        {
            // 위치 계산
            centerX = (Screen.width - boxWidth) / 2f;
            hiddenY = -boxHeight - 10f; // 화면 위쪽 숨겨진 위치
            visibleY = 20f; // 화면에 보이는 위치
            currentY = hiddenY;

            // 텍스처 생성
            CreateTextures();

            // 텍스트 스타일 초기화
            InitializeTextStyle();
        }

        void CreateTextures()
        {
            // 박스 배경 텍스처
            boxTexture = new Texture2D(1, 1);
            boxTexture.SetPixel(0, 0, boxColor);
            boxTexture.Apply();

            // 테두리 텍스처
            borderTexture = new Texture2D(1, 1);
            borderTexture.SetPixel(0, 0, borderColor);
            borderTexture.Apply();
        }

        void InitializeTextStyle()
        {
            textStyle = new GUIStyle();
            textStyle.fontSize = fontSize;
            textStyle.normal.textColor = textColor;
            textStyle.alignment = TextAnchor.MiddleLeft;
            textStyle.wordWrap = true;

            if (notificationFont != null)
            {
                textStyle.font = notificationFont;
            }
        }

        /// <summary>
        /// 드래곤 변심 알림 애니메이션을 시작하는 메서드
        /// </summary>
        public void ShowNotification()
        {
            if (!isAnimating)
            {
                StartCoroutine(PlayNotificationAnimation());
            }
        }

        /// <summary>
        /// 커스텀 메시지로 알림을 표시하는 메서드
        /// </summary>
        /// <param name="customMessage">표시할 커스텀 메시지</param>
        public void ShowNotification(string customMessage)
        {
            if (!isAnimating)
            {
                notificationText = customMessage;
                StartCoroutine(PlayNotificationAnimation());
            }
        }

        IEnumerator PlayNotificationAnimation()
        {
            isAnimating = true;
            animationTime = 0f;

            // 1단계: 내려오기
            float slideDownTime = (animationDuration - stayDuration) / 2f;
            while (animationTime < slideDownTime)
            {
                float progress = animationTime / slideDownTime;
                float curveValue = animationCurve.Evaluate(progress);
                currentY = Mathf.Lerp(hiddenY, visibleY, curveValue);

                animationTime += Time.deltaTime;
                yield return null;
            }

            currentY = visibleY;

            // 2단계: 머무르기
            yield return new WaitForSeconds(stayDuration);

            // 3단계: 올라가기
            animationTime = 0f;
            float slideUpTime = (animationDuration - stayDuration) / 2f;
            while (animationTime < slideUpTime)
            {
                float progress = animationTime / slideUpTime;
                float curveValue = animationCurve.Evaluate(progress);
                currentY = Mathf.Lerp(visibleY, hiddenY, curveValue);

                animationTime += Time.deltaTime;
                yield return null;
            }

            currentY = hiddenY;
            isAnimating = false;

            // 원래 텍스트로 복원
            notificationText = "음... 다른게 땡기네...";
        }

        void OnGUI()
        {
            if (!isAnimating) return;

            // 박스 그리기
            DrawNotificationBox();
        }

        void DrawNotificationBox()
        {
            float boxX = centerX;
            float boxY = currentY;

            // 테두리 그리기 (박스보다 약간 크게)
            float borderWidth = 3f;
            GUI.DrawTexture(new Rect(boxX - borderWidth, boxY - borderWidth,
                                   boxWidth + borderWidth * 2, boxHeight + borderWidth * 2),
                          borderTexture);

            // 박스 배경 그리기
            GUI.DrawTexture(new Rect(boxX, boxY, boxWidth, boxHeight), boxTexture);

            // 드래곤 이미지 그리기
            float imageSize = boxHeight - 20f; // 박스 높이에서 여백 빼기
            float imageX = boxX + 10f;
            float imageY = boxY + 10f;

            if (dragonImage != null)
            {
                GUI.DrawTexture(new Rect(imageX, imageY, imageSize, imageSize), dragonImage);
            }
            else
            {
                // 드래곤 이미지가 없으면 기본 사각형 표시
                GUI.DrawTexture(new Rect(imageX, imageY, imageSize, imageSize), borderTexture);

                // 기본 텍스트 표시
                GUIStyle defaultStyle = new GUIStyle();
                defaultStyle.normal.textColor = Color.black;
                defaultStyle.alignment = TextAnchor.MiddleCenter;
                defaultStyle.fontSize = 12;
                GUI.Label(new Rect(imageX, imageY, imageSize, imageSize), "Dragon", defaultStyle);
            }

            // 텍스트 그리기
            float textX = imageX + imageSize + 15f;
            float textY = boxY + 10f;
            float textWidth = boxWidth - (textX - boxX) - 10f;
            float textHeight = boxHeight - 20f;

            if (textStyle != null)
            {
                GUI.Label(new Rect(textX, textY, textWidth, textHeight), notificationText, textStyle);
            }
            else
            {
                // 기본 스타일로 텍스트 표시
                GUIStyle defaultTextStyle = new GUIStyle();
                defaultTextStyle.normal.textColor = textColor;
                defaultTextStyle.fontSize = fontSize;
                defaultTextStyle.alignment = TextAnchor.MiddleLeft;
                defaultTextStyle.wordWrap = true;
                GUI.Label(new Rect(textX, textY, textWidth, textHeight), notificationText, defaultTextStyle);
            }
        }

        /// <summary>
        /// 현재 애니메이션이 진행 중인지 확인하는 메서드
        /// </summary>
        /// <returns>애니메이션 진행 중이면 true</returns>
        public bool IsAnimating()
        {
            return isAnimating;
        }

        /// <summary>
        /// 애니메이션을 강제로 중단하는 메서드
        /// </summary>
        public void StopAnimation()
        {
            if (isAnimating)
            {
                StopAllCoroutines();
                isAnimating = false;
                currentY = hiddenY;
                notificationText = "음... 다른게 땡기네...";
            }
        }

        void OnDestroy()
        {
            // 생성한 텍스처 정리
            if (boxTexture != null)
            {
                DestroyImmediate(boxTexture);
            }
            if (borderTexture != null)
            {
                DestroyImmediate(borderTexture);
            }
        }

        // 인스펙터에서 실시간으로 값 변경 시 적용되도록 하는 메서드들
        void OnValidate()
        {
            if (Application.isPlaying)
            {
                // 위치 재계산
                centerX = (Screen.width - boxWidth) / 2f;
                hiddenY = -boxHeight - 10f;
                visibleY = 20f;

                // 텍스트 스타일 업데이트
                if (textStyle != null)
                {
                    textStyle.fontSize = fontSize;
                    textStyle.normal.textColor = textColor;
                    if (notificationFont != null)
                    {
                        textStyle.font = notificationFont;
                    }
                }

                // 텍스처 업데이트
                UpdateTextures();
            }
        }

        void UpdateTextures()
        {
            if (boxTexture != null)
            {
                boxTexture.SetPixel(0, 0, boxColor);
                boxTexture.Apply();
            }

            if (borderTexture != null)
            {
                borderTexture.SetPixel(0, 0, borderColor);
                borderTexture.Apply();
            }
        }
    }
}