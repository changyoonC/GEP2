using UnityEngine;

public class CookingPotSpace : MonoBehaviour
{
    [Header("상호작용 설정")]
    public float interactionRange = 5f;
    
    private GameObject player;
    private bool playerInRange = false;
    private PlayerControl playerControl;
    
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerControl = player.GetComponent<PlayerControl>();
        }
    }
    
    void Update()
    {
        CheckPlayerRange();
        
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
        
        if (playerInRange != wasInRange && playerInRange)
        {
            Debug.Log("솥 근처에 왔습니다. 스페이스바를 눌러 재료를 넣으세요!");
        }
    }
    
    void TryAddIngredientToPot()
    {
        if (playerControl == null) return;
        
        // 플레이어가 아이템을 들고 있는지 확인
        int carriedItemCount = playerControl.GetCarriedItemCount();
        if (carriedItemCount == 0)
        {
            Debug.Log("들고 있는 아이템이 없습니다!");
            return;
        }
        
        Debug.Log("솥에 재료를 넣습니다!");
        
        // PlayerControl의 ThrowTopItem() 메서드를 간접적으로 호출
        // 기존 던지기 시스템을 활용하여 솥 방향으로 던지기
        SimulateThrowToPot();
    }
    
    void SimulateThrowToPot()
    {
        // 플레이어를 솥 방향으로 회전시키고 던지기 시뮬레이션
        Vector3 directionToPot = (transform.position - player.transform.position).normalized;
        
        // 플레이어를 솥 방향으로 회전
        Vector3 lookDirection = new Vector3(directionToPot.x, 0, directionToPot.z);
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            player.transform.rotation = targetRotation;
        }
        
        // F키 입력을 시뮬레이션하여 던지기 (Reflection 사용)
        var playerControlType = typeof(PlayerControl);
        
        // isCharging 필드 설정
        var isChargingField = playerControlType.GetField("isCharging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var chargeStartTimeField = playerControlType.GetField("chargeStartTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentChargeTimeField = playerControlType.GetField("currentChargeTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (isChargingField != null && chargeStartTimeField != null && currentChargeTimeField != null)
        {
            // 중간 정도의 충전으로 설정
            isChargingField.SetValue(playerControl, true);
            chargeStartTimeField.SetValue(playerControl, Time.time - 0.4f); // 0.4초 충전
            currentChargeTimeField.SetValue(playerControl, 0.4f);
            
            // ThrowTopItem 메서드 호출
            var throwMethod = playerControlType.GetMethod("ThrowTopItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (throwMethod != null)
            {
                throwMethod.Invoke(playerControl, null);
                Debug.Log("재료를 솥에 던졌습니다!");
            }
            
            // 충전 상태 리셋
            isChargingField.SetValue(playerControl, false);
            currentChargeTimeField.SetValue(playerControl, 0f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}