using UnityEngine;
using System.Collections;

namespace GameCore
{
    public class DragonMoodChangeNotification : MonoBehaviour
    {
        [Header("�巡�� �˸� ����")]
        public Texture2D dragonImage;
        public Font notificationFont;
        public float animationDuration = 3f; // ��ü �ִϸ��̼� �ð�
        public float stayDuration = 2f; // ȭ�鿡 �ӹ��� �ð�
        public float boxWidth = 400f;
        public float boxHeight = 120f;
        public Color boxColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public Color borderColor = Color.white;
        public Color textColor = Color.white;
        public int fontSize = 18;

        [Header("�ִϸ��̼� ����")]
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private bool isAnimating = false;
        private float animationTime = 0f;
        private float currentY = 0f;
        private string notificationText = "��... �ٸ��� �����...";

        // ���� ��ġ��
        private float hiddenY;
        private float visibleY;
        private float centerX;

        private GUIStyle textStyle;
        private Texture2D boxTexture;
        private Texture2D borderTexture;

        void Start()
        {
            // ��ġ ���
            centerX = (Screen.width - boxWidth) / 2f;
            hiddenY = -boxHeight - 10f; // ȭ�� ���� ������ ��ġ
            visibleY = 20f; // ȭ�鿡 ���̴� ��ġ
            currentY = hiddenY;

            // �ؽ�ó ����
            CreateTextures();

            // �ؽ�Ʈ ��Ÿ�� �ʱ�ȭ
            InitializeTextStyle();
        }

        void CreateTextures()
        {
            // �ڽ� ��� �ؽ�ó
            boxTexture = new Texture2D(1, 1);
            boxTexture.SetPixel(0, 0, boxColor);
            boxTexture.Apply();

            // �׵θ� �ؽ�ó
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
        /// �巡�� ���� �˸� �ִϸ��̼��� �����ϴ� �޼���
        /// </summary>
        public void ShowNotification()
        {
            if (!isAnimating)
            {
                StartCoroutine(PlayNotificationAnimation());
            }
        }

        /// <summary>
        /// Ŀ���� �޽����� �˸��� ǥ���ϴ� �޼���
        /// </summary>
        /// <param name="customMessage">ǥ���� Ŀ���� �޽���</param>
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

            // 1�ܰ�: ��������
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

            // 2�ܰ�: �ӹ�����
            yield return new WaitForSeconds(stayDuration);

            // 3�ܰ�: �ö󰡱�
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

            // ���� �ؽ�Ʈ�� ����
            notificationText = "��... �ٸ��� �����...";
        }

        void OnGUI()
        {
            if (!isAnimating) return;

            // �ڽ� �׸���
            DrawNotificationBox();
        }

        void DrawNotificationBox()
        {
            float boxX = centerX;
            float boxY = currentY;

            // �׵θ� �׸��� (�ڽ����� �ణ ũ��)
            float borderWidth = 3f;
            GUI.DrawTexture(new Rect(boxX - borderWidth, boxY - borderWidth,
                                   boxWidth + borderWidth * 2, boxHeight + borderWidth * 2),
                          borderTexture);

            // �ڽ� ��� �׸���
            GUI.DrawTexture(new Rect(boxX, boxY, boxWidth, boxHeight), boxTexture);

            // �巡�� �̹��� �׸���
            float imageSize = boxHeight - 20f; // �ڽ� ���̿��� ���� ����
            float imageX = boxX + 10f;
            float imageY = boxY + 10f;

            if (dragonImage != null)
            {
                GUI.DrawTexture(new Rect(imageX, imageY, imageSize, imageSize), dragonImage);
            }
            else
            {
                // �巡�� �̹����� ������ �⺻ �簢�� ǥ��
                GUI.DrawTexture(new Rect(imageX, imageY, imageSize, imageSize), borderTexture);

                // �⺻ �ؽ�Ʈ ǥ��
                GUIStyle defaultStyle = new GUIStyle();
                defaultStyle.normal.textColor = Color.black;
                defaultStyle.alignment = TextAnchor.MiddleCenter;
                defaultStyle.fontSize = 12;
                GUI.Label(new Rect(imageX, imageY, imageSize, imageSize), "Dragon", defaultStyle);
            }

            // �ؽ�Ʈ �׸���
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
                // �⺻ ��Ÿ�Ϸ� �ؽ�Ʈ ǥ��
                GUIStyle defaultTextStyle = new GUIStyle();
                defaultTextStyle.normal.textColor = textColor;
                defaultTextStyle.fontSize = fontSize;
                defaultTextStyle.alignment = TextAnchor.MiddleLeft;
                defaultTextStyle.wordWrap = true;
                GUI.Label(new Rect(textX, textY, textWidth, textHeight), notificationText, defaultTextStyle);
            }
        }

        /// <summary>
        /// ���� �ִϸ��̼��� ���� ������ Ȯ���ϴ� �޼���
        /// </summary>
        /// <returns>�ִϸ��̼� ���� ���̸� true</returns>
        public bool IsAnimating()
        {
            return isAnimating;
        }

        /// <summary>
        /// �ִϸ��̼��� ������ �ߴ��ϴ� �޼���
        /// </summary>
        public void StopAnimation()
        {
            if (isAnimating)
            {
                StopAllCoroutines();
                isAnimating = false;
                currentY = hiddenY;
                notificationText = "��... �ٸ��� �����...";
            }
        }

        void OnDestroy()
        {
            // ������ �ؽ�ó ����
            if (boxTexture != null)
            {
                DestroyImmediate(boxTexture);
            }
            if (borderTexture != null)
            {
                DestroyImmediate(borderTexture);
            }
        }

        // �ν����Ϳ��� �ǽð����� �� ���� �� ����ǵ��� �ϴ� �޼����
        void OnValidate()
        {
            if (Application.isPlaying)
            {
                // ��ġ ����
                centerX = (Screen.width - boxWidth) / 2f;
                hiddenY = -boxHeight - 10f;
                visibleY = 20f;

                // �ؽ�Ʈ ��Ÿ�� ������Ʈ
                if (textStyle != null)
                {
                    textStyle.fontSize = fontSize;
                    textStyle.normal.textColor = textColor;
                    if (notificationFont != null)
                    {
                        textStyle.font = notificationFont;
                    }
                }

                // �ؽ�ó ������Ʈ
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