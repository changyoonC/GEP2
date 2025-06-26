using UnityEngine;

public class CookingPotFixed : MonoBehaviour
{
    [Header("설정")]
    public float interactionDistance = 8f;
    
    [Header("디버그")]
    public bool showDebugInfo = true;
    
    private GameObject player;
    private bool playerNearby = false;
    
    void Start()
    {
        Debug.Log("CookingPotFixed 시작 - 솥 위치: " + transform.position);
        
        // 플레이어 찾기
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log("플레이어 발견: " + player.name);
        }
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
        
        // 상태 변화 시에만 로그 출력
        if (playerNearby != wasNearby)
        {
            if (playerNearby)
            {
                Debug.Log($"플레이어가 솥 근처에 왔습니다! 거리: {distance:F1}m");
                Debug.Log("스페이스바를 눌러 상호작용하세요!");
            }
            else
            {
                Debug.Log("플레이어가 솥에서 멀어졌습니다.");
            }
        }
        
        if (showDebugInfo && Time.frameCount % 60 == 0) // 1초마다
        {
            Debug.Log($"현재 거리: {distance:F1}m, 상호작용 가능: {playerNearby}");
        }
    }
    
    void TryInteract()
    {
        if (player == null)
        {
            Debug.LogError("플레이어를 찾을 수 없습니다!");
            return;
        }
        
        if (!playerNearby)
        {
            Debug.Log("솥에 너무 멀리 있습니다! 더 가까이 오세요.");
            return;
        }
        
        Debug.Log("✅ 솥과 상호작용 성공!");
        
        // 플레이어의 재료 확인
        PlayerInteractionManager playerManager = player.GetComponent<PlayerInteractionManager>();
        if (playerManager != null)
        {
            if (playerManager.HasIngredient())
            {
                string ingredient = playerManager.GetIngredientType();
                playerManager.UseIngredient();
                Debug.Log($"🍲 {ingredient}를 솥에 넣었습니다!");
            }
            else
            {
                Debug.Log("❌ 재료가 없습니다! I키를 눌러 재료를 얻으세요.");
            }
        }
        else
        {
            Debug.Log("⚠️ PlayerInteractionManager를 찾을 수 없습니다!");
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
                playerManager.PickupIngredient("감자");
                Debug.Log("감자를 얻었습니다!");
            }
        }
    }
    
    // 기즈모로 상호작용 범위 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}