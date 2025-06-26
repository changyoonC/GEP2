using UnityEngine;
using System.Collections.Generic;
using GameCore;

public class PlayerControl : MonoBehaviour
{
    public static float MOVE_AREA_RADIUS = 50.0f;
    public static float MOVE_SPEED = 7.5f;

    [Header("아이템 운반 설정")]
    public int maxCarryItems = 3;
    public float itemStackHeight = 0.8f;
    public float baseCarryHeight = 1.8f;
    public float carryForwardOffset = 0.8f;

    [Header("던지기 설정")]
    public float minThrowForce = 5.0f;
    public float maxThrowForce = 60.0f;
    public float maxChargeTime = 0.8f;
    public float throwUpwardForce = 0.3f;

    [Header("회전 설정")]
    public float rotationSpeed = 8.0f;

    [Header("대쉬 설정")]
    public float dashForce = 15f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 2f;
    public float dashChargeTime = 3f;

    private GameObject closest_item = null;
    private GameObject closest_plant = null;
    private List<GameObject> carried_items = new List<GameObject>();
    private ItemRoot item_root = null;
    public GUIStyle guistyle;

    private bool isCharging = false;
    private float chargeStartTime = 0.0f;
    private float currentChargeTime = 0.0f;

    // 대쉬 관련 변수
    private bool isDashing = false;
    private bool canDash = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float dashChargeProgress = 0f;
    private Vector3 dashDirection = Vector3.zero;
    private Rigidbody playerRigidbody;

    void Start()
    {
        this.item_root = GameObject.Find("GameRoot").GetComponent<ItemRoot>();
        this.guistyle.fontSize = 16;
        carried_items = new List<GameObject>();

        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null)
        {
            playerRigidbody = gameObject.AddComponent<Rigidbody>();
            playerRigidbody.freezeRotation = true;
        }

        dashChargeProgress = 1f;
        canDash = true;
    }

    void Update()
    {
        get_input();

        if (!isDashing)
        {
            move_control();
        }

        handle_dash();
        handle_interactions();
        handle_throwing();
        update_carried_items_position();
        update_dash_system();
    }

    private void get_input()
    {
        // 기존과 동일
    }

    private void move_control()
    {
        Vector3 move_vector = Vector3.zero;

        if (Input.GetKey(KeyCode.RightArrow)) move_vector += Vector3.right;
        if (Input.GetKey(KeyCode.LeftArrow)) move_vector += Vector3.left;
        if (Input.GetKey(KeyCode.UpArrow)) move_vector += Vector3.forward;
        if (Input.GetKey(KeyCode.DownArrow)) move_vector += Vector3.back;

        float speedMultiplier = 1.0f - (carried_items.Count * 0.1f);
        speedMultiplier = Mathf.Clamp(speedMultiplier, 0.5f, 1.0f);

        if (move_vector.magnitude > 0.1f)
        {
            move_vector = move_vector.normalized;

            Vector3 newPosition = this.transform.position + move_vector * MOVE_SPEED * speedMultiplier * Time.deltaTime;
            newPosition.y = 0.5f;

            Vector3 centerToPlayer = new Vector3(newPosition.x, 0, newPosition.z);
            if (centerToPlayer.magnitude > MOVE_AREA_RADIUS)
            {
                centerToPlayer = centerToPlayer.normalized * MOVE_AREA_RADIUS;
                newPosition.x = centerToPlayer.x;
                newPosition.z = centerToPlayer.z;
            }

            this.transform.position = newPosition;

            Vector3 lookDirection = new Vector3(move_vector.x, 0, move_vector.z);
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                Vector3 eulerAngles = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(0, eulerAngles.y, 0);

                this.transform.rotation = Quaternion.Lerp(this.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void handle_dash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
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

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = dashDirection * dashForce;
        }

        Debug.Log("대쉬 시작!");
    }

    private void EndDash()
    {
        isDashing = false;
        dashTimer = 0f;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        Debug.Log("대쉬 종료!");
    }

    private void update_dash_system()
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

    private void handle_interactions()
    {
        // 스페이스바 누르고 있는 동안 수확 진행
        if (Input.GetKey(KeyCode.Space))
        {
            if (this.closest_plant != null)
            {
                UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
                if (universalPlant != null && universalPlant.CanHarvest())
                {
                    if (!universalPlant.IsBeingHarvested())
                    {
                        universalPlant.StartHarvest(this);
                    }
                }
            }
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            if (this.closest_plant != null)
            {
                UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
                if (universalPlant != null && universalPlant.IsBeingHarvested())
                {
                    universalPlant.StopHarvest();
                }
            }
        }

        // 스페이스바 한 번 누르기로 아이템/NPC 줍기 (NPC를 들고 있지 않을 때만)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // NPC를 들고 있는지 확인
            bool isCarryingNPC = HasNPCInCarriedItems();

            if (this.closest_item != null && carried_items.Count < maxCarryItems && !isCarryingNPC)
            {
                PickupItem(this.closest_item);
                this.closest_item = null;
            }
            else if (isCarryingNPC)
            {
                Debug.Log("NPC를 들고 있어서 다른 아이템을 들 수 없습니다!");
            }
        }
    }

    // NPC를 들고 있는지 확인하는 함수
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

    private void handle_throwing()
    {
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

    private void PickupItem(GameObject item)
    {
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
                Debug.Log("NPC 던지기!");
            }
        }

        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        }

        Debug.Log("아이템 던짐! 힘: " + throwForce + ", 남은 아이템: " + carried_items.Count);
    }

    private void update_carried_items_position()
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

        if (other_go.GetComponent<UniversalPlant>() != null)
        {
            HandlePlantInteraction(other_go);
            return;
        }

        // NPC를 들고 있으면 다른 아이템을 인식하지 않음
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
                if (this.is_other_in_view(other_go))
                {
                    this.closest_item = other_go;
                }
            }
            else if (this.closest_item == other_go)
            {
                if (!this.is_other_in_view(other_go))
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
            this.closest_item = null;
        }

        if (this.closest_plant == other.gameObject)
        {
            this.closest_plant = null;
        }
    }

    void OnGUI()
    {
        float x = 20.0f;
        float y = Screen.height - 150.0f;

        // 대쉬 게이지 표시
        DrawDashGauge(x, y);
        y += 50.0f;

        GUI.Label(new Rect(x, y, 200.0f, 20.0f), "들고 있는 아이템: " + carried_items.Count + "/" + maxCarryItems, guistyle);
        y += 25.0f;

        if (carried_items.Count > 0)
        {
            if (isCharging)
            {
                float chargePercent = (currentChargeTime / maxChargeTime) * 100.0f;
                GUI.Label(new Rect(x, y, 200.0f, 20.0f), "던지기 충전: " + chargePercent.ToString("F0") + "%", guistyle);
            }
            else
            {
                GUI.Label(new Rect(x, y, 200.0f, 20.0f), "F키로 던질 수 있음", guistyle);
            }
        }
        else if (this.closest_plant != null)
        {
            UniversalPlant universalPlant = this.closest_plant.GetComponent<UniversalPlant>();
            if (universalPlant != null)
            {
                if (universalPlant.CanHarvest())
                {
                    GUI.Label(new Rect(x, y, 200.0f, 20.0f), "스페이스로 수확 시작", guistyle);
                }
                else if (universalPlant.IsBeingHarvested())
                {
                    float harvestPercent = universalPlant.GetHarvestProgress() * 100.0f;
                    GUI.Label(new Rect(x, y, 200.0f, 20.0f), "수확 중: " + harvestPercent.ToString("F0") + "%", guistyle);

                    y += 25.0f;
                    float barWidth = 200.0f;
                    float barHeight = 10.0f;

                    GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.gray, 0, 0);
                    float progressWidth = barWidth * universalPlant.GetHarvestProgress();
                    GUI.DrawTexture(new Rect(x, y, progressWidth, barHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.green, 0, 0);
                }
            }
        }
        else if (this.closest_item != null && carried_items.Count < maxCarryItems)
        {
            bool isCarryingNPC = HasNPCInCarriedItems();

            if (isCarryingNPC)
            {
                GUI.Label(new Rect(x, y, 200.0f, 20.0f), "NPC를 들고 있어서 다른 아이템을 들 수 없음", guistyle);
            }
            else
            {
                GameCore.NPC npc = this.closest_item.GetComponent<GameCore.NPC>();
                if (npc != null)
                {
                    GUI.Label(new Rect(x, y, 200.0f, 20.0f), "스페이스 눌러 NPC 들기", guistyle);
                }
                else
                {
                    GUI.Label(new Rect(x, y, 200.0f, 20.0f), "스페이스로 아이템 줍기", guistyle);
                }
            }
        }
        else if (carried_items.Count >= maxCarryItems)
        {
            GUI.Label(new Rect(x, y, 200.0f, 20.0f), "가방이 가득 참!", guistyle);
        }
    }

    void DrawDashGauge(float x, float y)
    {
        float gaugeWidth = 200.0f;
        float gaugeHeight = 20.0f;

        string dashStatus = "";
        Color gaugeColor = Color.gray;

        if (isDashing)
        {
            dashStatus = "대쉬 중!";
            gaugeColor = Color.yellow;
        }
        else if (canDash)
        {
            dashStatus = "쉬프트: 대쉬 준비됨";
            gaugeColor = Color.cyan;
        }
        else if (dashCooldownTimer > 0f)
        {
            dashStatus = $"대쉬 쿨다운: {dashCooldownTimer:F1}초";
            gaugeColor = Color.red;
        }
        else
        {
            dashStatus = $"대쉬 충전 중: {dashChargeProgress * 100f:F0}%";
            gaugeColor = Color.blue;
        }

        GUI.Label(new Rect(x, y - 25.0f, gaugeWidth, 20.0f), dashStatus, guistyle);

        GUI.DrawTexture(new Rect(x, y, gaugeWidth, gaugeHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.gray, 0, 0);

        float progressWidth = 0f;
        if (isDashing)
        {
            progressWidth = gaugeWidth * (dashTimer / dashDuration);
        }
        else if (canDash)
        {
            progressWidth = gaugeWidth;
        }
        else
        {
            progressWidth = gaugeWidth * dashChargeProgress;
        }

        GUI.DrawTexture(new Rect(x, y, progressWidth, gaugeHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, gaugeColor, 0, 0);
    }

    private bool is_other_in_view(GameObject other)
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
            if (this.is_other_in_view(plant))
            {
                this.closest_plant = plant;
            }
        }
        else if (this.closest_plant == plant)
        {
            if (!this.is_other_in_view(plant))
            {
                this.closest_plant = null;
            }
        }
    }

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
}