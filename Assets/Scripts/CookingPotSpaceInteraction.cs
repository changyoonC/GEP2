using UnityEngine;

public class CookingPotSpaceInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    public float interactionRange = 5f;
    
    [Header("UI 표시")]
    public bool showInteractionPrompt = true;
    
    private GameObject player;
    private bool playerInRange = false;
    private GameCore.CookingPot cookingPotComponent;
    
    void Start()
    {
        // 기존 CookingPot 컴포넌트 참조
        cookingPotComponent = GetComponent<GameCore.CookingPot>();
        if (cookingPotComponent == null)
        {
            Debug.LogError("GameCore.CookingPot 컴포넌트를 찾을 수 없습니다!");
        }
        
        // 플레이어 찾기
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }
    
    void Update()
    {
        CheckPlayerRange();
        
        // 스페이스바 입력 체크
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            TryAddIngredientToPot();
        }
    }
    
    void CheckPlayerRange()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRange;
        
        // 범위 진입/이탈 시 메시지
        if (playerInRange != wasInRange)
        {
            if (playerInRange)
            {
                Debug.Log("솥 근처에 왔습니다. 스페이스바를 눌러 재료를 넣으세요!");
            }
            else
            {
                Debug.Log("솥에서 멀어졌습니다.");
            }
        }
    }
    
    void TryAddIngredientToPot()
    {
        if (player == null) return;
        
        // 플레이어가 들고 있는 아이템 확인
        GameObject heldItem = GetPlayerHeldItem();
        
        if (heldItem != null)
        {
            Debug.Log($"들고 있는 아이템: {heldItem.name}");
            
            // 기존 CookingPot의 ProcessItem 메서드 호출하여 재료 처리
            if (cookingPotComponent != null)
            {
                // ProcessItem은 private이므로 public 메서드가 필요하거나
                // 아이템을 솥 위치로 이동시켜서 기존 시스템이 감지하도록 함
                MoveItemToPot(heldItem);
            }
        }
        else
        {
            Debug.Log("들고 있는 재료가 없습니다!");
        }
    }
    
    GameObject GetPlayerHeldItem()
    {
        // 플레이어가 들고 있는 아이템을 찾는 로직
        // PlayerControl 스크립트에서 현재 들고 있는 아이템 참조
        
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerControl != null)
        {
            // PlayerControl에서 들고 있는 아이템을 가져오는 방법
            // 이 부분은 기존 PlayerControl 구조에 따라 수정 필요
            
            // 임시로 플레이어 근처의 아이템을 찾아보기
            Collider[] nearbyItems = Physics.OverlapSphere(player.transform.position, 2f);
            foreach (Collider col in nearbyItems)
            {
                // 아이템 레이어나 태그로 확인
                if (col.gameObject.layer == 3 || col.name.Contains("Broccoli") || col.name.Contains("Potato"))
                {
                    return col.gameObject;
                }
            }
        }
        
        return null;
    }
    
    void MoveItemToPot(GameObject item)
    {
        // 아이템을 솥 근처로 순간이동시켜서 기존 감지 시스템이 작동하도록 함
        Vector3 potPosition = transform.position;
        item.transform.position = potPosition + Vector3.up * 1f; // 솥 위쪽으로
        
        // 아이템이 솥으로 떨어지도록 중력 적용
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * 2f; // 아래로 떨어뜨리기
        }
        
        Debug.Log($"아이템 '{item.name}'을 솥에 넣었습니다!");
    }
    
    void OnDrawGizmosSelected()
    {
        // 상호작용 범위 시각화
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
    
    void OnGUI()
    {
        if (!showInteractionPrompt || !playerInRange) return;
        
        // 화면에 상호작용 프롬프트 표시
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        
        string message = "Press SPACE to put ingredient in pot";
        GUI.Label(new Rect(Screen.width/2 - 150, Screen.height - 100, 300, 30), message, style);
    }
}