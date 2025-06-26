using UnityEngine;
using System.Collections;

namespace GameCore
{
    public enum CropType
    {
        Broccoli,
        Sunflower,
        Mushroom,
        Carrot,
        Cauliflower,
        Potato,
        Corn
    }

    public enum PlantState
    {
        Empty,      // 빈 상태 (수확 후)
        Growing,    // 자라는 중
        Full        // 수확 가능
    }

    [System.Serializable]
    public class CropData
    {
        public CropType cropType;
        public GameObject itemPrefab;          // 수확시 생성될 아이템 프리팹
        public int minDropCount = 3;           // 최소 드롭 개수
        public int maxDropCount = 5;           // 최대 드롭 개수
        public float regrowTime = 30f;         // 재성장 시간 (초)
        public string cropName = "";           // 작물 이름
        public float harvestTime = 3f;         // 수확에 걸리는 시간 (초)
        public float dropRadius = 2f;          // 아이템 드롭 반경
    }

    public class UniversalPlant : MonoBehaviour
    {
        [Header("작물 설정")]
        public CropData cropData;

        [Header("시각적 요소")]
        public GameObject plantVisual_Empty;   // 빈 상태
        public GameObject plantVisual_Few;     // 자라는 중
        public GameObject plantVisual_Full;    // 수확 가능

        [Header("현재 상태")]
        public PlantState currentState = PlantState.Full;

        private bool canHarvest = true;
        private bool isRegrowing = false;
        private bool isBeingHarvested = false;
        private float harvestProgress = 0f;
        private PlayerControl currentHarvester = null;

        // 코루틴 참조 저장
        private Coroutine harvestCoroutine = null;
        private Coroutine regrowCoroutine = null;

        void Start()
        {
            UpdateVisual();
        }

        public bool CanHarvest()
        {
            return canHarvest && !isRegrowing && !isBeingHarvested;
        }

        public bool IsBeingHarvested()
        {
            return isBeingHarvested;
        }

        public float GetHarvestProgress()
        {
            return harvestProgress / cropData.harvestTime;
        }

        // === 상태 변경 Public 메서드들 ===

        /// <summary>
        /// 식물 상태를 직접 변경합니다
        /// </summary>
        public void SetPlantState(PlantState newState)
        {
            currentState = newState;

            switch (newState)
            {
                case PlantState.Empty:
                    canHarvest = false;
                    isRegrowing = false;
                    break;
                case PlantState.Growing:
                    canHarvest = false;
                    isRegrowing = true;
                    break;
                case PlantState.Full:
                    canHarvest = true;
                    isRegrowing = false;
                    break;
            }

            UpdateVisual();
            Debug.Log($"{cropData.cropName} 상태가 {newState}로 변경되었습니다.");
        }

        /// <summary>
        /// 현재 식물 상태를 반환합니다
        /// </summary>
        public PlantState GetCurrentState()
        {
            return currentState;
        }

        /// <summary>
        /// 식물을 즉시 Empty 상태로 변경
        /// </summary>
        public void SetToEmpty()
        {
            SetPlantState(PlantState.Empty);
            StopAllGrowthProcesses();
        }

        /// <summary>
        /// 식물을 즉시 Growing 상태로 변경
        /// </summary>
        public void SetToGrowing()
        {
            SetPlantState(PlantState.Growing);
        }

        /// <summary>
        /// 식물을 즉시 Full 상태로 변경 (수확 가능)
        /// </summary>
        public void SetToFull()
        {
            SetPlantState(PlantState.Full);
            StopAllGrowthProcesses();
        }

        /// <summary>
        /// 강제로 재성장 시작 (Growing → Full)
        /// </summary>
        public void ForceStartRegrow()
        {
            SetPlantState(PlantState.Growing);

            // 기존 재성장 코루틴이 있다면 중지
            if (regrowCoroutine != null)
            {
                StopCoroutine(regrowCoroutine);
            }

            regrowCoroutine = StartCoroutine(RegrowCycle());
        }

        /// <summary>
        /// 모든 성장 프로세스 중지
        /// </summary>
        public void StopAllGrowthProcesses()
        {
            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
                harvestCoroutine = null;
            }

            if (regrowCoroutine != null)
            {
                StopCoroutine(regrowCoroutine);
                regrowCoroutine = null;
            }

            isBeingHarvested = false;
            harvestProgress = 0f;
            currentHarvester = null;
        }

        /// <summary>
        /// 재성장 시간을 런타임에 변경
        /// </summary>
        public void SetRegrowTime(float newRegrowTime)
        {
            cropData.regrowTime = newRegrowTime;
            Debug.Log($"{cropData.cropName} 재성장 시간이 {newRegrowTime}초로 변경되었습니다.");
        }

        /// <summary>
        /// 수확 시간을 런타임에 변경
        /// </summary>
        public void SetHarvestTime(float newHarvestTime)
        {
            cropData.harvestTime = newHarvestTime;
            Debug.Log($"{cropData.cropName} 수확 시간이 {newHarvestTime}초로 변경되었습니다.");
        }

        // 플레이어용 수확 시작 (3초 대기)
        public void StartHarvest(PlayerControl harvester)
        {
            if (!CanHarvest()) return;

            isBeingHarvested = true;
            harvestProgress = 0f;
            currentHarvester = harvester;

            Debug.Log($"{cropData.cropName} 수확 시작!");

            // 기존 수확 코루틴이 있다면 중지
            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
            }

            harvestCoroutine = StartCoroutine(HarvestProcess());
        }

        // NPC용 즉시 수확
        public void TryHarvest()
        {
            if (!CanHarvest()) return;

            Debug.Log($"{cropData.cropName} NPC가 즉시 수확!");
            CompleteHarvest();
        }

        // 수확 중단
        public void StopHarvest()
        {
            if (!isBeingHarvested) return;

            isBeingHarvested = false;
            harvestProgress = 0f;
            currentHarvester = null;

            Debug.Log($"{cropData.cropName} 수확 중단!");

            // 수확 코루틴 중지
            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
                harvestCoroutine = null;
            }
        }

        private IEnumerator HarvestProcess()
        {
            while (harvestProgress < cropData.harvestTime && isBeingHarvested)
            {
                harvestProgress += Time.deltaTime;
                yield return null;
            }

            if (isBeingHarvested && harvestProgress >= cropData.harvestTime)
            {
                CompleteHarvest();
            }

            harvestCoroutine = null; // 코루틴 완료 후 참조 제거
        }

        private void CompleteHarvest()
        {
            // 상태를 Empty로 변경
            SetPlantState(PlantState.Empty);

            isBeingHarvested = false;
            harvestProgress = 0f;
            currentHarvester = null;

            // 아이템 드롭 - 더 넓은 범위에 생성
            int dropCount = Random.Range(cropData.minDropCount, cropData.maxDropCount + 1);

            Debug.Log($"{cropData.cropName} 수확 완료! {dropCount}개 생성 시도");

            for (int i = 0; i < dropCount; i++)
            {
                // 원형으로 퍼뜨려서 드롭
                float angle = (360f / dropCount) * i + Random.Range(-30f, 30f);
                float distance = Random.Range(1f, cropData.dropRadius);

                Vector3 dropOffset = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    1f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );

                Vector3 dropPosition = transform.position + dropOffset;

                if (cropData.itemPrefab != null)
                {
                    GameObject droppedItem = Instantiate(cropData.itemPrefab, dropPosition, Quaternion.identity);
                    Debug.Log($"아이템 {i + 1} 생성됨: {droppedItem.name} at {dropPosition}");

                    // 약간의 물리적 힘을 가해서 자연스럽게 떨어뜨리기
                    Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 randomForce = new Vector3(
                            Random.Range(-2f, 2f),
                            Random.Range(1f, 3f),
                            Random.Range(-2f, 2f)
                        );
                        rb.AddForce(randomForce, ForceMode.Impulse);
                    }
                }
                else
                {
                    Debug.LogWarning("cropData.itemPrefab이 null입니다!");
                }
            }

            // 자동으로 재성장 시작 (Growing 상태로)
            ForceStartRegrow();
        }

        private IEnumerator RegrowCycle()
        {
            // Growing 상태로 설정
            SetPlantState(PlantState.Growing);

            yield return new WaitForSeconds(cropData.regrowTime);

            // Full 상태로 설정
            SetPlantState(PlantState.Full);

            Debug.Log($"{cropData.cropName} 재성장 완료!");

            regrowCoroutine = null; // 코루틴 완료 후 참조 제거
        }

        private void UpdateVisual()
        {
            switch (currentState)
            {
                case PlantState.Empty:
                    plantVisual_Empty?.SetActive(true);
                    plantVisual_Few?.SetActive(false);
                    plantVisual_Full?.SetActive(false);
                    break;

                case PlantState.Growing:
                    plantVisual_Empty?.SetActive(false);
                    plantVisual_Few?.SetActive(true);
                    plantVisual_Full?.SetActive(false);
                    break;

                case PlantState.Full:
                    plantVisual_Empty?.SetActive(false);
                    plantVisual_Few?.SetActive(false);
                    plantVisual_Full?.SetActive(true);
                    break;
            }
        }

        void OnDestroy()
        {
            // 오브젝트 파괴시 모든 코루틴 정리
            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
            }
            if (regrowCoroutine != null)
            {
                StopCoroutine(regrowCoroutine);
            }
        }
    }
}