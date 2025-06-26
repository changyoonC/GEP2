using UnityEngine;

namespace GameCore
{
    public class UniversalItem : MonoBehaviour
    {
        [Header("아이템 설정")]
        public Item.TYPE itemType = Item.TYPE.PLANT;
        public CropType cropType; // Inspector에서 지정
        public float lifeTime = 30.0f;
        public float bobSpeed = 2.0f;
        public float bobHeight = 0.2f;

        [Header("물리 설정")]
        public float bounceForce = 0.6f;           // 바운스 강도
        public float slowdownDuration = 1.0f;      // 느려지는 시간 (1초)
        public float frictionStrength = 0.95f;     // 마찰력 강도
        public float minYPosition = 0.0f;          // 최소 Y 위치 제한
        public float maxAngularVelocity = 5.0f;    // 최대 각속도 제한

        private Vector3 startPosition;
        private float timer = 0.0f;
        private bool isPickedUp = false;
        private bool hasLanded = false;
        private bool isGrounded = false;
        private float landedTime = 0.0f;
        private bool isSlowingDown = false;

       

        void Update()
        {
            if (isPickedUp) return;

            timer += Time.deltaTime;

            // Y 위치 제한 - 최소값 이하로 내려가지 않도록
            if (transform.position.y <= minYPosition)
            {
                Vector3 clampedPosition = transform.position;
                clampedPosition.y = minYPosition;
                transform.position = clampedPosition;

                // Y=0에 도달하면 완전히 멈춤
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !isGrounded)
                {
                    // 모든 움직임 완전 정지
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                    rb.useGravity = false;

                    // 상태 변경
                    isGrounded = true;
                    hasLanded = true;
                    isSlowingDown = false;
                    startPosition = transform.position;
                    timer = 0.0f;

                    Debug.Log("아이템이 Y=0에 도달하여 완전 정지: " + gameObject.name);
                }
            }

            // 회전 속도 제한
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null && !rigidbody.isKinematic)
            {
                // 각속도의 크기가 최대값을 초과하면 제한
                if (rigidbody.angularVelocity.magnitude > maxAngularVelocity)
                {
                    rigidbody.angularVelocity = rigidbody.angularVelocity.normalized * maxAngularVelocity;
                }
            }

            // 던져진 후 통통 튀면서 점점 느려지는 처리
            if (isSlowingDown && !isGrounded)
            {
                SlowDownGradually();
            }

            // 완전히 정착한 후에만 둥둥 떠다니는 애니메이션
            if (isGrounded && hasLanded)
            {
                float bobOffset = Mathf.Sin(timer * bobSpeed) * bobHeight;
                Vector3 targetPosition = startPosition + Vector3.up * bobOffset;

                // 둥둥 애니메이션도 최소 Y 위치 제한 적용
                if (targetPosition.y < minYPosition)
                {
                    targetPosition.y = minYPosition;
                }

                transform.position = targetPosition;
            }

            // 생존 시간 확인
            if (timer >= lifeTime)
            {
                DestroyItem();
            }
        }
        [Header("하이라이트 설정")]
        public Color highlightColor = Color.yellow;
        public float highlightIntensity = 0.3f;

        // 하이라이트 관련 private 필드들
        private bool isHighlighted = false;
        private Renderer itemRenderer;
        private Material originalMaterial;
        private Material highlightMaterial;
        private Color originalColor;
        private bool hasEmission;
        private Color originalEmissionColor;

        void Start()
        {
            startPosition = transform.position;
            gameObject.layer = LayerMask.NameToLayer("Item");
            gameObject.tag = "Item";

            // 하이라이트 초기화
            InitializeHighlight();
        }

        // ... 기존 Update, 기타 메서드들 ...

        #region 하이라이트 관련 메서드들

        private void InitializeHighlight()
        {
            // Renderer 컴포넌트 찾기
            itemRenderer = GetComponent<Renderer>();
            if (itemRenderer == null)
            {
                itemRenderer = GetComponentInChildren<Renderer>();
            }

            if (itemRenderer != null && itemRenderer.material != null)
            {
                // 원본 머티리얼 정보 저장
                originalMaterial = itemRenderer.material;
                originalColor = originalMaterial.color;

                // Emission 속성 확인
                hasEmission = originalMaterial.HasProperty("_EmissionColor");
                if (hasEmission)
                {
                    originalEmissionColor = originalMaterial.GetColor("_EmissionColor");
                }

                // 하이라이트용 머티리얼 생성 (원본의 복사본)
                highlightMaterial = new Material(originalMaterial);
            }
        }

        /// <summary>
        /// 아이템 하이라이트를 켜거나 끔
        /// </summary>
        /// <param name="highlight">하이라이트 활성화 여부</param>
        public void SetHighlight(bool highlight)
        {
            if (isHighlighted == highlight || itemRenderer == null) return;

            isHighlighted = highlight;

            if (highlight)
            {
                ApplyHighlight();
            }
            else
            {
                RemoveHighlight();
            }
        }

        private void ApplyHighlight()
        {
            if (highlightMaterial == null) return;

            // 하이라이트 색상 적용
            highlightMaterial.color = highlightColor;

            // Emission 발광 효과 추가
            if (hasEmission)
            {
                highlightMaterial.EnableKeyword("_EMISSION");
                highlightMaterial.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            }

            // 하이라이트 머티리얼 적용
            itemRenderer.material = highlightMaterial;
        }

        private void RemoveHighlight()
        {
            if (originalMaterial == null) return;

            // 원본 머티리얼로 복원
            itemRenderer.material = originalMaterial;
        }

        #endregion

        // ... 기존 메서드들 (OnPickedUp, OnThrown 등) ...

        public void OnPickedUp()
        {
            isPickedUp = true;
            SetHighlight(false); // 줍힐 때 하이라이트 끄기
            Debug.Log("아이템 줍혔음!" + gameObject.name);
        }

        void OnDestroy()
        {
            // 메모리 누수 방지를 위해 생성한 머티리얼 정리
            if (highlightMaterial != null)
            {
                DestroyImmediate(highlightMaterial);
            }
        }
        private void SlowDownGradually()
        {
            float timeSinceLanded = Time.time - landedTime;

            // 1초 동안 점점 느려지기
            if (timeSinceLanded < slowdownDuration)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 수평 속도에 마찰력 적용
                    Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                    rb.linearVelocity = new Vector3(
                        horizontalVelocity.x * frictionStrength,
                        rb.linearVelocity.y,
                        horizontalVelocity.z * frictionStrength
                    );

                    // 각속도도 감소 (더 강한 감소)
                    rb.angularVelocity *= frictionStrength * 0.8f; // 회전은 더 빨리 멈추도록
                }
            }
            else
            {
                // 1초 후 완전 정지
                StopCompletelyAndStartBob();
            }
        }

        private void StopCompletelyAndStartBob()
        {
            isGrounded = true;
            isSlowingDown = false;

            // startPosition도 최소 Y 위치 제한 적용
            startPosition = transform.position;
            if (startPosition.y < minYPosition)
            {
                startPosition.y = minYPosition;
                transform.position = startPosition;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            timer = 0.0f;

            Debug.Log("아이템 완전 정지, 둥둥 애니메이션 시작: " + gameObject.name);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!hasLanded && collision.gameObject.CompareTag("Ground"))
            {
                OnLandOnGround(collision);
            }
        }

        private void OnLandOnGround(Collision collision)
        {
            hasLanded = true;
            landedTime = Time.time;
            isSlowingDown = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 바운스 효과 (통통 튀기) - 회전 제한
                Vector3 velocity = rb.linearVelocity;
                velocity.y = Mathf.Abs(velocity.y) * bounceForce;
                rb.linearVelocity = velocity;

                // 착지 시 회전 속도 감소
                rb.angularVelocity *= 0.3f;
            }

            Debug.Log("아이템이 바닥에 착지, 통통 튀기 시작: " + gameObject.name);
        }


        public void OnThrown()
        {
            hasLanded = false;
            isGrounded = false;
            isPickedUp = false;
            isSlowingDown = false;
            landedTime = 0.0f;

            Debug.Log("아이템이 던져짐: " + gameObject.name);
        }

        private void DestroyItem()
        {
            Debug.Log("아이템이 사라졌습니다." + gameObject.name);
            Destroy(gameObject);
        }

        public Item.TYPE GetItemType()
        {
            return itemType;
        }
    }
}