using UnityEngine;
using System.Reflection;

public class PotInteractionWithUI : MonoBehaviour
{
    public float range = 5f;
    
    private GameObject player;
    private PlayerControl playerControl;
    private GameCore.CookingPot cookingPot;
    private bool inRange = false;
    private bool hasItems = false;
    
    // UI 관련
    private GameObject uiCanvas;
    
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerControl = player.GetComponent<PlayerControl>();
        cookingPot = GetComponent<GameCore.CookingPot>();
        
        CreateUI();
    }
    
    void CreateUI()
    {
        // World Space Canvas 생성
        uiCanvas = new GameObject("PotUI");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300, 80);
        uiCanvas.transform.localScale = Vector3.one * 0.01f;
        
        // 텍스트 생성
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(uiCanvas.transform);
        
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "솥에 재료 넣기 (Space)";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(300, 80);
        textRect.anchoredPosition = Vector2.zero;
        
        // 배경 추가
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(uiCanvas.transform);
        bg.transform.SetAsFirstSibling();
        
        UnityEngine.UI.Image bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0, 0, 0, 0.8f);
        
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(300, 80);
        bgRect.anchoredPosition = Vector2.zero;
        
        uiCanvas.SetActive(false);
    }
    
    void Update()
    {
        CheckRange();
        CheckItems();
        UpdateUI();
        
        if (inRange && hasItems && Input.GetKeyDown(KeyCode.Space))
            AddIngredient();
    }
    
    void CheckRange()
    {
        if (player == null) return;
        
        float dist = Vector3.Distance(transform.position, player.transform.position);
        inRange = dist <= range;
    }
    
    void CheckItems()
    {
        if (playerControl == null) return;
        hasItems = playerControl.GetCarriedItemCount() > 0;
    }
    
    void UpdateUI()
    {
        if (uiCanvas == null || player == null) return;
        
        bool show = inRange && hasItems;
        uiCanvas.SetActive(show);
        
        if (show)
        {
            // 플레이어 옆에 위치
            Vector3 pos = player.transform.position + Vector3.right * 2f + Vector3.up * 1.5f;
            uiCanvas.transform.position = pos;
            
            // 카메라 바라보기
            if (Camera.main != null)
            {
                Vector3 dir = Camera.main.transform.position - pos;
                dir.y = 0;
                if (dir != Vector3.zero)
                    uiCanvas.transform.rotation = Quaternion.LookRotation(-dir);
            }
        }
    }
    
    void AddIngredient()
    {
        if (playerControl == null) return;
        
        var field = typeof(PlayerControl).GetField("carried_items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            var items = (System.Collections.Generic.List<GameObject>)field.GetValue(playerControl);
            if (items != null && items.Count > 0)
            {
                GameObject item = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                item.transform.parent = null;
                
                var method = typeof(GameCore.CookingPot).GetMethod("ProcessItem", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(cookingPot, new object[] { item });
                }
                else
                {
                    Destroy(item);
                }
                
                Debug.Log($"{item.name}을 솥에 넣었습니다!");
            }
        }
    }
    
    void OnDestroy()
    {
        if (uiCanvas != null)
            Destroy(uiCanvas);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}