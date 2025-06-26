using UnityEngine;
using System.Collections.Generic;
using GameCore;

namespace Controller
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    public class IntegratedPlayerController : MonoBehaviour
    {
        public static float MOVE_AREA_RADIUS = 50.0f;

        [Header("Sounds")]
        public AudioClip harvestSuccessSound;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float rotateSpeed = 540f; // 180에서 540으로 증가 (3배 빠르게)

        [Header("Game Mechanics")]
        [SerializeField] private int maxCarryItems = 3;
        [SerializeField] private float itemStackHeight = 0.8f;
        [SerializeField] private float baseCarryHeight = 1.8f;
        [SerializeField] private float carryForwardOffset = 0.8f;

        [Header("Throwing")]
        [SerializeField] private float minThrowForce = 5.0f;
        [SerializeField] private float maxThrowForce = 60.0f;
        [SerializeField] private float maxChargeTime = 0.8f;

        [Header("Dash")]
        [SerializeField] private float dashForce = 15f;
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float dashCooldown = 2f;
        [SerializeField] private float dashChargeTime = 3f;

        [Header("Effects")]
        public ParticleSystem harvestingParticle;

        [Header("Animator")]
        [SerializeField] private string horizontalID = "Hor";
        [SerializeField] private string verticalID = "Vert";
        [SerializeField] private string stateID = "State";
        [SerializeField] private string isDashingID = "IsDashing";
        [SerializeField] private string isInteractingID = "IsInteracting";

        // 컴포넌트 참조
        private CharacterController controller;
        private Animator animator;
        private ItemRoot item_root = null;
        public GUIStyle guistyle;

        // 이동 관련
        private Vector2 inputAxis = Vector2.zero;
        private Vector3 velocity = Vector3.zero;
        private bool isMoving = false;
        private bool isInteracting = false;
        private bool harvestCancelled = false;
        private AudioSource audioSource;

        // 애니메이션 관련
        private Vector2 animationAxis = Vector2.zero;
        private float animationState = 0f;
        private readonly float inputFlowSpeed = 4.5f;

        // 게임 로직
        private GameObject closest_item = null;
        private GameObject closest_plant = null;
        private GameObject closest_pot = null;
        private GameObject previous_closest_item = null; // 이전 가장 가까운 아이템
        private List<GameObject> carried_items = new List<GameObject>();

        // 던지기
        private bool isCharging = false;
        private float chargeStartTime = 0.0f;
        private float currentChargeTime = 0.0f;

        // 대쉬
        private bool isDashing = false;
        private bool canDash = true;
        private float dashTimer = 0f;
        private float dashCooldownTimer = 0f;
        private float dashChargeProgress = 1f;
        private Vector3 dashDirection = Vector3.zero;

        void Start()
        {
            // 컴포넌트 초기화
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            if (harvestingParticle != null)
            {
                harvestingParticle.Stop();
            }

            // ItemRoot 찾기
            GameObject gameRoot = GameObject.Find("GameRoot");
            if (gameRoot != null)
            {
                this.item_root = gameRoot.GetComponent<ItemRoot>();
            }

            // GUI 스타일 초기화
            if (guistyle == null) guistyle = new GUIStyle();
            this.guistyle.fontSize = 20;
            this.guistyle.fontStyle = FontStyle.Bold;

            carried_items = new List<GameObject>();

            Debug.Log("IntegratedPlayerController 초기화 완료!");
        }

        void Update()
        {
            HandleInput();
            HandleMovement();
            HandleDash();
            HandleInteractions();
            HandleThrowing();
            UpdateCarriedItemsPosition();
            UpdateDashSystem();
            UpdateInteractionState();
            UpdateAnimation();
            UpdateParticleEffects();
        }

        private void HandleInput()
        {
            // 상호작용 중에는 입력을 받지 않음
            if (isInteracting) return;

            // 이동 입력 (방향키)
            inputAxis.x = 0f;
            inputAxis.y = 0f;

            if (Input.GetKey(KeyCode.RightArrow)) inputAxis.x += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) inputAxis.x -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) inputAxis.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) inputAxis.y -= 1f;

            // 이동 상태 확인
            isMoving = inputAxis.magnitude > 0.1f;
        }

        private void HandleMovement()
        {
            // 상호작용 중에는 이동하지 않음
            if (isInteracting)
            {
                // 중력만 적용
                if (controller.isGrounded)
                {
                    if (velocity.y < 0)
                    {
                        velocity.y = -2f;
                    }
                }
                else
                {
                    velocity.y += Physics.gravity.y * Time.deltaTime;
                }

                controller.Move(velocity * Time.deltaTime);
                return;
            }

            // 속도 배수 (아이템 개수에 따라)
            float speedMultiplier = 1.0f - (carried_items.Count * 0.1f);
            speedMultiplier = Mathf.Clamp(speedMultiplier, 0.5f, 1.0f);

            // 이동 처리
            Vector3 movement = Vector3.zero;

            if (isMoving && !isDashing)
            {
                // 입력을 월드 좌표계 이동으로 변환
                Vector3 forward = Vector3.forward;
                Vector3 right = Vector3.right;

                movement = inputAxis.x * right + inputAxis.y * forward;
                movement = movement.normalized;

                // 속도 적용
                float currentSpeed = walkSpeed * speedMultiplier;
                movement *= currentSpeed;

                // 회전 처리
                if (movement.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movement);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
                }
            }

            // 대쉬 처리
            if (isDashing)
            {
                movement = dashDirection * dashForce;
            }

            // 중력 처리
            if (controller.isGrounded)
            {
                if (velocity.y < 0)
                {
                    velocity.y = -2f;
                }
            }
            else
            {
                velocity.y += Physics.gravity.y * Time.deltaTime;
            }

            // 최종 이동 적용
            Vector3 finalMovement = movement + velocity;
            controller.Move(finalMovement * Time.deltaTime);
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            // 로컬 좌표계로 이동 벡터 변환
            Vector3 localMovement = transform.InverseTransformDirection(controller.velocity);
            Vector2 targetAnimAxis = new Vector2(localMovement.x, localMovement.z);

            // 속도 정규화 (더 나은 애니메이션을 위해)
            float maxSpeed = walkSpeed;
            if (maxSpeed > 0)
            {
                targetAnimAxis = targetAnimAxis / maxSpeed;
                targetAnimAxis = Vector2.ClampMagnitude(targetAnimAxis, 1f);
            }

            // 상호작용 중이 아닐 때만 이동 애니메이션 업데이트
            if (!isInteracting)
            {
                // 부드러운 애니메이션 전환
                animationAxis = Vector2.MoveTowards(animationAxis, targetAnimAxis, inputFlowSpeed * Time.deltaTime);
            }
            else
            {
                // 상호작용 중에는 이동 애니메이션을 0으로
                animationAxis = Vector2.MoveTowards(animationAxis, Vector2.zero, inputFlowSpeed * Time.deltaTime);
            }

            // 상태 애니메이션 - 대쉬 중일 때는 달리기 상태
            float targetState = 0f;
            if (isDashing)
            {
                targetState = 1f; // 대쉬 중에는 달리기 애니메이션
            }
            else if (isMoving && !isInteracting)
            {
                targetState = 0f; // 일반 걷기
            }

            animationState = Mathf.MoveTowards(animationState, targetState, inputFlowSpeed * Time.deltaTime);

            // 애니메이터 파라미터 설정
            animator.SetFloat(horizontalID, animationAxis.x);
            animator.SetFloat(verticalID, animationAxis.y);
            animator.SetFloat(stateID, animationState);
            animator.SetBool(isDashingID, isDashing);
            animator.SetBool(isInteractingID, isInteracting);
        }

        private void HandleDash()
        {
            // 상호작용 중에는 대쉬 불가
            if (isInteracting) return;

            if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing && isMoving)
            {
                StartDash();
            }

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                {
                    EndDash();
                }
            }
        }

        private void StartDash()
        {
            dashDirection = transform.forward;
            isDashing = true;
            dashTimer = dashDuration;
            canDash = false;
            dashChargeProgress = 0f;
            dashCooldownTimer = dashCooldown;

            Debug.Log("대쉬 시작!");
        }

        private void EndDash()
        {
            isDashing = false;
            dashTimer = 0f;
            Debug.Log("대쉬 종료!");
        }

        private void UpdateDashSystem()
        {
            if (!canDash && dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;

                if (dashCooldownTimer <= 0f)
                {
                    dashCooldownTimer = 0f;
                }
            }

            if (!canDash && dashCooldownTimer <= 0f)
            {
                dashChargeProgress += Time.deltaTime / dashChargeTime;
                dashChargeProgress = Mathf.Clamp01(dashChargeProgress);

                if (dashChargeProgress >= 1f)
                {
                    canDash = true;
                    Debug.Log("대쉬 준비 완료!");
                }
            }
        }

        private void UpdateInteractionState()
        {
            // 수확 상태 확인
            if (isInteracting && this.closest_plant != null)
            {
                UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
                if (universalPlant != null)
                {
                    // 수확이 완료되거나 중단되면 상호작용 종료
                    if (!universalPlant.IsBeingHarvested())
                    {
                        if (!harvestCancelled)
                        {
                            if (audioSource != null && harvestSuccessSound != null)
                            {
                                audioSource.PlayOneShot(harvestSuccessSound);
                            }
                        }
                        isInteracting = false;
                    }
                }
            }
            else if (isInteracting && this.closest_plant == null)
            {
                // 식물에서 멀어지면 상호작용 종료
                isInteracting = false;
            }
        }

        private void HandleInteractions()
        {
            // 상호작용 중일 때 스페이스를 떼면 즉시 종료
            if (isInteracting && Input.GetKeyUp(KeyCode.Space))
            {
                if (this.closest_plant != null)
                {
                    UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
                    if (universalPlant != null && universalPlant.IsBeingHarvested())
                    {
                        universalPlant.StopHarvest();
                        isInteracting = false;
                        harvestCancelled = true;
                        Debug.Log("수확 중단!");
                    }
                }
                return;
            }

            // 상호작용 중에는 새로운 상호작용 불가
            if (isInteracting) return;

            // 스페이스키 눌렀을 때 우선순위:
            // 1. 솥 근처에서 아이템 넣기 (아이템이 있을 때)
            // 2. 식물 근처에서 수확 시작
            // 3. 아이템 근처에서 줍기
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // 1. 솥에 아이템 넣기 (최우선)
                if (carried_items.Count > 0 && closest_pot != null)
                {
                    Debug.Log("[HandleInteractions] 솥에 아이템 넣기 시도");
                    DebugLogPotInteraction();
                    PutItemInPot();
                    return;
                }

                // 2. 식물 수확 시작
                if (this.closest_plant != null)
                {
                    UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
                    if (universalPlant != null && universalPlant.CanHarvest())
                    {
                        universalPlant.StartHarvest(null);
                        isInteracting = true;
                        harvestCancelled = false;
                        Debug.Log("수확 시작!");
                        return;
                    }
                }

                // 3. 아이템 줍기
                bool isCarryingNPC = HasNPCInCarriedItems();
                if (this.closest_item != null && carried_items.Count < maxCarryItems && !isCarryingNPC)
                {
                    PickupItem(this.closest_item);
                    this.closest_item = null;
                    return;
                }

                // 어떤 상호작용도 되지 않았을 때 디버그 로그
                Debug.Log("[HandleInteractions] 상호작용할 수 있는 대상이 없음");
                Debug.Log("closest_pot: " + (closest_pot != null ? closest_pot.name : "null"));
                Debug.Log("closest_plant: " + (closest_plant != null ? closest_plant.name : "null"));
                Debug.Log("closest_item: " + (closest_item != null ? closest_item.name : "null"));
                Debug.Log("carried_items.Count: " + carried_items.Count);
            }
        }

        private void HandleThrowing()
        {
            // 상호작용 중에는 던지기 불가
            if (isInteracting) return;

            if (Input.GetKeyDown(KeyCode.F) && carried_items.Count > 0)
            {
                isCharging = true;
                chargeStartTime = Time.time;
            }

            if (isCharging && Input.GetKey(KeyCode.F))
            {
                currentChargeTime = Time.time - chargeStartTime;
                currentChargeTime = Mathf.Clamp(currentChargeTime, 0.0f, maxChargeTime);
            }

            if (Input.GetKeyUp(KeyCode.F) && isCharging && carried_items.Count > 0)
            {
                ThrowTopItem();
                isCharging = false;
                currentChargeTime = 0.0f;
            }

            if (isCharging && (!Input.GetKey(KeyCode.F) || carried_items.Count == 0))
            {
                isCharging = false;
                currentChargeTime = 0.0f;
            }
        }

        private bool HasNPCInCarriedItems()
        {
            foreach (GameObject item in carried_items)
            {
                if (item != null && item.GetComponent<GameCore.NPC>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void PickupItem(GameObject item)
        {
            // 하이라이트 끄기
            SetItemHighlight(item, false);

            carried_items.Add(item);

            GameCore.UniversalItem universalItem = item.GetComponent<GameCore.UniversalItem>();
            if (universalItem != null)
            {
                universalItem.OnPickedUp();
            }
            else
            {
                GameCore.NPC npc = item.GetComponent<GameCore.NPC>();
                if (npc != null)
                {
                    npc.OnPickedUpByPlayer();
                }
            }

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            Collider col = item.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            item.transform.parent = this.transform;

            int itemIndex = carried_items.Count - 1;
            float height = baseCarryHeight + (itemIndex * itemStackHeight);
            Vector3 carryPosition = new Vector3(0, height, carryForwardOffset);
            item.transform.localPosition = carryPosition;
            item.transform.localRotation = Quaternion.identity;

            Debug.Log("아이템 획득! 현재 개수: " + carried_items.Count + "/" + maxCarryItems);
        }

        private void ThrowTopItem()
        {
            if (carried_items.Count == 0) return;

            float chargeRatio = currentChargeTime / maxChargeTime;
            float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);

            Vector3 throwDirection = this.transform.forward;
            throwDirection.y = 0;
            throwDirection.Normalize();

            GameObject itemToThrow = carried_items[carried_items.Count - 1];
            carried_items.RemoveAt(carried_items.Count - 1);
            itemToThrow.transform.parent = null;

            Collider col = itemToThrow.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }

            GameCore.UniversalItem universalItem = itemToThrow.GetComponent<GameCore.UniversalItem>();
            if (universalItem != null)
            {
                universalItem.OnThrown();
            }
            else
            {
                GameCore.NPC npc = itemToThrow.GetComponent<GameCore.NPC>();
                if (npc != null)
                {
                    npc.OnDroppedByPlayer();
                }
            }

            Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
            }

            Debug.Log("아이템 던짐! 힘: " + throwForce);
        }

        private void PutItemInPot()
        {
            if (carried_items.Count == 0 || closest_pot == null)
            {
                Debug.Log("[PutItemInPot] 들고 있는 아이템이 없거나 가까운 솥이 없음");
                return;
            }

            GameObject itemToPut = carried_items[carried_items.Count - 1];
            Debug.Log($"[PutItemInPot] 솥에 넣을 아이템: {itemToPut.name}");

            // CookingPot 컴포넌트 확인
            CookingPot cookingPot = closest_pot.GetComponent<CookingPot>();
            if (cookingPot == null)
            {
                Debug.LogError("[PutItemInPot] CookingPot 컴포넌트를 찾을 수 없음");
                return;
            }

            // UniversalItem 컴포넌트에서 CropType 가져오기
            GameCore.UniversalItem universalItem = itemToPut.GetComponent<GameCore.UniversalItem>();
            if (universalItem == null)
            {
                Debug.LogError("[PutItemInPot] UniversalItem 컴포넌트를 찾을 수 없음");
                return;
            }

            CropType cropType = universalItem.cropType;
            Debug.Log($"[PutItemInPot] 아이템의 CropType: {cropType}");

            // CookingPot에 아이템 추가 시도
            bool addedSuccessfully = cookingPot.AddItem(cropType);

            if (addedSuccessfully)
            {
                // 성공적으로 추가되면 carried_items에서 제거하고 오브젝트 파괴
                carried_items.RemoveAt(carried_items.Count - 1);

                // 아이템 상태 변경
                universalItem.OnThrown();

                // 오브젝트 파괴
                Destroy(itemToPut);

                Debug.Log($"[PutItemInPot] {cropType} 아이템을 솥에 성공적으로 넣었습니다! 남은 아이템: {carried_items.Count}");
            }
            else
            {
                Debug.Log($"[PutItemInPot] {cropType} 아이템을 솥에 넣을 수 없습니다 - 현재 레시피에 필요하지 않거나 이미 충분함");
            }
        }

        private void UpdateCarriedItemsPosition()
        {
            for (int i = 0; i < carried_items.Count; i++)
            {
                if (carried_items[i] != null)
                {
                    float height = baseCarryHeight + (i * itemStackHeight);
                    Vector3 carryPosition = new Vector3(0, height, carryForwardOffset);
                    carried_items[i].transform.localPosition = carryPosition;
                    carried_items[i].transform.localRotation = Quaternion.identity;
                }
            }
        }

        void OnTriggerStay(Collider other)
        {
            GameObject other_go = other.gameObject;

            // 솥 감지 - CookingPot 컴포넌트로 변경
            if (other_go.name.ToLower().Contains("pot") ||
                other_go.tag == "Pot" ||
                other_go.GetComponent<CookingPot>() != null ||
                other_go.name.ToLower().Contains("cauldron"))
            {
                HandlePotInteraction(other_go);
                return;
            }

            if (other_go.GetComponent<UniversalPlant>() != null)
            {
                HandlePlantInteraction(other_go);
                return;
            }

            bool isCarryingNPC = HasNPCInCarriedItems();

            bool isItemOrNPC = false;
            if (other_go.GetComponent<GameCore.UniversalItem>() != null)
                isItemOrNPC = true;
            if (other_go.GetComponent<GameCore.NPC>() != null)
                isItemOrNPC = true;

            if (isItemOrNPC && carried_items.Count < maxCarryItems && !isCarryingNPC)
            {
                if (this.closest_item == null)
                {
                    if (this.IsOtherInView(other_go))
                    {
                        this.closest_item = other_go;
                    }
                }
                else if (this.closest_item == other_go)
                {
                    if (!this.IsOtherInView(other_go))
                    {
                        this.closest_item = null;
                    }
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (this.closest_item == other.gameObject)
            {
                // 아이템 하이라이트 끄기
                SetItemHighlight(this.closest_item, false);
                this.closest_item = null;
            }

            if (this.closest_plant == other.gameObject)
            {
                this.closest_plant = null;
            }

            if (this.closest_pot == other.gameObject)
            {
                this.closest_pot = null;
            }
        }

        private bool IsOtherInView(GameObject other)
        {
            Vector3 heading = this.transform.TransformDirection(Vector3.forward);
            Vector3 to_other = other.transform.position - this.transform.position;

            heading.y = 0.0f;
            to_other.y = 0.0f;

            heading.Normalize();
            to_other.Normalize();

            float dp = Vector3.Dot(heading, to_other);

            return dp >= Mathf.Cos(45.0f * Mathf.Deg2Rad);
        }

        private void HandlePlantInteraction(GameObject plant)
        {
            if (this.closest_plant == null)
            {
                if (this.IsOtherInView(plant))
                {
                    this.closest_plant = plant;
                }
            }
            else if (this.closest_plant == plant)
            {
                if (!this.IsOtherInView(plant))
                {
                    this.closest_plant = null;
                }
            }
        }

        private void HandlePotInteraction(GameObject pot)
        {
            if (this.closest_pot == null)
            {
                if (this.IsOtherInView(pot))
                {
                    this.closest_pot = pot;
                }
            }
            else if (this.closest_pot == pot)
            {
                if (!this.IsOtherInView(pot))
                {
                    this.closest_pot = null;
                }
            }
        }

        // PlayerControl 호환성 메서드들
        public int GetCarriedItemCount()
        {
            return carried_items.Count;
        }

        public int GetCarriedItemCountOfType(Item.TYPE itemType)
        {
            int count = 0;
            foreach (GameObject item in carried_items)
            {
                if (item_root != null)
                {
                    Item.TYPE type = item_root.getItemType(item);
                    if (type == itemType)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        void OnGUI()
        {
            // 메인 카메라가 없으면 UI를 그리지 않음
            if (Camera.main == null) return;

            // 플레이어의 월드 좌표를 화면 좌표로 변환
            // 캐릭터의 머리 약간 위를 기준으로 삼음
            Vector3 characterUIPosition = transform.position + Vector3.up * 2.2f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(characterUIPosition);

            // 캐릭터가 카메라 뒤에 있거나 너무 멀리 있으면 UI를 그리지 않음
            if (screenPos.z < 0) return;

            // GUI 좌표계는 좌측 상단이 (0,0)이므로 y좌표를 변환
            screenPos.y = Screen.height - screenPos.y;

            // UI 표시 위치 오프셋 (캐릭터 오른쪽 위)
            float uiX = screenPos.x + 40f;
            float uiY = screenPos.y - 80f; // y는 위로 갈수록 작아지므로 빼줌

            // --- UI 컨텐츠 크기 및 위치 계산 ---
            float contentWidth = 240f; // 너비 증가
            float lineHeight = 22f; // 라인 높이 증가
            float lineSpacing = 3f; // 라인 간격 증가
            float sectionSpacing = 10f; // 섹션 간격 증가

            float currentY = uiY;
            guistyle.normal.textColor = Color.white; // 기본 텍스트 색상 설정

            // --- 내용물 그리기 (그림자 효과 포함) ---
            // 대쉬 게이지 표시
            DrawDashGauge(uiX, currentY, contentWidth);
            currentY += lineHeight + 14f + sectionSpacing; // 대쉬 게이지 높이에 맞게 조정 (라벨 + 바 + 간격)

            // 들고 있는 아이템 수
            DrawLabelWithShadow(new Rect(uiX, currentY, contentWidth, lineHeight), "아이템: " + carried_items.Count + "/" + maxCarryItems, guistyle);
            currentY += lineHeight;

            // 상호작용 텍스트
            string interactionText = GetInteractionText();
            if (!string.IsNullOrEmpty(interactionText))
            {
                DrawLabelWithShadow(new Rect(uiX, currentY, contentWidth, lineHeight), interactionText, guistyle);
                currentY += lineHeight;
            }

            // 수확 진행 바
            if (isInteracting && this.closest_plant != null)
            {
                UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
                if (universalPlant != null && universalPlant.IsBeingHarvested())
                {
                    currentY += lineSpacing;
                    float barHeight = 10.0f;
                    GUI.DrawTexture(new Rect(uiX, currentY, contentWidth, barHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.3f, 0.3f, 0.3f, 0.9f), 0, 0);
                    float progressWidth = contentWidth * universalPlant.GetHarvestProgress();
                    GUI.DrawTexture(new Rect(uiX, currentY, progressWidth, barHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.green, 0, 0);
                }
            }
        }
        
        // 상호작용 텍스트를 결정하는 헬퍼 메서드
        private string GetInteractionText()
        {
            if (carried_items.Count > 0)
            {
                if (isCharging)
                {
                    float chargePercent = (currentChargeTime / maxChargeTime) * 100.0f;
                    return "던지기 충전: " + chargePercent.ToString("F0") + "%";
                }
                if (this.closest_pot != null)
                {
                    return "스페이스: 솥에 넣기";
                }
                return "F키: 던지기";
            }

            if (this.closest_plant != null)
            {
                UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
                if (universalPlant != null)
                {
                    if (universalPlant.CanHarvest())
                    {
                        return "스페이스: 수확하기";
                    }
                    if (universalPlant.IsBeingHarvested())
                    {
                        float harvestPercent = universalPlant.GetHarvestProgress() * 100.0f;
                        return "수확 중: " + harvestPercent.ToString("F0") + "%";
                    }
                }
            }
            
            if (this.closest_item != null && carried_items.Count < maxCarryItems)
            {
                if (HasNPCInCarriedItems())
                {
                    return "NPC 운반중 (줍기 불가)";
                }

                GameCore.NPC npc = this.closest_item.GetComponent<GameCore.NPC>();
                return npc != null ? "스페이스: NPC 들기" : "스페이스: 아이템 줍기";
            }

            if (carried_items.Count >= maxCarryItems)
            {
                return "가방 가득 참!";
            }

            return ""; // 아무 상호작용도 없을 때
        }

        void DrawDashGauge(float x, float y, float width)
        {
            float gaugeHeight = 14.0f; // 12에서 14로
            float labelHeight = 22.0f; // 18에서 22로

            string dashStatus = "";
            Color gaugeColor = Color.gray;

            if (isDashing)
            {
                dashStatus = "대쉬!";
                gaugeColor = new Color(1f, 0.9f, 0.3f); // Yellow
            }
            else if (canDash)
            {
                dashStatus = "대쉬 준비 (Shift)";
                gaugeColor = new Color(0.4f, 0.9f, 1f); // Cyan
            }
            else if (dashCooldownTimer > 0f)
            {
                dashStatus = $"쿨다운: {dashCooldownTimer:F1}초";
                gaugeColor = new Color(1f, 0.5f, 0.5f); // Red
            }
            else
            {
                dashStatus = $"충전 중... {dashChargeProgress * 100f:F0}%";
                gaugeColor = new Color(0.6f, 0.6f, 1f); // Blue
            }

            DrawLabelWithShadow(new Rect(x, y, width, labelHeight), dashStatus, guistyle);

            float barY = y + labelHeight;
            GUI.DrawTexture(new Rect(x, barY, width, gaugeHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.3f, 0.3f, 0.3f, 0.9f), 0, 0);

            float progress = 0f;

            if (isDashing)
            {
                progress = dashTimer / dashDuration;
            }
            else if (canDash)
            {
                progress = 1f;
            }
            else if (dashCooldownTimer > 0f)
            {
                progress = 1f - (dashCooldownTimer / dashCooldown);
            }
            else
            {
                progress = dashChargeProgress;
            }
            float progressWidth = width * Mathf.Clamp01(progress);

            GUI.DrawTexture(new Rect(x, barY, progressWidth, gaugeHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, gaugeColor, 0, 0);
        }

        // 텍스트에 그림자 효과를 주기 위한 헬퍼 메서드
        void DrawLabelWithShadow(Rect rect, string text, GUIStyle style)
        {
            Color originalColor = style.normal.textColor;
            // 그림자
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
            // 원본 텍스트
            style.normal.textColor = originalColor;
            GUI.Label(rect, text, style);
        }

        // 아이템 하이라이트 제어
        private void SetItemHighlight(GameObject item, bool highlight)
        {
            if (item == null) return;

            // UniversalItem의 SetHighlight 메서드 사용 (이제 존재함)
            GameCore.UniversalItem universalItem = item.GetComponent<GameCore.UniversalItem>();
            if (universalItem != null)
            {
                universalItem.SetHighlight(highlight);
                return;
            }

            // UniversalItem이 없는 경우 (NPC 등) 기본 하이라이트 처리
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = item.GetComponentInChildren<Renderer>();
            }

            if (renderer != null)
            {
                if (highlight)
                {
                    // 밝은 노란색으로 하이라이트
                    renderer.material.color = Color.yellow;

                    // Emission 발광 효과 (지원하는 경우)
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        renderer.material.SetColor("_EmissionColor", Color.yellow * 0.3f);
                    }
                }
                else
                {
                    // 원래 색상으로 복원
                    renderer.material.color = Color.white;

                    // Emission 끄기
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                        renderer.material.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Renderer를 찾을 수 없어서 {item.name}에 하이라이트를 적용할 수 없습니다.");
            }
        }

        // 디버깅용 메서드들
        private void DebugLogPotInteraction()
        {
            Debug.Log("[DEBUG] closest_pot: " + (closest_pot != null ? closest_pot.name : "null"));
            Debug.Log("[DEBUG] carried_items.Count: " + carried_items.Count);
            if (carried_items.Count > 0)
            {
                GameObject topItem = carried_items[carried_items.Count - 1];
                GameCore.UniversalItem universalItem = topItem.GetComponent<GameCore.UniversalItem>();
                Debug.Log("[DEBUG] 들고 있는 아이템: " + topItem.name + ", cropType: " + (universalItem != null ? universalItem.cropType.ToString() : "null"));
            }
        }

        private void UpdateParticleEffects()
        {
            if (harvestingParticle == null) return;

            if (isInteracting && !harvestingParticle.isPlaying)
            {
                harvestingParticle.Play();
            }
            else if (!isInteracting && harvestingParticle.isPlaying)
            {
                harvestingParticle.Stop();
            }
        }
    }
}