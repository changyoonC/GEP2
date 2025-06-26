using UnityEngine;
using System.Reflection;

public class SimpleDirect : MonoBehaviour
{
    public float range = 10f;
    
    private GameObject player;
    private PlayerControl playerControl;
    private GameCore.CookingPot cookingPot;
    private bool inRange = false;
    private bool hasItems = false;
    
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerControl = player.GetComponent<PlayerControl>();
        cookingPot = GetComponent<GameCore.CookingPot>();
        
        Debug.Log("SimpleDirect 시작!");
    }
    
    void Update()
    {
        CheckAll();
        
        if (inRange && hasItems && Input.GetKeyDown(KeyCode.Space))
        {
            AddDirect();
        }
    }
    
    void CheckAll()
    {
        if (player == null || playerControl == null) return;
        
        float dist = Vector3.Distance(transform.position, player.transform.position);
        inRange = dist <= range;
        hasItems = playerControl.GetCarriedItemCount() > 0;
    }
    
    void AddDirect()
    {
        Debug.Log("직접 추가!");
        
        var field = typeof(PlayerControl).GetField("carried_items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            var items = (System.Collections.Generic.List<GameObject>)field.GetValue(playerControl);
            if (items != null && items.Count > 0)
            {
                GameObject item = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                
                // 타입 확인
                GameCore.CropType itemType = GameCore.CropType.Broccoli;
                var uni = item.GetComponent<GameCore.UniversalItem>();
                if (uni != null) itemType = uni.cropType;
                
                Debug.Log($"{item.name} ({itemType}) 삭제하고 레시피에 추가");
                
                // 아이템 삭제
                Destroy(item);
                
                // 레시피에 추가
                AddToRecipe(itemType);
            }
        }
    }
    
    void AddToRecipe(GameCore.CropType itemType)
    {
        var recipeField = typeof(GameCore.CookingPot).GetField("currentRecipe", BindingFlags.NonPublic | BindingFlags.Instance);
        if (recipeField != null)
        {
            var recipe = recipeField.GetValue(cookingPot);
            if (recipe != null)
            {
                var ingredientsProp = recipe.GetType().GetProperty("ingredients");
                if (ingredientsProp != null)
                {
                    var ingredients = (System.Collections.Generic.List<GameCore.Ingredient>)ingredientsProp.GetValue(recipe);
                    
                    foreach (var ing in ingredients)
                    {
                        if (ing.cropType == itemType && !ing.IsComplete())
                        {
                            ing.currentAmount++;
                            Debug.Log($"추가 완료! {itemType}: {ing.currentAmount}/{ing.requiredAmount}");
                            
                            // 완성 확인
                            var completeMethod = recipe.GetType().GetMethod("IsComplete");
                            if (completeMethod != null)
                            {
                                bool complete = (bool)completeMethod.Invoke(recipe, null);
                                if (complete)
                                {
                                    Debug.Log("요리 완성!");
                                    var finishMethod = typeof(GameCore.CookingPot).GetMethod("CompleteCooking", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (finishMethod != null)
                                        finishMethod.Invoke(cookingPot, null);
                                }
                            }
                            return;
                        }
                    }
                    Debug.Log($"{itemType}는 필요 없음");
                }
            }
        }
    }
    
    void OnGUI()
    {
        if (inRange && hasItems)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(Screen.width/2 - 150, Screen.height - 80, 300, 40), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width/2 - 150, Screen.height - 80, 300, 40), "솥에 재료 넣기 (Space)", style);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}