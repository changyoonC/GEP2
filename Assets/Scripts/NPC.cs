using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GameCore
{
    public enum NPCState
    {
        Idle,
        MovingToPlant,
        Harvesting,
        MovingToPot,
        ReturningToZone,
        MovingToItem,
        MovingToCenter,        // Center로 이동 중 (솥 가기 전)
        MovingToCenterAfterPot // Center로 이동 중 (솥 다녀온 후)
    }

    public class NPC : MonoBehaviour
    {
        [Header("NPC 기본 설정")]
        public float moveSpeed = 3f;
        public float workRadius = 10f;
        public float harvestTime = 5f;
        public int maxCarryItems = 3;
        public float potDeliveryDistance = 3f; // 솥 배달 거리

        [Header("Center 경유지 설정")]
        public float centerDetectionRange = 50f; // Center 감지 범위 (넓게 설정)

        [Header("상태 표시")]
        public NPCState currentState = NPCState.Idle;
        public CropZone assignedZone = null;

        // 내부 변수들
        [HideInInspector] public List<GameObject> carriedItems = new List<GameObject>();
        private UniversalPlant targetPlant = null;
        private CookingPot cookingPot = null;
        private bool isPlayerControlled = false;
        private Rigidbody rb;
        private float lastThrowTime = 0f;
        private Vector3 lastWorkArea;
        private bool hasLastWorkArea = false;
        private string lastWorkItemType = "";
        private GameObject targetItem = null;

        // 수확 관련
        private bool isHarvesting = false;
        private float harvestProgress = 0f;
        private float harvestStartTime = 0f;

        // AI 타이머
        private float nextActionTime = 0f;
        private float actionInterval = 0.1f;

        // 플레이어 던지기 관련
        private bool wasThrown = false;
        private bool checkingLanding = false;
        private float landingCheckTime = 0f;
        private Vector3 throwStartPosition;

        // Center 경유지 관련
        private GameObject rememberedCenter = null; // 기억해놓은 Center
        private bool shouldGoThroughCenter = false; // Center를 거쳐야 하는지

        // 아이템 집기 관련 (새로 추가)
        private bool isPickingUpItem = false; // 아이템을 집는 중인지

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.linearDamping = 5f;
            }

            cookingPot = FindObjectOfType<CookingPot>();
            nextActionTime = Time.time + 2f;
        }

        void Update()
        {
            if (isPlayerControlled) return;

            // 던져진 후 착지 확인
            if (checkingLanding)
            {
                CheckForLanding();
            }

            if (isHarvesting)
            {
                UpdateHarvestProgress();
            }

            if (Time.time >= nextActionTime)
            {
                UpdateAI();
                nextActionTime = Time.time + actionInterval;
            }
        }

        void FixedUpdate()
        {
            if (isPlayerControlled) return;

            if (currentState == NPCState.MovingToPlant)
            {
                MoveToPlant();
            }
            else if (currentState == NPCState.MovingToPot)
            {
                MoveToPot();
            }
            else if (currentState == NPCState.ReturningToZone)
            {
                ReturnToWorkArea();
            }
            else if (currentState == NPCState.MovingToItem)
            {
                MoveToItem();
            }
            else if (currentState == NPCState.MovingToCenter)
            {
                MoveToCenter();
            }
            else if (currentState == NPCState.MovingToCenterAfterPot)
            {
                MoveToCenter();
            }
        }

        void CheckForLanding()
        {
            // 바닥에 닿았는지 확인
            bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);

            // 속도가 거의 멈췄고 바닥에 닿았으면 착지로 판정
            if (isGrounded && rb != null && rb.linearVelocity.magnitude < 0.5f)
            {
                // 던져진 시작점에서 충분히 멀리 떨어진 곳에 착지했으면 새로운 작업 구역으로 설정
                float distanceFromStart = Vector3.Distance(transform.position, throwStartPosition);
                if (distanceFromStart > 3f) // 최소 3유닛 이상 이동했을 때만
                {
                    SetNewWorkArea(transform.position);
                    Debug.Log($"[NPC] 새로운 작업 구역 설정: {transform.position}");
                }

                checkingLanding = false;
                wasThrown = false;
                currentState = NPCState.Idle;
                nextActionTime = Time.time + 1f; // 1초 후 작업 시작
            }
            else
            {
                // 너무 오래 착지를 기다렸으면 포기
                landingCheckTime += Time.deltaTime;
                if (landingCheckTime > 5f) // 5초 후 포기
                {
                    checkingLanding = false;
                    wasThrown = false;
                    currentState = NPCState.Idle;
                }
            }
        }

        void SetNewWorkArea(Vector3 position)
        {
            lastWorkArea = position;
            hasLastWorkArea = true;
            lastWorkItemType = ""; // 새로운 구역이므로 이전 작물 타입 초기화
            Debug.Log($"[NPC] 새로운 작업 구역 설정됨: {position}");
        }

        void UpdateAI()
        {
            switch (currentState)
            {
                case NPCState.Idle:
                    DecideWhatToDo();
                    break;
                case NPCState.MovingToPlant:
                    CheckPlantArrival();
                    break;
                case NPCState.MovingToPot:
                    CheckPotArrival();
                    break;
                case NPCState.ReturningToZone:
                    CheckWorkAreaArrival();
                    break;
                case NPCState.MovingToItem:
                    CheckItemArrival();
                    break;
                case NPCState.MovingToCenter:
                    CheckCenterArrival();
                    break;
                case NPCState.MovingToCenterAfterPot:
                    CheckCenterAfterPotArrival();
                    break;
            }
        }

        void DecideWhatToDo()
        {
            // 아이템을 집는 중이면 기다리기 (새로 추가)
            if (isPickingUpItem)
            {
                Debug.Log("[NPC] 아이템 집는 중 - 대기");
                return;
            }

            Debug.Log($"[NPC] 행동 결정 - 상태: {currentState}, 아이템: {carriedItems.Count}개, 작업구역: {(hasLastWorkArea ? "있음" : "없음")}, 종류: {lastWorkItemType}");

            // 1. 아이템을 들고 있으면 Center를 거쳐서 냄비로 가기
            if (carriedItems.Count > 0)
            {
                Debug.Log("[NPC] 아이템을 들고 있음 - Center 찾기 시작");

                // 가장 가까운 Center 찾기
                GameObject nearestCenter = FindNearestCenter();
                if (nearestCenter != null)
                {
                    rememberedCenter = nearestCenter;
                    shouldGoThroughCenter = true;
                    Debug.Log($"[NPC] Center 경유해서 냄비로 이동: {nearestCenter.name}");
                    currentState = NPCState.MovingToCenter;
                }
                else
                {
                    // Center가 없으면 바로 냄비로
                    Debug.Log($"[NPC] Center 없음 (감지범위: {centerDetectionRange}) - 바로 냄비로 이동");
                    currentState = NPCState.MovingToPot;
                }
                return;
            }

            // 2. 근처에 떨어진 아이템이 있으면 바로 줍기
            GameObject nearbyItem = FindNearbyItem();
            if (nearbyItem != null)
            {
                Debug.Log($"[NPC] 근처 아이템 줍기: {nearbyItem.name}");
                StartCoroutine(PickupItemCoroutine(nearbyItem)); // 수정됨
                return;
            }

            // 3. 현재 위치 근처에서 가장 가까운 식물 수확
            UniversalPlant nearestPlant = FindNearestPlantFromCurrentPosition();
            if (nearestPlant != null)
            {
                Debug.Log($"[NPC] 현재 위치에서 가장 가까운 식물 수확: {nearestPlant.name}");
                targetPlant = nearestPlant;
                currentState = NPCState.MovingToPlant;
                return;
            }

            // 4. 마지막 작업 구역이 있으면 그곳의 아이템 확인
            if (hasLastWorkArea)
            {
                GameObject workAreaItem = FindItemInWorkArea();
                if (workAreaItem != null)
                {
                    Debug.Log($"[NPC] 작업 구역 아이템으로 이동: {workAreaItem.name}");
                    targetItem = workAreaItem;
                    currentState = NPCState.MovingToItem;
                    return;
                }
                else
                {
                    // 작업 구역에 아이템이 없으면 같은 종류 수확하기
                    UniversalPlant sameTypePlant = FindSameTypePlant();
                    if (sameTypePlant != null)
                    {
                        Debug.Log($"[NPC] 같은 종류 식물 수확: {sameTypePlant.name}");
                        targetPlant = sameTypePlant;
                        currentState = NPCState.MovingToPlant;
                        return;
                    }
                }
            }

            // 5. 마지막 작업 구역으로 돌아가기
            if (hasLastWorkArea && !IsNearWorkArea())
            {
                Debug.Log("[NPC] 작업 구역으로 돌아가는 중");
                currentState = NPCState.ReturningToZone;
                return;
            }

            // 6. 할당된 구역으로 돌아가기
            if (!hasLastWorkArea && assignedZone != null && !IsNearAssignedZone())
            {
                Debug.Log("[NPC] 할당된 구역으로 돌아가는 중");
                currentState = NPCState.ReturningToZone;
                return;
            }

            Debug.Log("[NPC] 계속 아이템 찾는 중...");
        }

        // 새로운 메서드: 가장 가까운 Center 찾기
        GameObject FindNearestCenter()
        {
            GameObject[] centers = GameObject.FindGameObjectsWithTag("Center");

            Debug.Log($"[NPC] 전체 Center 개수: {centers.Length}");

            if (centers.Length == 0)
            {
                Debug.LogWarning("[NPC] Scene에 'Center' 태그를 가진 오브젝트가 없습니다!");
                return null;
            }

            GameObject closestCenter = null;
            float closestDistance = float.MaxValue;
            int centersInRange = 0;

            foreach (GameObject center in centers)
            {
                float distance = Vector3.Distance(transform.position, center.transform.position);
                Debug.Log($"[NPC] Center '{center.name}' 거리: {distance:F1f}");

                // 감지 범위 내에 있는 Center만 고려
                if (distance <= centerDetectionRange)
                {
                    centersInRange++;
                    if (distance < closestDistance)
                    {
                        closestCenter = center;
                        closestDistance = distance;
                    }
                }
            }

            Debug.Log($"[NPC] 감지 범위({centerDetectionRange}) 내 Center 개수: {centersInRange}");

            if (closestCenter != null)
            {
                Debug.Log($"[NPC] 선택된 Center: {closestCenter.name} (거리: {closestDistance:F1f})");
            }
            else
            {
                Debug.LogWarning($"[NPC] {centerDetectionRange} 범위 내에 Center가 없습니다!");
            }

            return closestCenter;
        }

        void MoveToCenter()
        {
            if (rememberedCenter == null || rb == null) return;

            Vector3 centerPos = rememberedCenter.transform.position;
            Vector3 direction = (centerPos - transform.position).normalized;
            direction.y = 0;

            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * 5f);
            }
        }

        void CheckCenterArrival()
        {
            if (rememberedCenter == null)
            {
                currentState = NPCState.Idle;
                return;
            }

            float distance = Vector3.Distance(transform.position, rememberedCenter.transform.position);
            if (distance <= 2f)
            {
                Debug.Log("[NPC] Center 도착 - 이제 솥으로 이동");
                currentState = NPCState.MovingToPot;
            }
        }

        void CheckCenterAfterPotArrival()
        {
            if (rememberedCenter == null)
            {
                currentState = NPCState.Idle;
                return;
            }

            float distance = Vector3.Distance(transform.position, rememberedCenter.transform.position);
            if (distance <= 2f)
            {
                Debug.Log("[NPC] Center 복귀 완료 - 이제 작업 구역으로 돌아가기");
                rememberedCenter = null; // Center 기억 초기화
                shouldGoThroughCenter = false;
                currentState = NPCState.ReturningToZone;
            }
        }

        UniversalPlant FindNearestPlantFromCurrentPosition()
        {
            UniversalPlant[] allPlants = FindObjectsOfType<UniversalPlant>();
            UniversalPlant closestPlant = null;
            float closestDistance = float.MaxValue;

            foreach (UniversalPlant plant in allPlants)
            {
                if (!plant.CanHarvest()) continue;

                float distance = Vector3.Distance(transform.position, plant.transform.position);

                // 현재 위치에서 가장 가까운 수확 가능한 식물 찾기
                if (distance <= workRadius * 1.5f && distance < closestDistance)
                {
                    closestPlant = plant;
                    closestDistance = distance;
                }
            }

            return closestPlant;
        }

        GameObject FindNearbyItem()
        {
            if (Time.time - lastThrowTime < 0.5f)
            {
                return null;
            }

            Collider[] items = Physics.OverlapSphere(transform.position, 4f, LayerMask.GetMask("Item"));

            GameObject closestItem = null;
            float closestDistance = float.MaxValue;

            foreach (Collider item in items)
            {
                if (item.transform.parent == null)
                {
                    float distance = Vector3.Distance(transform.position, item.transform.position);
                    if (distance < closestDistance)
                    {
                        closestItem = item.gameObject;
                        closestDistance = distance;
                    }
                }
            }

            return closestItem;
        }

        GameObject FindItemInWorkArea()
        {
            if (!hasLastWorkArea) return null;

            Collider[] items = Physics.OverlapSphere(lastWorkArea, 12f, LayerMask.GetMask("Item"));

            GameObject bestItem = null;
            float closestDistance = float.MaxValue;

            foreach (Collider item in items)
            {
                if (item.transform.parent == null)
                {
                    float distance = Vector3.Distance(transform.position, item.transform.position);

                    // 같은 종류의 아이템 우선 (더 관대한 조건)
                    if (!string.IsNullOrEmpty(lastWorkItemType))
                    {
                        string itemType = item.name.Replace("(Clone)", "").Replace(" ", "");
                        string workType = lastWorkItemType.Replace("Plant", "").Replace(" ", "");

                        if (itemType.Contains(workType) || workType.Contains(itemType))
                        {
                            Debug.Log($"[NPC] 같은 종류 아이템 발견: {item.name}");
                            return item.gameObject;
                        }
                    }

                    // 가장 가까운 아이템
                    if (distance < closestDistance)
                    {
                        bestItem = item.gameObject;
                        closestDistance = distance;
                    }
                }
            }

            return bestItem;
        }

        UniversalPlant FindSameTypePlant()
        {
            if (string.IsNullOrEmpty(lastWorkItemType)) return null;

            UniversalPlant[] allPlants = FindObjectsOfType<UniversalPlant>();
            UniversalPlant closestPlant = null;
            float closestDistance = float.MaxValue;

            foreach (UniversalPlant plant in allPlants)
            {
                if (!plant.CanHarvest()) continue;

                // 같은 종류의 식물인지 확인
                if (!plant.name.Contains(lastWorkItemType)) continue;

                float distance = Vector3.Distance(transform.position, plant.transform.position);

                // 작업 반경 내에 있고 가장 가까운 식물
                if (distance <= workRadius * 2f && distance < closestDistance)
                {
                    closestPlant = plant;
                    closestDistance = distance;
                }
            }

            return closestPlant;
        }

        void MoveToPlant()
        {
            if (targetPlant == null || rb == null) return;

            Vector3 targetPos = targetPlant.transform.position;
            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0;

            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * 5f);
            }
        }

        void MoveToItem()
        {
            if (targetItem == null || rb == null)
            {
                currentState = NPCState.Idle;
                return;
            }

            Vector3 targetPos = targetItem.transform.position;
            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0;

            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * 5f);
            }
        }

        void MoveToPot()
        {
            if (cookingPot == null || rb == null) return;

            Vector3 potPos = cookingPot.transform.position;
            Vector3 direction = (potPos - transform.position).normalized;
            direction.y = 0;

            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * 5f);
            }
        }

        void ReturnToWorkArea()
        {
            Vector3 targetPos = hasLastWorkArea ? lastWorkArea : (assignedZone != null ? assignedZone.transform.position : transform.position);

            if (rb == null) return;

            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0;

            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * 5f);
            }
        }

        void CheckPlantArrival()
        {
            if (targetPlant == null)
            {
                currentState = NPCState.Idle;
                return;
            }

            float distance = Vector3.Distance(transform.position, targetPlant.transform.position);
            if (distance <= 2f)
            {
                StartHarvesting();
            }
        }

        void CheckItemArrival()
        {
            if (targetItem == null)
            {
                currentState = NPCState.Idle;
                return;
            }

            float distance = Vector3.Distance(transform.position, targetItem.transform.position);
            if (distance <= 2f)
            {
                StartCoroutine(PickupItemCoroutine(targetItem)); // 수정됨
                targetItem = null;
                currentState = NPCState.Idle;
            }
        }

        void CheckPotArrival()
        {
            if (cookingPot == null) return;

            float distance = Vector3.Distance(transform.position, cookingPot.transform.position);
            if (distance <= potDeliveryDistance)
            {
                DeliverItemsToPot();
            }
        }

        void CheckWorkAreaArrival()
        {
            bool arrived = false;

            if (hasLastWorkArea && IsNearWorkArea())
            {
                arrived = true;
            }
            else if (!hasLastWorkArea && IsNearAssignedZone())
            {
                arrived = true;
            }

            if (arrived)
            {
                currentState = NPCState.Idle;
            }
        }

        bool IsNearWorkArea()
        {
            if (!hasLastWorkArea) return true;
            float distance = Vector3.Distance(transform.position, lastWorkArea);
            return distance <= 8f;
        }

        bool IsNearAssignedZone()
        {
            if (assignedZone == null) return true;
            float distance = Vector3.Distance(transform.position, assignedZone.transform.position);
            return distance <= assignedZone.spawnRadius + 2f;
        }

        void StartHarvesting()
        {
            if (targetPlant == null || !targetPlant.CanHarvest())
            {
                currentState = NPCState.Idle;
                targetPlant = null;
                return;
            }

            // 작업 구역과 아이템 종류 기록 (기존 작업 구역이 없을 때만)
            if (!hasLastWorkArea)
            {
                lastWorkArea = transform.position;
                hasLastWorkArea = true;
            }
            lastWorkItemType = targetPlant.name.Replace("(Clone)", "");

            currentState = NPCState.Harvesting;
            isHarvesting = true;
            harvestProgress = 0f;
            harvestStartTime = Time.time;
        }

        void UpdateHarvestProgress()
        {
            if (!isHarvesting) return;

            harvestProgress = (Time.time - harvestStartTime) / harvestTime;

            if (harvestProgress >= 1f)
            {
                CompleteHarvest();
            }
        }

        void CompleteHarvest()
        {
            isHarvesting = false;
            harvestProgress = 0f;

            if (targetPlant != null && targetPlant.CanHarvest())
            {
                targetPlant.TryHarvest();
            }

            targetPlant = null;
            currentState = NPCState.Idle;
        }

        // 개선된 아이템 집기 코루틴 (새로 추가)
        IEnumerator PickupItemCoroutine(GameObject item)
        {
            if (carriedItems.Count >= maxCarryItems || item == null)
            {
                Debug.LogWarning("[NPC] 아이템을 집을 수 없음 - 용량 초과 또는 아이템 없음");
                yield break;
            }

            isPickingUpItem = true;
            Debug.Log($"[NPC] 아이템 집기 시작: {item.name}");

            // 1. 아이템의 모든 물리 컴포넌트 저장 및 비활성화
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            Collider itemCollider = item.GetComponent<Collider>();

            // 아이템에 붙어있는 다른 스크립트들도 확인
            MonoBehaviour[] itemScripts = item.GetComponents<MonoBehaviour>();
            List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

            // UniversalItem을 제외한 다른 스크립트들 임시 비활성화
            foreach (MonoBehaviour script in itemScripts)
            {
                if (script != null && !(script is UniversalItem) && script.enabled)
                {
                    script.enabled = false;
                    disabledScripts.Add(script);
                    Debug.Log($"[NPC] {script.GetType().Name} 스크립트 임시 비활성화");
                }
            }

            // 물리 상태 완전히 비활성화
            if (itemRb != null)
            {
                itemRb.isKinematic = true;
                itemRb.useGravity = false;
                itemRb.linearVelocity = Vector3.zero;
                itemRb.angularVelocity = Vector3.zero;
                itemRb.detectCollisions = false; // 충돌 감지도 비활성화
                Debug.Log($"[NPC] {item.name} Rigidbody 완전 비활성화");
            }

            if (itemCollider != null)
            {
                itemCollider.enabled = false;
                Debug.Log($"[NPC] {item.name} Collider 비활성화");
            }

            // 2. 여러 프레임 대기 (물리 상태 완전 적용)
            yield return null;
            yield return null;

            // 3. 아이템을 리스트에 추가
            carriedItems.Add(item);
            int itemIndex = carriedItems.Count - 1;

            // 4. Parent 설정 전 위치 기록
            Vector3 originalWorldPosition = item.transform.position;
            Debug.Log($"[NPC] {item.name} 원래 위치: {originalWorldPosition}");

            // 5. Parent 설정
            item.transform.parent = transform;
            Debug.Log($"[NPC] {item.name} Parent 설정 완료");

            // 6. 한 프레임 더 대기
            yield return null;

            // 7. 목표 위치 계산
            Vector3 targetLocalPosition = new Vector3(0, 2.5f + itemIndex * 0.4f, 0);
            Debug.Log($"[NPC] {item.name} 목표 위치: {targetLocalPosition}");

            // 8. 강제 위치 설정 (즉시)
            item.transform.localPosition = targetLocalPosition;
            item.transform.localRotation = Quaternion.identity;

            // 9. 위치 고정을 위한 연속 체크 (0.5초간)
            float fixTime = 0f;
            while (fixTime < 0.5f)
            {
                // 위치가 목표에서 벗어났으면 강제로 되돌리기
                if (Vector3.Distance(item.transform.localPosition, targetLocalPosition) > 0.1f)
                {
                    Debug.LogWarning($"[NPC] {item.name} 위치 이탈 감지! 강제 복구: {item.transform.localPosition} -> {targetLocalPosition}");
                    item.transform.localPosition = targetLocalPosition;
                    item.transform.localRotation = Quaternion.identity;

                    // 물리 상태 재확인
                    if (itemRb != null)
                    {
                        itemRb.isKinematic = true;
                        itemRb.useGravity = false;
                        itemRb.linearVelocity = Vector3.zero;
                        itemRb.angularVelocity = Vector3.zero;
                    }
                }

                fixTime += Time.deltaTime;
                yield return null;
            }

            // 10. 최종 확인 및 고정
            item.transform.localPosition = targetLocalPosition;
            item.transform.localRotation = Quaternion.identity;

            // 11. 물리 상태 최종 확인
            if (itemRb != null)
            {
                itemRb.isKinematic = true;
                itemRb.useGravity = false;
                itemRb.linearVelocity = Vector3.zero;
                itemRb.angularVelocity = Vector3.zero;
                itemRb.detectCollisions = false;
            }

            // 12. 필요하다면 일부 스크립트만 다시 활성화 (UniversalItem은 유지)
            // 다른 스크립트들은 비활성화 상태 유지하여 간섭 방지

            Debug.Log($"[NPC] 아이템 집기 완료: {item.name} (최종위치: {item.transform.localPosition})");

            isPickingUpItem = false;
        }

        // 기존 PickupItem 메서드 (호환성을 위해 유지, 수정됨)
        void PickupItem(GameObject item)
        {
            StartCoroutine(PickupItemCoroutine(item));
        }

        void DeliverItemsToPot()
        {
            if (cookingPot == null || carriedItems.Count == 0) return;

            Debug.Log($"[NPC] {carriedItems.Count}개 아이템 솥에 자동 배달");

            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                GameObject item = carriedItems[i];
                carriedItems.RemoveAt(i);

                // 아이템에서 UniversalItem 컴포넌트 가져오기
                UniversalItem universalItem = item.GetComponent<UniversalItem>();
                if (universalItem != null)
                {
                    // cropType을 직접 사용 (실제 작물 타입)
                    CropType cropType = universalItem.cropType;

                    // CookingPot에 실제 cropType 추가
                    bool success = cookingPot.AddItem(cropType);
                    Debug.Log($"[NPC] {cropType} 아이템을 솥에 추가: {(success ? "성공" : "실패")}");
                }
                else
                {
                    Debug.LogWarning($"[NPC] {item.name}에서 UniversalItem 컴포넌트를 찾을 수 없음");
                }

                // 아이템 오브젝트 제거
                Destroy(item);
            }

            // 배달 완료 후 다음 작업 준비
            if (shouldGoThroughCenter && rememberedCenter != null)
            {
                // Center를 거쳐서 왔다면 다시 Center로 돌아가기
                Debug.Log("[NPC] 솥 배달 완료 - Center로 복귀");
                currentState = NPCState.MovingToCenterAfterPot;
            }
            else if (hasLastWorkArea)
            {
                // Center를 거치지 않았다면 바로 작업 구역으로
                Debug.Log("[NPC] 솥 배달 완료 - 작업 구역으로 복귀");
                currentState = NPCState.ReturningToZone;
            }
            else if (assignedZone != null)
            {
                currentState = NPCState.ReturningToZone;
            }
            else
            {
                currentState = NPCState.Idle;
            }

            nextActionTime = Time.time + 0.2f;
        }
        void LateUpdate()
        {
            if (isPlayerControlled) return;

            // 들고 있는 아이템들의 위치 지속적으로 보정
            for (int i = 0; i < carriedItems.Count; i++)
            {
                if (carriedItems[i] != null)
                {
                    Vector3 correctPosition = new Vector3(0, 2.5f + i * 0.4f, 0);

                    // 위치가 크게 벗어났으면 강제 보정
                    if (Vector3.Distance(carriedItems[i].transform.localPosition, correctPosition) > 0.2f)
                    {
                        Debug.LogWarning($"[NPC] {carriedItems[i].name} 위치 보정: {carriedItems[i].transform.localPosition} -> {correctPosition}");
                        carriedItems[i].transform.localPosition = correctPosition;
                        carriedItems[i].transform.localRotation = Quaternion.identity;

                        // 물리 상태 재확인
                        Rigidbody rb = carriedItems[i].GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }
                }
            }
        }
        public void OnPickedUpByPlayer()
        {
            isPlayerControlled = true;
            isHarvesting = false;
            harvestProgress = 0f;
            currentState = NPCState.Idle;

            // 아이템 집기 중단 (새로 추가)
            isPickingUpItem = false;
            StopAllCoroutines();

            // 새로운 던지기를 위해 이전 작업 구역 정보를 초기화합니다.
            // 이렇게 해야 플레이어가 다시 던졌을 때 새로운 위치를 작업 구역으로 기억합니다.
            hasLastWorkArea = false;

            // 던져지기 시작할 때의 위치 기록
            throwStartPosition = transform.position;
            wasThrown = true;

            // Center 관련 초기화
            rememberedCenter = null;
            shouldGoThroughCenter = false;

            DropAllItems();
        }

        public void OnDroppedByPlayer()
        {
            isPlayerControlled = false;

            // 아이템 집기 중단 (새로 추가)
            isPickingUpItem = false;
            StopAllCoroutines();

            // 던져진 상태라면 착지를 기다림
            if (wasThrown)
            {
                checkingLanding = true;
                landingCheckTime = 0f;
                Debug.Log("[NPC] 던져짐 - 착지 대기 중...");
            }
            else
            {
                // 그냥 놓여진 경우 즉시 작업 시작
                currentState = NPCState.Idle;
                nextActionTime = Time.time + 0.5f;
            }

            // 타겟 초기화
            targetPlant = null;
            targetItem = null;

            // Center 관련 초기화
            rememberedCenter = null;
            shouldGoThroughCenter = false;
        }

        void DropAllItems()
        {
            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                GameObject item = carriedItems[i];
                carriedItems.RemoveAt(i);

                item.transform.parent = null;
                item.transform.position = transform.position + Vector3.up + Random.insideUnitSphere;

                Rigidbody itemRb = item.GetComponent<Rigidbody>();
                if (itemRb != null)
                {
                    itemRb.isKinematic = false;
                    itemRb.useGravity = true;
                }

                Collider col = item.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }

        public void AssignToZone(CropZone zone)
        {
            assignedZone = zone;
        }

        public void RemoveFromZone()
        {
            if (assignedZone != null)
            {
                assignedZone = null;
            }
        }

        void OnGUI()
        {
            if (!isHarvesting || Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);

            if (screenPos.z > 0)
            {
                float barWidth = 100f;
                float barHeight = 10f;
                float x = screenPos.x - barWidth / 2f;
                float y = Screen.height - screenPos.y - barHeight / 2f;

                GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.gray, 0, 0);

                float progressWidth = barWidth * harvestProgress;
                GUI.DrawTexture(new Rect(x, y, progressWidth, barHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.yellow, 0, 0);

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;

                GUI.Label(new Rect(x, y - 20f, barWidth, 15f), $"수확 중... {harvestProgress * 100f:F0}%", style);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, workRadius);

            // Center 감지 범위 표시
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, centerDetectionRange);

            if (targetPlant != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPlant.transform.position);
            }

            if (hasLastWorkArea)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lastWorkArea);
                Gizmos.DrawWireSphere(lastWorkArea, 8f);

                // 착지 대기 중이면 다른 색으로 표시
                if (checkingLanding)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(lastWorkArea + Vector3.up * 2f, Vector3.one);
                }
            }

            if (cookingPot != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(cookingPot.transform.position, potDeliveryDistance);
            }

            // 던져진 시작점 표시
            if (wasThrown)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(throwStartPosition, 1f);
                Gizmos.DrawLine(throwStartPosition, transform.position);
            }

            // 기억해놓은 Center 표시
            if (rememberedCenter != null)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(rememberedCenter.transform.position, 2f);
                Gizmos.DrawLine(transform.position, rememberedCenter.transform.position);

                // Center를 거쳐서 솥으로 가는 경로 표시
                if (cookingPot != null && shouldGoThroughCenter)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(rememberedCenter.transform.position, cookingPot.transform.position);
                }
            }

            // 범위 내 모든 Center들을 표시
            GameObject[] allCenters = GameObject.FindGameObjectsWithTag("Center");
            foreach (GameObject center in allCenters)
            {
                float distance = Vector3.Distance(transform.position, center.transform.position);

                if (distance <= centerDetectionRange)
                {
                    // 범위 내 Center는 초록색
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(center.transform.position, Vector3.one * 2f);
                }
                else
                {
                    // 범위 밖 Center는 빨간색
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(center.transform.position, Vector3.one * 1f);
                }
            }

            // 아이템 집기 중이면 표시 (새로 추가)
            if (isPickingUpItem)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 4f, Vector3.one * 0.5f);
            }
        }
    }
}