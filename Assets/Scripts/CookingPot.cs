using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    [RequireComponent(typeof(AudioSource))]
    public class CookingPot : MonoBehaviour
    {
        [Header("Sounds")]
        public AudioClip ingredientAddSound;

        [Header("Detection Settings")]
        public float detectionRadius = 2f;
        public LayerMask itemLayerMask = 8;

        [Header("Recipe Data")]
        public TextAsset recipeJsonFile;

        [Header("Effects")]
        public GameObject cookingEffect;
        public GameObject completionEffect;

        [Header("GUI Settings")]
        public GUIStyle titleStyle;
        public GUIStyle ingredientStyle;
        public GUIStyle progressStyle;

        [Header("상단 알림 UI")]
        public Texture2D notificationDragonImage;
        public Font dragonFont;
        public float notificationSlideTime = 0.4f; // 슬라이드 인/아웃 시간
        public float notificationStayTime = 2.7f; // 내려와서 머무는 시간 (public)
        private float notificationDuration => notificationSlideTime * 2 + notificationStayTime;
        private float notificationEndTime = 0f;
        private bool showNotification = false;
        private string notificationMessage = "";
        private Texture2D notificationImage = null;

        private Recipe currentRecipe;
        private RecipeDatabase recipeDatabase;
        private bool isCooking = false;
        private GameManager gameManager;
        private float recipeStartTime = 0f;

        // 드래곤 변심 이벤트 관련 변수들
        private float lastMoodCheckTime = 0f; // 마지막 변심 체크 시간
        private bool hasAnyIngredient = false;   // 현재 레시피에 재료가 하나라도 들어갔는지

        private float notificationAnimY = -120f; // 알림 박스의 현재 y 위치(애니메이션)
        private float notificationTargetY = 10f; // 목표 y 위치(더 위쪽)
        private float notificationStartY = -120f; // 시작 y 위치(화면 위)
        private float notificationAnimSpeed = 10f; // 애니메이션 속도

        void Start()
        {
            LoadRecipes();
            AssignRandomRecipe();
            InitializeGUIStyles();
            gameManager = FindObjectOfType<GameManager>();
        }

        void InitializeGUIStyles()
        {
            if (titleStyle == null) titleStyle = new GUIStyle();
            if (ingredientStyle == null) ingredientStyle = new GUIStyle();
            if (progressStyle == null) progressStyle = new GUIStyle();

            titleStyle.fontSize = 20;
            titleStyle.normal.textColor = Color.white;
            ingredientStyle.fontSize = 16;
            ingredientStyle.normal.textColor = Color.white;
            progressStyle.fontSize = 14;
            progressStyle.normal.textColor = Color.green;
        }

        void LoadRecipes()
        {
            if (recipeJsonFile != null)
            {
                string jsonString = recipeJsonFile.text;
                recipeDatabase = JsonUtility.FromJson<RecipeDatabase>(jsonString);
                Debug.Log($"Loaded {recipeDatabase.recipes.Count} recipes");
            }
            else
            {
                Debug.LogError("Recipe JSON file not found!");
                CreateDefaultRecipe();
            }
        }

        void CreateDefaultRecipe()
        {
            recipeDatabase = new RecipeDatabase();
            recipeDatabase.recipes = new List<Recipe>();

            Recipe defaultRecipe = new Recipe();
            defaultRecipe.id = 1;
            defaultRecipe.name = "Default Recipe";
            defaultRecipe.description = "Basic cooking recipe";
            defaultRecipe.ingredients = new List<Ingredient>();
            defaultRecipe.rewardPoints = 100;

            Ingredient berryIngredient = new Ingredient();
            berryIngredient.cropType = CropType.Broccoli;
            berryIngredient.requiredAmount = 3;
            berryIngredient.currentAmount = 0;

            defaultRecipe.ingredients.Add(berryIngredient);
            recipeDatabase.recipes.Add(defaultRecipe);
        }

        void AssignRandomRecipe()
        {
            if (recipeDatabase != null && recipeDatabase.recipes.Count > 0)
            {
                int randomIndex = Random.Range(0, recipeDatabase.recipes.Count);
                currentRecipe = recipeDatabase.recipes[randomIndex];

                foreach (var ingredient in currentRecipe.ingredients)
                {
                    ingredient.currentAmount = 0;
                }

                recipeStartTime = Time.time; // 레시피 시작 시각 기록

                // 드래곤 변심 이벤트 관련 변수 리셋
                ResetMoodChangeVariables();

                Debug.Log($"New recipe assigned: {currentRecipe.name}");
            }
        }

        void ResetMoodChangeVariables()
        {
            lastMoodCheckTime = 0f;
            hasAnyIngredient = false;
            // GameManager의 드래곤 변심 이벤트도 리셋
            if (gameManager != null)
            {
                gameManager.ResetDragonMoodChangeEvent();
            }
        }

        void Update()
        {
            CheckForItems();
            CheckDragonMoodChange();
            // 알림 시간 체크 및 애니메이션
            if (showNotification)
            {
                float elapsed = Time.time - (notificationEndTime - notificationDuration);
                if (elapsed < notificationSlideTime) // 슬라이드 인
                {
                    float t = Mathf.Clamp01(elapsed / notificationSlideTime);
                    notificationAnimY = Mathf.Lerp(notificationStartY, notificationTargetY, t);
                }
                else if (elapsed < notificationSlideTime + notificationStayTime) // 머무름
                {
                    notificationAnimY = notificationTargetY;
                }
                else // 슬라이드 아웃
                {
                    float t = Mathf.Clamp01((elapsed - notificationSlideTime - notificationStayTime) / notificationSlideTime);
                    notificationAnimY = Mathf.Lerp(notificationTargetY, notificationStartY, t);
                }
            }
            else
            {
                // 사라질 때(슬라이드 아웃)
                notificationAnimY = Mathf.Lerp(notificationAnimY, notificationStartY, Time.deltaTime * notificationAnimSpeed);
            }
            if (showNotification && Time.time > notificationEndTime)
            {
                showNotification = false;
            }
        }

        void CheckForItems()
        {
            if (isCooking || currentRecipe == null) return;

            Collider[] items = Physics.OverlapSphere(transform.position, detectionRadius, itemLayerMask);

            foreach (Collider itemCol in items)
            {
                GameObject item = itemCol.gameObject;

                // Pot 중심을 향해 이동(빨려드는)
                float suckSpeed = 10f; // 빨려드는 속도
                Vector3 dir = (transform.position - item.transform.position).normalized;
                float dist = Vector3.Distance(transform.position, item.transform.position);

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = dir * suckSpeed;
                }
                else
                {
                    item.transform.position = Vector3.MoveTowards(item.transform.position, transform.position, suckSpeed * Time.deltaTime);
                }

                // Pot에 충분히 가까워지면 처리
                if (dist < 0.5f)
                {
                    ProcessItem(item);
                }
            }
        }

        void ProcessItem(GameObject item)
        {
            CropType itemType = GetItemCropType(item);

            foreach (var ingredient in currentRecipe.ingredients)
            {
                if (ingredient.cropType == itemType && !ingredient.IsComplete())
                {
                    ingredient.currentAmount++;
                    Debug.Log($"Added {itemType}! ({ingredient.currentAmount}/{ingredient.requiredAmount})");
                    PlayIngredientAddSound();

                    // 첫 재료가 들어간 시간 기록
                    CheckAndRecordFirstIngredient();

                    Destroy(item);

                    if (currentRecipe.IsComplete())
                    {
                        CompleteCooking();
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// 첫 재료가 들어갔는지 체크하고 시간을 기록하는 메서드
        /// </summary>
        void CheckAndRecordFirstIngredient()
        {
            if (!hasAnyIngredient)
            {
                // 현재 레시피에 재료가 하나라도 들어갔는지 체크
                foreach (var ingredient in currentRecipe.ingredients)
                {
                    if (ingredient.currentAmount > 0)
                    {
                        hasAnyIngredient = true;
                        lastMoodCheckTime = Time.time; // 변심 타이머 시작
                        Debug.Log($"[CookingPot] 첫 재료 투입 시간 기록: {lastMoodCheckTime}");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 드래곤 변심 이벤트가 발생할 조건인지 체크하는 메서드
        /// </summary>
        /// <returns>첫 재료 투입 후 15초가 지났고 재료가 있으면 true</returns>
        public bool ShouldTriggerMoodChangeEvent()
        {
            if (!hasAnyIngredient || lastMoodCheckTime <= 0f)
                return false;

            float elapsedTime = Time.time - lastMoodCheckTime;
            return elapsedTime >= 15f;
        }

        /// <summary>
        /// 현재 들어간 재료를 포함하는 다른 레시피로 변경하는 메서드
        /// </summary>
        /// <returns>성공적으로 변경했으면 true</returns>
        public bool TriggerRecipeChange()
        {
            if (currentRecipe == null || !hasAnyIngredient)
                return false;

            // 현재 들어간 재료들을 수집
            List<CropType> currentIngredients = new List<CropType>();
            foreach (var ingredient in currentRecipe.ingredients)
            {
                if (ingredient.currentAmount > 0)
                {
                    // 들어간 개수만큼 추가
                    for (int i = 0; i < ingredient.currentAmount; i++)
                    {
                        currentIngredients.Add(ingredient.cropType);
                    }
                }
            }

            if (currentIngredients.Count == 0)
                return false;

            // 현재 재료들을 포함하는 다른 레시피 찾기
            Recipe newRecipe = FindRecipeWithIngredients(currentIngredients);

            if (newRecipe != null && newRecipe.id != currentRecipe.id)
            {
                Debug.Log($"[CookingPot] 레시피 변경: {currentRecipe.name} -> {newRecipe.name}");

                // 새 레시피로 변경
                Recipe oldRecipe = currentRecipe;
                currentRecipe = newRecipe;

                // 새 레시피의 재료 상태 초기화
                foreach (var ingredient in currentRecipe.ingredients)
                {
                    ingredient.currentAmount = 0;
                }

                // 기존 재료들을 새 레시피에 적용
                foreach (CropType cropType in currentIngredients)
                {
                    foreach (var ingredient in currentRecipe.ingredients)
                    {
                        if (ingredient.cropType == cropType && !ingredient.IsComplete())
                        {
                            ingredient.currentAmount++;
                            break;
                        }
                    }
                }

                // 드래곤 변심 이벤트 관련 변수는 유지 (이벤트 중이므로)
                // firstIngredientTime과 hasAnyIngredient는 그대로 두어 이벤트가 중복 발생하지 않도록 함

                return true;
            }

            return false;
        }

        /// <summary>
        /// 특정 재료들을 포함할 수 있는 레시피를 찾는 메서드
        /// </summary>
        /// <param name="availableIngredients">사용 가능한 재료들</param>
        /// <returns>조건에 맞는 레시피, 없으면 null</returns>
        Recipe FindRecipeWithIngredients(List<CropType> availableIngredients)
        {
            if (recipeDatabase == null || recipeDatabase.recipes == null)
                return null;

            // 현재 레시피가 아닌 다른 레시피들 중에서 찾기
            var otherRecipes = recipeDatabase.recipes.Where(r => r.id != currentRecipe.id).ToList();

            foreach (Recipe recipe in otherRecipes)
            {
                bool canMakeRecipe = true;
                List<CropType> tempIngredients = new List<CropType>(availableIngredients);

                // 이 레시피의 모든 재료가 사용 가능한 재료로 만들어질 수 있는지 체크
                foreach (var ingredient in recipe.ingredients)
                {
                    int needed = ingredient.requiredAmount;
                    int available = tempIngredients.Count(c => c == ingredient.cropType);

                    if (available < needed)
                    {
                        canMakeRecipe = false;
                        break;
                    }

                    // 사용된 재료는 임시 리스트에서 제거
                    for (int i = 0; i < needed; i++)
                    {
                        tempIngredients.Remove(ingredient.cropType);
                    }
                }

                if (canMakeRecipe)
                {
                    Debug.Log($"[CookingPot] 변경 가능한 레시피 발견: {recipe.name}");
                    return recipe;
                }
            }

            // 완전히 매칭되는 레시피가 없으면, 일부 재료라도 활용할 수 있는 레시피 찾기
            foreach (Recipe recipe in otherRecipes)
            {
                foreach (var ingredient in recipe.ingredients)
                {
                    if (availableIngredients.Contains(ingredient.cropType))
                    {
                        Debug.Log($"[CookingPot] 부분적으로 활용 가능한 레시피 발견: {recipe.name}");
                        return recipe;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// NPC 또는 다른 시스템에서 아이템을 직접 솥에 추가하는 메서드
        /// </summary>
        /// <param name="itemType">추가할 아이템의 타입</param>
        /// <returns>성공적으로 추가되었는지 여부</returns>
        public bool AddItem(Item.TYPE itemType)
        {
            if (isCooking || currentRecipe == null)
            {
                Debug.Log($"[CookingPot] 요리 중이거나 레시피가 없어서 {itemType} 추가 실패");
                return false;
            }

            // Item.TYPE.PLANT는 실제 UniversalItem의 cropType을 사용해야 하므로
            // 이 메서드보다는 CropType을 직접 받는 AddItem(CropType) 사용을 권장
            CropType cropType = ConvertItemTypeToCropType(itemType);

            foreach (var ingredient in currentRecipe.ingredients)
            {
                if (ingredient.cropType == cropType && !ingredient.IsComplete())
                {
                    ingredient.currentAmount++;
                    Debug.Log($"[CookingPot] {cropType} 추가됨! ({ingredient.currentAmount}/{ingredient.requiredAmount})");
                    PlayIngredientAddSound();

                    // 첫 재료가 들어간 시간 기록
                    CheckAndRecordFirstIngredient();

                    if (currentRecipe.IsComplete())
                    {
                        CompleteCooking();
                    }
                    return true;
                }
            }

            Debug.Log($"[CookingPot] {cropType}은 현재 레시피에 필요하지 않거나 이미 충분함");
            return false;
        }

        /// <summary>
        /// CropType을 직접 받는 AddItem 오버로드
        /// </summary>
        /// <param name="cropType">추가할 작물 타입</param>
        /// <returns>성공적으로 추가되었는지 여부</returns>
        public bool AddItem(CropType cropType)
        {
            if (isCooking || currentRecipe == null)
            {
                Debug.Log($"[CookingPot] 요리 중이거나 레시피가 없어서 {cropType} 추가 실패");
                return false;
            }

            foreach (var ingredient in currentRecipe.ingredients)
            {
                if (ingredient.cropType == cropType && !ingredient.IsComplete())
                {
                    ingredient.currentAmount++;
                    Debug.Log($"[CookingPot] {cropType} 추가됨! ({ingredient.currentAmount}/{ingredient.requiredAmount})");
                    PlayIngredientAddSound();

                    // 첫 재료가 들어간 시간 기록
                    CheckAndRecordFirstIngredient();

                    if (currentRecipe.IsComplete())
                    {
                        CompleteCooking();
                    }
                    return true;
                }
            }

            Debug.Log($"[CookingPot] {cropType}은 현재 레시피에 필요하지 않거나 이미 충분함");
            return false;
        }

        /// <summary>
        /// Item.TYPE을 CropType으로 변환하는 헬퍼 메서드
        /// </summary>
        /// <param name="itemType">변환할 Item.TYPE</param>
        /// <returns>해당하는 CropType</returns>
        private CropType ConvertItemTypeToCropType(Item.TYPE itemType)
        {
            switch (itemType)
            {
                case Item.TYPE.PLANT:
                    return CropType.Broccoli; // 기본값으로 Broccoli 사용
                default:
                    Debug.LogWarning($"[CookingPot] 알 수 없는 Item.TYPE: {itemType}, Broccoli로 기본 설정");
                    return CropType.Broccoli;
            }
        }

        CropType GetItemCropType(GameObject item)
        {
            var universal = item.GetComponent<UniversalItem>();
            if (universal != null)
                return universal.cropType;

            // 이름으로 보조 판별 (예외 상황 대비)
            if (item.name.Contains("Broccoli"))
                return CropType.Broccoli;
            if (item.name.Contains("Mushroom"))
                return CropType.Mushroom;
            if (item.name.Contains("Sunflower"))
                return CropType.Sunflower;
            if (item.name.Contains("Carrot"))
                return CropType.Carrot;
            if (item.name.Contains("Cauliflower"))
                return CropType.Cauliflower;
            if (item.name.Contains("Potato"))
                return CropType.Potato;
            if (item.name.Contains("Corn"))
                return CropType.Corn;

            return CropType.Broccoli; // 기본값
        }

        void CompleteCooking()
        {
            isCooking = false;

            if (cookingEffect != null)
                cookingEffect.SetActive(false);

            if (completionEffect != null)
            {
                completionEffect.SetActive(true);
                StartCoroutine(HideCompletionEffect());
            }

            // 걸린 시간 계산 및 보너스 점수
            float elapsed = Time.time - recipeStartTime;
            float bonus = Mathf.Max(0, 100 - elapsed * 5); // 20초 이내 완성시 최대 100점, 1초 지날 때마다 5점씩 감소(최소 0점)
            int totalPoints = currentRecipe.rewardPoints + Mathf.RoundToInt(bonus);

            Debug.Log($"{currentRecipe.name} completed! Earned {totalPoints} points! (기본:{currentRecipe.rewardPoints}, 보너스:{Mathf.RoundToInt(bonus)})");

            if (gameManager != null)
            {
                gameManager.OnRecipeCompleted(totalPoints);
            }

            AssignRandomRecipe();
            // 변심 관련 변수 리셋
            hasAnyIngredient = false;
            lastMoodCheckTime = 0f;
        }

        IEnumerator HideCompletionEffect()
        {
            yield return new WaitForSeconds(3f);
            if (completionEffect != null)
                completionEffect.SetActive(false);
        }

        public void ShowNotification(string message, Texture2D image = null, float stayTime = 2.7f)
        {
            this.notificationMessage = message;
            this.notificationImage = image;
            this.notificationStayTime = stayTime;
            this.showNotification = true;
            this.notificationEndTime = Time.time + this.notificationDuration;
            this.notificationAnimY = this.notificationStartY; // 애니메이션 시작 위치로 초기화
        }

        void OnGUI()
        {
            if (currentRecipe == null) return;

            // 드래곤 변심 알림 박스 (슬라이드 애니메이션)
            float boxWidth = 400f;
            float boxHeight = 100f;
            float x = (Screen.width - boxWidth) / 2f;
            float y = notificationAnimY;
            if (showNotification || notificationAnimY > notificationStartY + 1f)
            {
                // 둥근 네모 박스 (배경)
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = MakeRoundRectTexture(16, new Color(0,0,0,0.8f));
                boxStyle.border = new RectOffset(16,16,16,16);
                boxStyle.alignment = TextAnchor.MiddleLeft;
                boxStyle.padding = new RectOffset(20,20,20,20);
                GUI.Box(new Rect(x, y, boxWidth, boxHeight), GUIContent.none, boxStyle);

                // 흰색 반투명 테두리 (네 방향 직선만)
                DrawRectBorder(new Rect(x, y, boxWidth, boxHeight), new Color(1,1,1,0.5f), 3);

                // 이미지와 문구
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.fontSize = 34;
                textStyle.normal.textColor = Color.white;
                if (dragonFont != null) textStyle.font = dragonFont;

                if (notificationImage != null)
                {
                    // 이미지가 있을 때
                    float imgSize = 64f;
                    GUI.DrawTexture(new Rect(x + 20, y + 18, imgSize, imgSize), notificationImage, ScaleMode.ScaleToFit, true);
                    textStyle.alignment = TextAnchor.MiddleLeft;
                    GUI.Label(new Rect(x + 100, y + 18, boxWidth - 120, 64), notificationMessage, textStyle);
                }
                else
                {
                    // 이미지가 없을 때 (텍스트 중앙 정렬)
                    textStyle.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(x, y, boxWidth, boxHeight), notificationMessage, textStyle);
                }
            }

            // --- 우측 상단 레시피 UI ---
            float recipeUIX = Screen.width - 300f;
            float recipeUIY = 20f;
            float recipeUIWidth = 280f;
            float recipeUIPadding = 10f;

            // 1. 내용물에 따라 박스 높이를 동적으로 계산
            float recipeContentHeight = 0f;
            recipeContentHeight += 35f; // Title(30) + Spacing(5)
            recipeContentHeight += 25f; // Subtitle(20) + Spacing(5)
            recipeContentHeight += currentRecipe.ingredients.Count * 22f; // Each ingredient line

            float recipeBoxHeight = recipeContentHeight;
            if (currentRecipe.ingredients.Count == 0)
            {
                // 재료가 없을 때를 대비한 최소 높이
                recipeBoxHeight += 10f;
            }
            // 마지막 아이템 아래의 불필요한 여백(2px)을 빼서 타이트하게 보이게 함
            else
            {
                recipeBoxHeight -= 2f;
            }
            recipeBoxHeight += recipeUIPadding * 2; // 상하 패딩 추가

            // 2. 계산된 높이로 배경 박스를 먼저 그림
            GUI.Box(new Rect(recipeUIX - recipeUIPadding, recipeUIY - recipeUIPadding, recipeUIWidth + recipeUIPadding * 2, recipeBoxHeight), "");

            // 3. 박스 위에 내용물을 그림
            float currentY = recipeUIY;
            GUI.Label(new Rect(recipeUIX, currentY, recipeUIWidth, 30), currentRecipe.name, titleStyle);
            currentY += 35;

            GUI.Label(new Rect(recipeUIX, currentY, recipeUIWidth, 20), "Required Ingredients:", ingredientStyle);
            currentY += 25;

            // GameManager의 이벤트 플래그 확인
            bool hideSomeIngredients = false;
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.isMemoryLostEvent)
                hideSomeIngredients = true;

            int hideCount = 0;
            if (hideSomeIngredients)
                hideCount = Mathf.Max(1, currentRecipe.ingredients.Count / 2); // 절반 가리기

            int hidden = 0;
            foreach (var ingredient in currentRecipe.ingredients)
            {
                string status = ingredient.IsComplete() ? "✓" : "○";
                Color originalColor = ingredientStyle.normal.textColor;
                ingredientStyle.normal.textColor = ingredient.IsComplete() ? Color.green : Color.white;

                string label;
                // ???로 가릴 재료는 아직 currentAmount == 0인 것만 대상으로 함
                if (hideSomeIngredients && hidden < hideCount && ingredient.currentAmount == 0)
                {
                    label = $"{status} ???: {ingredient.currentAmount}/{ingredient.requiredAmount}";
                    hidden++;
                }
                else
                {
                    label = $"{status} {ingredient.cropType}: {ingredient.currentAmount}/{ingredient.requiredAmount}";
                }

                GUI.Label(new Rect(recipeUIX, currentY, recipeUIWidth, 20), label, ingredientStyle);

                ingredientStyle.normal.textColor = originalColor;
                currentY += 22;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        // 드래곤 변심 이벤트: 10초마다 90% 확률로 레시피 변경
        void CheckDragonMoodChange()
        {
            if (!hasAnyIngredient) return;
            if (currentRecipe == null) return;
            if (gameManager == null) return;

            // 게임 시간이 2분(120초) 미만으로 남았을 때만 변심
            if (gameManager.CurrentGameTime >= 120f)
            {
                return;
            }

            // 재료가 들어간 후 10초마다 체크
            if (lastMoodCheckTime == 0f)
            {
                lastMoodCheckTime = Time.time;
                return;
            }
            if (Time.time - lastMoodCheckTime >= 10f)
            {
                lastMoodCheckTime = Time.time;
                // 90% 확률
                if (Random.value < 0.9f)
                {
                    bool changed = TriggerRecipeChange();
                    if (changed)
                    {
                        Debug.Log("드래곤의 변심! 레시피가 변경되었습니다! (남은 시간 < 120초)");
                        // 알림 표시
                        ShowNotification("음... 다른게 땡기네...", notificationDragonImage);
                    }
                }
            }
        }

        // 둥근 네모 박스용 텍스처 생성
        Texture2D MakeRoundRectTexture(int radius, Color color)
        {
            int size = radius * 2 + 1;
            Texture2D tex = new Texture2D(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    float dist = Mathf.Sqrt(dx*dx + dy*dy);
                    float alpha = dist <= radius ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        // 네 방향 직선 테두리 그리기
        void DrawRectBorder(Rect rect, Color color, int thickness)
        {
            Color prev = GUI.color;
            GUI.color = color;
            // 상단
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            // 하단
            GUI.DrawTexture(new Rect(rect.x, rect.y+rect.height-thickness, rect.width, thickness), Texture2D.whiteTexture);
            // 좌측
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            // 우측
            GUI.DrawTexture(new Rect(rect.x+rect.width-thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void PlayIngredientAddSound()
        {
            AudioClip clipToPlay = ingredientAddSound;

            // public 변수가 비어있으면, AudioSource에 직접 할당된 클립을 찾아봄
            if (clipToPlay == null)
            {
                AudioSource sourceOnPot = GetComponent<AudioSource>();
                if (sourceOnPot != null && sourceOnPot.clip != null)
                {
                    clipToPlay = sourceOnPot.clip;
                }
            }

            if (clipToPlay != null && Camera.main != null)
            {
                // 카메라 위치에서 사운드를 재생하여 2D 사운드처럼 항상 들리게 함
                AudioSource.PlayClipAtPoint(clipToPlay, Camera.main.transform.position);
            }
            else
            {
                if (clipToPlay == null)
                {
                    Debug.LogWarning("솥(CookingPot)의 'Ingredient Add Sound' 또는 AudioSource 컴포넌트의 'Clip'에 오디오 클립이 할당되지 않았습니다!");
                }
                if (Camera.main == null)
                {
                    Debug.LogError("Main Camera를 찾을 수 없습니다. 사운드를 재생할 위치를 알 수 없습니다!");
                }
            }
        }
    }
}