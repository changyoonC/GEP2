using UnityEngine;
using System.Collections.Generic;

namespace GameCore
{
    public class CropZone : MonoBehaviour
    {
        [Header("구역 설정")]
        public CropType zoneType;
        public string zoneName;
        public Color zoneColor = Color.green;
        
        [Header("작물 생성 설정")]
        public GameObject plantPrefab;
        public int maxPlantsInZone = 5;
        [HideInInspector] public float spawnRadius = 5f; // NPC가 접근할 수 있도록 public으로 변경
        public LayerMask groundLayer = 1;
        
        [Header("주민 관련")]
        public bool hasWorker = false;
        public GameObject workerPrefab;
        
        private List<UniversalPlant> plantsInZone = new List<UniversalPlant>();
        private GameObject currentWorker;
        
        void Start()
        {
            // 필요한 태그들 확인/생성
            EnsureTagExists(zoneType + "Plant");
            
            // plantPrefab이 있을 때만 식물 생성
            if (plantPrefab != null)
            {
                SpawnInitialPlants();
            }
            else
            {
                Debug.Log($"{zoneName}: plantPrefab이 설정되지 않아 기존 식물들만 관리합니다.");
            }
            
            // 구역 이름이 비어있으면 자동 설정
            if (string.IsNullOrEmpty(zoneName))
            {
                zoneName = zoneType.ToString() + " 구역";
            }
            
            Debug.Log($"{zoneName} 초기화 완료");
        }
        
        void EnsureTagExists(string tagName)
        {
            // Unity Editor에서만 태그를 추가할 수 있음
#if UNITY_EDITOR
            // 태그가 존재하는지 확인
            UnityEngine.Object[] tags = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tags != null && tags.Length > 0)
            {
                UnityEditor.SerializedObject tagManager = new UnityEditor.SerializedObject(tags[0]);
                UnityEditor.SerializedProperty tagsProp = tagManager.FindProperty("tags");
                
                // 이미 존재하는 태그인지 확인
                bool tagExists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    UnityEditor.SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
                    if (tag.stringValue.Equals(tagName))
                    {
                        tagExists = true;
                        break;
                    }
                }
                
                // 태그가 없으면 추가
                if (!tagExists)
                {
                    tagsProp.InsertArrayElementAtIndex(0);
                    UnityEditor.SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(0);
                    newTag.stringValue = tagName;
                    tagManager.ApplyModifiedProperties();
                    
                    Debug.Log($"새 태그 생성됨: {tagName}");
                }
            }
#endif
        }
        
        void SpawnInitialPlants()
        {
            // plantPrefab이 할당되지 않았으면 건너뛰기
            if (plantPrefab == null)
            {
                Debug.LogWarning($"{zoneName}: plantPrefab이 할당되지 않았습니다. 식물 생성을 건너뜁니다.");
                return;
            }
            
            for (int i = 0; i < maxPlantsInZone; i++)
            {
                SpawnPlant();
            }
        }
        
        void SpawnPlant()
        {
            // plantPrefab 안전 체크
            if (plantPrefab == null)
            {
                Debug.LogWarning($"{zoneName}: plantPrefab이 null입니다.");
                return;
            }
            
            Vector3 randomPos = GetRandomPositionInZone();
            if (randomPos != Vector3.zero)
            {
                GameObject newPlant = Instantiate(plantPrefab, randomPos, Quaternion.identity);
                UniversalPlant plantScript = newPlant.GetComponent<UniversalPlant>();
                
                if (plantScript != null)
                {
                    plantsInZone.Add(plantScript);
                    
                    // 태그 설정 (안전하게)
                    string desiredTag = zoneType + "Plant";
                    
                    // 태그가 존재하는지 확인하고 설정
                    try
                    {
                        newPlant.tag = desiredTag;
                        Debug.Log($"태그 설정 완료: {desiredTag}");
                    }
                    catch
                    {
                        // 태그가 없으면 기본 태그 사용
                        newPlant.tag = "Untagged";
                        Debug.LogWarning($"태그 '{desiredTag}'가 존재하지 않습니다. 'Untagged'로 설정됩니다.");
                    }
                    
                    Debug.Log($"{zoneName}에 식물 생성 완료: {newPlant.name}");
                }
                else
                {
                    Debug.LogWarning($"{zoneName}: 생성된 프리팹에 UniversalPlant 컴포넌트가 없습니다.");
                }
            }
        }
        
        Vector3 GetRandomPositionInZone()
        {
            // 구역 중심에서 spawnRadius 내의 랜덤 위치 생성
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            
            // 구역의 X, Z 좌표에 랜덤 오프셋 추가, Y는 무조건 0
            Vector3 spawnPosition = new Vector3(
                transform.position.x + randomCircle.x,
                0f,  // Y축 무조건 0
                transform.position.z + randomCircle.y
            );
            
            Debug.Log($"{zoneName}: 작물 생성 위치 - {spawnPosition}");
            return spawnPosition;
        }
        
        // NPC가 구역에 들어왔을 때 자동 할당
        void OnTriggerEnter(Collider other)
        {
            NPC npc = other.GetComponent<NPC>();
            if (npc != null && !hasWorker)
            {
                // NPC가 플레이어에 의해 조작되고 있지 않을 때만 할당
                if (other.transform.parent == null)
                {
                    AssignWorker(other.gameObject);
                }
            }
        }
        
        // NPC가 구역을 벗어났을 때
        void OnTriggerExit(Collider other)
        {
            if (currentWorker == other.gameObject)
            {
                RemoveWorker();
            }
        }
        
        public void AssignWorker(GameObject worker)
        {
            if (currentWorker == null)
            {
                currentWorker = worker;
                hasWorker = true;
                
                NPC npcScript = worker.GetComponent<NPC>();
                if (npcScript != null)
                {
                    npcScript.AssignToZone(this);
                }
                
                Debug.Log($"{zoneName}에 주민 배치됨: {worker.name}");
            }
        }
        
        public void RemoveWorker()
        {
            if (currentWorker != null)
            {
                NPC npcScript = currentWorker.GetComponent<NPC>();
                if (npcScript != null)
                {
                    npcScript.RemoveFromZone();
                }
                
                Debug.Log($"{zoneName}에서 주민 제거됨");
            }
            
            currentWorker = null;
            hasWorker = false;
        }
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = zoneColor;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.2f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
            
            // 구역 이름 표시
            if (!string.IsNullOrEmpty(zoneName))
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, zoneName);
                #endif
            }
        }
    }
}