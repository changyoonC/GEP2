using UnityEngine;

public class IngredientThrower : MonoBehaviour
{
    [Header("투척 설정")]
    public float throwForce = 15f;
    public LayerMask itemLayer = 8; // 2^3 = 8
    
    public void ThrowIngredient(string ingredientType, Vector3 fromPosition, Vector3 targetPosition)
    {
        // 기본 큐브로 재료 아이템 생성
        GameObject ingredient = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ingredient.transform.position = fromPosition;
        ingredient.transform.localScale = Vector3.one * 0.3f;
        ingredient.name = ingredientType + "(Clone)";
        
        // 레이어 설정 (기존 CookingPot이 감지할 수 있도록)
        ingredient.layer = 3; // itemLayerMask = 8 = 2^3
        
        // 색상 설정
        Renderer renderer = ingredient.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = GetIngredientColor(ingredientType);
            renderer.material = mat;
        }
        
        // UniversalItem 대신 간단한 태그 사용
        switch (ingredientType.ToLower())
        {
            case "감자":
            case "potato":
                ingredient.name = "Potato(Clone)";
                break;
            case "베리":
            case "Broccoli":
                ingredient.name = "Broccoli(Clone)";
                break;
            default:
                ingredient.name = "Broccoli(Clone)"; // 기본값
                break;
        }
        
        // 솥 방향으로 던지기
        Rigidbody rb = ingredient.GetComponent<Rigidbody>();
        Vector3 direction = (targetPosition - fromPosition).normalized;
        Vector3 throwVector = direction + Vector3.up * 0.5f; // 살짝 위로
        
        rb.AddForce(throwVector * throwForce, ForceMode.Impulse);
        
        Debug.Log($"재료 '{ingredient.name}' 생성 및 투척 완료!");
        
        // 10초 후 자동 삭제 (메모리 관리)
        Destroy(ingredient, 10f);
    }
    
    Color GetIngredientColor(string ingredientType)
    {
        switch (ingredientType.ToLower())
        {
            case "감자":
            case "potato":
                return new Color(0.8f, 0.7f, 0.3f); // 갈색
            case "베리":
            case "Broccoli":
                return Color.red;
            default:
                return Color.red; // 기본값
        }
    }
}