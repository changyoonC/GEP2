using UnityEngine;
using System.Collections;

namespace GameCore
{
    // NPC의 아이템 집기 기능을 개선하는 헬퍼 클래스
    public class NPCItemPickupHelper : MonoBehaviour
    {
        private NPC npc;
        
        void Awake()
        {
            npc = GetComponent<NPC>();
        }

        // 개선된 아이템 집기 코루틴
        public IEnumerator PickupItemCoroutine(GameObject item)
        {
            if (npc.carriedItems.Count >= npc.maxCarryItems || item == null)
            {
                Debug.LogWarning("[NPC] 아이템을 집을 수 없음 - 용량 초과 또는 아이템 없음");
                yield break;
            }

            Debug.Log($"[NPC] 아이템 집기 시작: {item.name}");

            // 1. 먼저 아이템의 물리 상태를 안전하게 변경
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            Collider itemCollider = item.GetComponent<Collider>();

            // 물리 상태 초기화
            if (itemRb != null)
            {
                itemRb.isKinematic = true;
                itemRb.useGravity = false;
                itemRb.linearVelocity = Vector3.zero;
                itemRb.angularVelocity = Vector3.zero;
            }

            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }

            // 2. 한 프레임 대기 (물리 상태 변경이 적용되도록)
            yield return null;

            // 3. 아이템을 리스트에 추가
            npc.carriedItems.Add(item);
            int itemIndex = npc.carriedItems.Count - 1;

            // 4. Parent 설정
            item.transform.parent = transform;

            // 5. 또 한 프레임 대기
            yield return null;

            // 6. 위치와 회전 설정
            Vector3 targetLocalPosition = new Vector3(0, 2.5f + itemIndex * 0.4f, 0);
            
            // 7. 위치를 부드럽게 이동시키기
            float moveTime = 0f;
            Vector3 startPosition = item.transform.localPosition;
            
            while (moveTime < 0.3f)
            {
                moveTime += Time.deltaTime;
                float progress = moveTime / 0.3f;
                
                item.transform.localPosition = Vector3.Lerp(startPosition, targetLocalPosition, progress);
                item.transform.localRotation = Quaternion.Slerp(item.transform.localRotation, Quaternion.identity, progress);
                
                yield return null;
            }

            // 8. 최종 위치 강제 설정
            item.transform.localPosition = targetLocalPosition;
            item.transform.localRotation = Quaternion.identity;

            // 9. 물리 상태 다시 한번 확인
            if (itemRb != null)
            {
                itemRb.isKinematic = true;
                itemRb.useGravity = false;
            }

            Debug.Log($"[NPC] 아이템 집기 완료: {item.name} (위치: {item.transform.localPosition})");
        }
    }
}