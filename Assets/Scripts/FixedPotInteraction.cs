using UnityEngine;
using System.Reflection;

public class FixedPotInteraction : MonoBehaviour
{
    public float range = 5f;
    
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
        
        Debug.Log("FixedPotInteraction 준비 완료!");
    }
    
    void Update()
    {
        CheckRange();
        CheckItems();
        
        if (inRange && hasItems && Input.GetKeyDown(KeyCode.Space))
        {
            AddIngredient();
        }
    }
    
    void CheckRange()
    {
        if (player == null) return;
        
        float dist = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = inRange;
        inRange = dist <= range;
        
        if (inRange && !wasInRange)
            Debug.Log("솥 범위 진입!");
    }
    
    void CheckItems()
    {
        if (playerControl == null) return;
        hasItems = playerControl.GetCarriedItemCount() > 0;
    }
    
    void AddIngredient()
    {
        Debug.Log("재료 추가 시작!");
        
        var field = typeof(PlayerControl).GetField("carried_items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            var items = (System.Collections.Generic.List<GameObject>)field.GetValue(playerControl);
            if (items != null && items.Count > 0)
            {
                GameObject item = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                
                // 아이템 완전히 제거하고 물리 복원
                item.transform.parent = null;
                
                // 콜라이더 활성화
                Collider col = item.GetComponent<Collider>();
                if (col != null) col.enabled = true;
                
                // 리지드바디 활성화
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
                
                // UniversalItem OnThrown 호출
                GameCore.UniversalItem uni = item.GetComponent<GameCore.UniversalItem>();
                if (uni != null) uni.OnThrown();
                
                Debug.Log($"{item.name} 물리 복원 완료");
                
                // ProcessItem 직접 호출
                var method = typeof(GameCore.CookingPot).GetMethod("ProcessItem", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(cookingPot, new object[] { item });
                    Debug.Log("ProcessItem 호출 성공!");
                }
                else
                {
                    Debug.Log("ProcessItem 없음, 직접 삭제");
                    Destroy(item);
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}