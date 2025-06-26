using UnityEngine;
using System.Reflection;

public class WorkingPotInteraction : MonoBehaviour
{
    [Header("설정")]
    public float interactionRange = 5f;
    
    private GameObject player;
    private PlayerControl playerControl;
    private GameCore.CookingPot cookingPot;
    private bool playerInRange = false;
    private bool playerHasItems = false;
    
    void Start()
    {
        Debug.Log("WorkingPotInteraction 시작!");
        
        // 플레이어 찾기
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player를 찾을 수 없습니다!");
            return;
        }
        
        playerControl = player.GetComponent<PlayerControl>();
        if (playerControl == null)
        {
            Debug.LogError("PlayerControl을 찾을 수 없습니다!");
            return;
        }
        
        cookingPot = GetComponent<GameCore.CookingPot>();
        if (cookingPot == null)
        {
            Debug.LogError("CookingPot을 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log("모든 컴포넌트 찾기 완료!");
    }
    
    void Update()
    {
        if (player == null || playerControl == null) return;
        
        CheckPlayerDistance();
        CheckPlayerItems();
        
        // 스페이스바 입력 확인
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("스페이스바 눌림!");
            
            if (playerInRange && playerHasItems)
            {
                TryAddIngredient();
            }
            else
            {
                Debug.Log($"상호작용 불가 - 범위안: {playerInRange}, 아이템: {playerHasItems}");
            }
        }
    }
    
    void CheckPlayerDistance()
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRange;
        
        if (playerInRange != wasInRange)
        {
            if (playerInRange)
            {
                Debug.Log($"플레이어가 솥 범위에 들어왔습니다! 거리: {distance:F1}m");
            }
            else
            {
                Debug.Log("플레이어가 솥 범위를 벗어났습니다.");
            }
        }
    }
    
    void CheckPlayerItems()
    {
        int itemCount = playerControl.GetCarriedItemCount();
        bool hasItems = itemCount > 0;
        
        if (hasItems != playerHasItems)
        {
            playerHasItems = hasItems;
            Debug.Log($"플레이어 아이템 상태 변경: {itemCount}개 보유");
        }
    }
    
    void TryAddIngredient()
    {
        Debug.Log("재료 추가 시도!");
        
        // Reflection으로 carried_items 가져오기
        var carriedItemsField = typeof(PlayerControl).GetField("carried_items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (carriedItemsField == null)
        {
            Debug.LogError("carried_items 필드를 찾을 수 없습니다!");
            return;
        }
        
        var carriedItems = (System.Collections.Generic.List<GameObject>)carriedItemsField.GetValue(playerControl);
        if (carriedItems == null || carriedItems.Count == 0)
        {
            Debug.Log("들고 있는 아이템이 없습니다!");
            return;
        }
        
        // 맨 위 아이템 가져오기
        GameObject item = carriedItems[carriedItems.Count - 1];
        carriedItems.RemoveAt(carriedItems.Count - 1);
        
        // 아이템을 플레이어에서 분리
        item.transform.parent = null;
        
        Debug.Log($"아이템 {item.name} 제거 완료. 남은 아이템: {carriedItems.Count}");
        
        // 솥에 아이템 추가
        ProcessItemInPot(item);
    }
    
    void ProcessItemInPot(GameObject item)
    {
        Debug.Log($"솥에 {item.name} 처리 시작");
        
        // ProcessItem 메서드 호출 시도
        var processItemMethod = typeof(GameCore.CookingPot).GetMethod("ProcessItem", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (processItemMethod != null)
        {
            Debug.Log("ProcessItem 메서드 발견, 호출 중...");
            processItemMethod.Invoke(cookingPot, new object[] { item });
            Debug.Log("ProcessItem 호출 완료!");
        }
        else
        {
            Debug.LogWarning("ProcessItem 메서드를 찾을 수 없어 아이템을 삭제합니다.");
            Destroy(item);
        }
    }
    
    void OnGUI()
    {
        if (playerInRange && playerHasItems)
        {
            // 화면 중앙 하단에 UI 표시
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            
            // 배경
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(Screen.width/2 - 150, Screen.height - 80, 300, 40), "");
            
            // 텍스트
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width/2 - 150, Screen.height - 80, 300, 40), "솥에 재료 넣기 (Space)", style);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}