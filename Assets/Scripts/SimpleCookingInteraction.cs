using UnityEngine;

public class SimpleCookingInteraction : MonoBehaviour
{
    [Header("설정")]
    public float interactionDistance = 8f;
    
    private GameObject player;
    private bool playerNearby = false;
    private IngredientThrower thrower;
    
    void Start()
    {
        Debug.Log("SimpleCookingInteraction 시작 - 솥 위치: " + transform.position);
        
        // 플레이어 찾기
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
        
        // IngredientThrower 컴포넌트 추가
        thrower = gameObject.AddComponent<IngredientThrower>();
    }
    
    void Update()
    {
        CheckPlayerDistance();
        
        // 스페이스바 입력 확인
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("스페이스바 입력 감지!");
            TryInteract();
        }
        
        // 디버그용 - I키로 재료 토글
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleIngredient();
        }
    }
    
    void CheckPlayerDistance()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        
        bool wasNearby = playerNearby;
        playerNearby = distance <= interactionDistance;
        
        if (playerNearby != wasNearby)
        {
            if (playerNearby)
            {
                Debug.Log($"플레이어가 솥 근처에 왔습니다! 거리: {distance:F1}m");
                Debug.Log("스페이스바를 눌러 상호작용하세요!");
            }
        }
    }
    
    void TryInteract()
    {
        if (!playerNearby)
        {
            Debug.Log("솥에 너무 멀리 있습니다!");
            return;
        }
        
        PlayerInteractionManager playerManager = player.GetComponent<PlayerInteractionManager>();
        if (playerManager != null && playerManager.HasIngredient())
        {
            string ingredient = playerManager.GetIngredientType();
            playerManager.UseIngredient();
            
            // 재료를 솥에 던지기
            Vector3 throwFrom = player.transform.position + Vector3.up * 1f;
            thrower.ThrowIngredient(ingredient, throwFrom, transform.position);
            
            Debug.Log($"🍲 {ingredient}를 솥에 던졌습니다!");
        }
        else
        {
            Debug.Log("❌ 재료가 없습니다! I키를 눌러 재료를 얻으세요.");
        }
    }
    
    void ToggleIngredient()
    {
        if (player == null) return;
        
        PlayerInteractionManager playerManager = player.GetComponent<PlayerInteractionManager>();
        if (playerManager != null)
        {
            if (playerManager.HasIngredient())
            {
                playerManager.UseIngredient();
                Debug.Log("재료를 사용했습니다.");
            }
            else
            {
                playerManager.PickupIngredient("Broccoli");
                Debug.Log("베리를 얻었습니다!");
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}