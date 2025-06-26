using UnityEngine;
using System.Reflection;
using Controller; // IntegratedPlayerController 네임스페이스
using GameCore;

public class ForceAdd : MonoBehaviour
{
    public float range = 10f;

    private GameObject player;
    private IntegratedPlayerController playerController; // PlayerControl에서 변경
    private CookingPot cookingPot;
    private bool inRange = false;
    private bool hasItems = false;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<IntegratedPlayerController>(); // 변경된 부분
            if (playerController == null)
            {
                Debug.LogError("IntegratedPlayerController를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }

        cookingPot = GetComponent<CookingPot>();
        if (cookingPot == null)
        {
            Debug.LogError("CookingPot 컴포넌트를 찾을 수 없습니다!");
        }

        Debug.Log("ForceAdd 시작! Player: " + (player != null ? player.name : "null") +
                  ", Controller: " + (playerController != null ? "found" : "null") +
                  ", CookingPot: " + (cookingPot != null ? "found" : "null"));
    }

    void Update()
    {
        CheckAll();

        if (inRange && hasItems && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("스페이스 키 눌림! 강제 재료 추가 시도");
            ForceAddIngredient();
        }
    }

    void CheckAll()
    {
        if (player == null || playerController == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        inRange = dist <= range;
        hasItems = playerController.GetCarriedItemCount() > 0; // 공개 메서드 사용

        // 디버깅 정보
        if (inRange && hasItems && Time.frameCount % 60 == 0) // 1초마다 한 번씩 로그
        {
            Debug.Log($"플레이어 거리: {dist:F2}, 아이템 개수: {playerController.GetCarriedItemCount()}");
        }
    }

    void ForceAddIngredient()
    {
        Debug.Log("강제 재료 추가 시작!");

        // carried_items 필드에 접근
        var field = typeof(IntegratedPlayerController).GetField("carried_items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            var items = (System.Collections.Generic.List<GameObject>)field.GetValue(playerController);
            if (items != null && items.Count > 0)
            {
                GameObject item = items[items.Count - 1];
                Debug.Log($"제거할 아이템: {item.name}");

                // 아이템 타입 확인
                CropType itemType = CropType.Broccoli; // 기본값
                var universalItem = item.GetComponent<UniversalItem>();
                if (universalItem != null)
                {
                    itemType = universalItem.cropType;
                    Debug.Log($"아이템 타입: {itemType}");
                }
                else
                {
                    Debug.LogWarning("UniversalItem 컴포넌트를 찾을 수 없어 기본값(Broccoli) 사용");
                }

                // carried_items에서 제거
                items.RemoveAt(items.Count - 1);
                Debug.Log($"아이템 제거 후 남은 개수: {items.Count}");

                // 아이템 파괴 (OnThrown 먼저 호출하고 하이라이트 끄기)
                if (universalItem != null)
                {
                    universalItem.SetHighlight(false);
                    universalItem.OnThrown();
                }
                Destroy(item);

                // CookingPot에 직접 추가 (공개 메서드 사용)
                bool success = cookingPot.AddItem(itemType);
                if (success)
                {
                    Debug.Log($"{itemType} 성공적으로 추가됨!");
                }
                else
                {
                    Debug.Log($"{itemType} 추가 실패 - 현재 레시피에 필요하지 않거나 이미 충분함");
                }
            }
            else
            {
                Debug.LogWarning("carried_items가 null이거나 비어있음");
            }
        }
        else
        {
            Debug.LogError("carried_items 필드를 찾을 수 없음");
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
            GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height - 80, 300, 40), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 80, 300, 40), "솥에 재료 넣기 (Space)", style);
        }

        // 디버깅 정보 표시
        if (Application.isEditor)
        {
            GUIStyle debugStyle = new GUIStyle();
            debugStyle.fontSize = 12;
            debugStyle.normal.textColor = Color.yellow;

            string debugInfo = $"Range: {inRange}, HasItems: {hasItems}, Items: {(playerController != null ? playerController.GetCarriedItemCount() : 0)}";
            GUI.Label(new Rect(10, 10, 400, 20), debugInfo, debugStyle);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}