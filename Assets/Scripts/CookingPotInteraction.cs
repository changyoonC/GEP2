using UnityEngine;
using UnityEngine.UI;

public class CookingPotInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 5f;
    public LayerMask playerLayer = -1;
    
    [Header("UI Elements")]
    public GameObject interactionUI;
    public Text interactionText;
    
    [Header("Cooking Settings")]
    public Transform ingredientDropPoint;
    public ParticleSystem cookingEffect;
    
    private bool playerInRange = false;
    private PlayerControl playerController;
    private PlayerInteractionManager playerManager;
    private GameObject currentPlayer;
    
    void Start()
    {
        // UI 초기화
        if (interactionUI != null)
            interactionUI.SetActive(false);
            
        if (interactionText == null && interactionUI != null)
            interactionText = interactionUI.GetComponentInChildren<Text>();
            
        if (interactionText != null)
            interactionText.text = "Press SPACE to cook";
            
        // 재료 드롭 포인트가 없으면 솥 위쪽으로 설정
        if (ingredientDropPoint == null)
        {
            GameObject dropPoint = new GameObject("IngredientDropPoint");
            dropPoint.transform.SetParent(transform);
            dropPoint.transform.localPosition = Vector3.up * 2f;
            ingredientDropPoint = dropPoint.transform;
        }
    }
    
    void Update()
    {
        CheckForPlayer();
        
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            InteractWithPot();
        }
    }
    
    void CheckForPlayer()
    {
        Collider[] players = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
        
        bool foundPlayer = false;
        foreach (Collider col in players)
        {
            if (col.CompareTag("Player") || col.name == "Player")
            {
                foundPlayer = true;
                currentPlayer = col.gameObject;
                playerController = col.GetComponent<PlayerControl>();
                playerManager = col.GetComponent<PlayerInteractionManager>();
                break;
            }
        }
        
        if (foundPlayer && !playerInRange)
        {
            playerInRange = true;
            ShowInteractionUI(true);
        }
        else if (!foundPlayer && playerInRange)
        {
            playerInRange = false;
            ShowInteractionUI(false);
            currentPlayer = null;
            playerController = null;
            playerManager = null;
        }
    }
    
    void InteractWithPot()
    {
        if (playerManager == null) return;
        
        // 플레이어가 재료를 가지고 있는지 확인
        bool hasIngredient = playerManager.HasIngredient();
        
        if (hasIngredient)
        {
            // 재료를 솥에 넣기
            string ingredientType = playerManager.GetIngredientType();
            bool success = playerManager.UseIngredient();
            
            if (success)
            {
                AddIngredientToPot(ingredientType);
                Debug.Log($"Added {ingredientType} to cooking pot!");
                
                // 요리 효과 재생
                if (cookingEffect != null)
                    cookingEffect.Play();
                    
                // 성공 메시지 표시
                if (interactionText != null)
                {
                    StartCoroutine(ShowTemporaryMessage($"Added {ingredientType}!"));
                }
            }
        }
        else
        {
            Debug.Log("No ingredients to cook!");
            // UI에 메시지 표시
            if (interactionText != null)
            {
                StartCoroutine(ShowTemporaryMessage("No ingredients!"));
            }
        }
    }
    
    void AddIngredientToPot(string ingredientType)
    {
        // 재료를 솥에 추가하는 로직
        // 여기에 기존 요리 시스템 로직을 구현하세요
        
        // 예시: 재료 타입에 따른 처리
        switch (ingredientType.ToLower())
        {
            case "meat":
                Debug.Log("Added meat to the pot");
                break;
            case "potato":
                Debug.Log("Added potato to the pot");
                break;
            case "Broccoli":
                Debug.Log("Added Broccoli to the pot");
                break;
            default:
                Debug.Log($"Added {ingredientType} to the pot");
                break;
        }
        
        // 요리 진행도 업데이트 등의 로직을 여기에 추가
    }
    
    void ShowInteractionUI(bool show)
    {
        if (interactionUI != null)
            interactionUI.SetActive(show);
    }
    
    System.Collections.IEnumerator ShowTemporaryMessage(string message)
    {
        if (interactionText == null) yield break;
        
        string originalMessage = interactionText.text;
        interactionText.text = message;
        yield return new WaitForSeconds(1.5f);
        
        // 플레이어가 여전히 범위 안에 있으면 원래 메시지로 복원
        if (playerInRange)
            interactionText.text = originalMessage;
    }
    
    void OnDrawGizmosSelected()
    {
        // 상호작용 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}