using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    public bool enableThrowingSystem = false; // 기존 던지기 시스템 비활성화
    public bool enableSpacebarInteraction = true; // 새로운 스페이스바 상호작용 활성화
    
    [Header("Current Item")]
    public GameObject currentItem; // 현재 플레이어가 들고 있는 아이템
    public Transform itemHoldPoint; // 아이템을 들고 있을 위치
    
    [Header("Inventory")]
    public bool hasIngredient = false; // 재료를 가지고 있는지 여부
    public string ingredientType = ""; // 재료 타입
    
    private PlayerControl playerControl;
    
    void Start()
    {
        playerControl = GetComponent<PlayerControl>();
        
        // 아이템 홀드 포인트가 없으면 플레이어 앞쪽으로 생성
        if (itemHoldPoint == null)
        {
            GameObject holdPoint = new GameObject("ItemHoldPoint");
            holdPoint.transform.SetParent(transform);
            holdPoint.transform.localPosition = Vector3.forward * 1.5f + Vector3.up * 0.5f;
            itemHoldPoint = holdPoint.transform;
        }
    }
    
    void Update()
    {
        // 기존 던지기 시스템 비활성화 처리
        if (!enableThrowingSystem)
        {
            // 여기에 기존 던지기 입력을 무시하는 로직 추가
            // 예: 마우스 클릭 등을 무시
        }
        
        // 디버깅용 키 입력
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleIngredient();
        }
    }
    
    public void PickupIngredient(string ingredientType)
    {
        this.ingredientType = ingredientType;
        hasIngredient = true;
        Debug.Log($"Picked up: {ingredientType}");
    }
    
    public bool UseIngredient()
    {
        if (hasIngredient)
        {
            Debug.Log($"Used ingredient: {ingredientType}");
            hasIngredient = false;
            string usedIngredient = ingredientType;
            ingredientType = "";
            return true;
        }
        return false;
    }
    
    public bool HasIngredient()
    {
        return hasIngredient;
    }
    
    public string GetIngredientType()
    {
        return ingredientType;
    }
    
    // 디버깅용 - I키로 재료 토글
    void ToggleIngredient()
    {
        if (hasIngredient)
        {
            UseIngredient();
        }
        else
        {
            PickupIngredient("TestIngredient");
        }
    }
    
    // 기존 던지기 시스템 비활성화
    public void DisableThrowingSystem()
    {
        enableThrowingSystem = false;
        // 기존 PlayerControl의 던지기 관련 기능 비활성화
        // 이 부분은 기존 PlayerControl 스크립트의 구조에 따라 수정 필요
    }
    
    // 기존 던지기 시스템 활성화 (필요시)
    public void EnableThrowingSystem()
    {
        enableThrowingSystem = true;
    }
}

// CookingPotInteraction에서 사용할 수 있도록 확장
public static class CookingPotInteractionExtensions
{
    public static bool CheckPlayerHasIngredient(this CookingPotInteraction pot, PlayerControl playerControl)
    {
        PlayerInteractionManager manager = playerControl.GetComponent<PlayerInteractionManager>();
        return manager != null && manager.HasIngredient();
    }
    
    public static bool UsePlayerIngredient(this CookingPotInteraction pot, PlayerControl playerControl)
    {
        PlayerInteractionManager manager = playerControl.GetComponent<PlayerInteractionManager>();
        return manager != null && manager.UseIngredient();
    }
}