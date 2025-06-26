using UnityEngine;

public class center : MonoBehaviour
{
    [Header("자동으로 오브젝트를 찾아서 태그를 설정합니다")]
    [SerializeField] private bool enableLogging = true;
    
    private GameObject center1;
    private GameObject center2;
    private GameObject center3;
    private GameObject ground;
    
    void Start()
    {
        SetupTagsAutomatically();
    }
    
    void Update()
    {
        // 매 프레임마다 태그가 올바른지 확인하고 수정
        CheckAndFixTags();
    }
    
    private void SetupTagsAutomatically()
    {
        // 오브젝트들을 자동으로 찾기
        center1 = GameObject.Find("center1");
        center2 = GameObject.Find("center2");
        center3 = GameObject.Find("center3");
        ground = GameObject.Find("Ground");
        
        // Ground가 없으면 다른 이름으로도 찾아보기
        if (ground == null)
        {
            ground = GameObject.Find("ground");
        }
        
        if (enableLogging)
        {
            Debug.Log("[TagManager] 오브젝트 검색 결과:");
            Debug.Log($"center1: {(center1 != null ? "발견됨" : "없음")}");
            Debug.Log($"center2: {(center2 != null ? "발견됨" : "없음")}");
            Debug.Log($"center3: {(center3 != null ? "발견됨" : "없음")}");
            Debug.Log($"ground: {(ground != null ? "발견됨" : "없음")}");
        }
        
        // 태그 설정
        ApplyTags();
    }
    
    private void CheckAndFixTags()
    {
        // center 오브젝트들의 태그가 "Center"가 아니면 수정
        if (center1 != null && center1.tag != "Center")
        {
            center1.tag = "Center";
            if (enableLogging) Debug.Log("[TagManager] center1 태그를 Center로 수정");
        }
        
        if (center2 != null && center2.tag != "Center")
        {
            center2.tag = "Center";
            if (enableLogging) Debug.Log("[TagManager] center2 태그를 Center로 수정");
        }
        
        if (center3 != null && center3.tag != "Center")
        {
            center3.tag = "Center";
            if (enableLogging) Debug.Log("[TagManager] center3 태그를 Center로 수정");
        }
        
        // ground 오브젝트의 태그가 "Ground"가 아니면 수정
        if (ground != null && ground.tag != "Ground")
        {
            ground.tag = "Ground";
            if (enableLogging) Debug.Log("[TagManager] ground 태그를 Ground로 수정");
        }
    }
    
    private void ApplyTags()
    {
        // Center 태그 생성 (존재하지 않을 경우)
        CreateTagIfNotExists("Center");
        CreateTagIfNotExists("Ground");
        
        // 태그 적용
        if (center1 != null)
        {
            center1.tag = "Center";
            if (enableLogging) Debug.Log("[TagManager] center1 태그를 Center로 설정");
        }
        
        if (center2 != null)
        {
            center2.tag = "Center";
            if (enableLogging) Debug.Log("[TagManager] center2 태그를 Center로 설정");
        }
        
        if (center3 != null)
        {
            center3.tag = "Center";
            if (enableLogging) Debug.Log("[TagManager] center3 태그를 Center로 설정");
        }
        
        if (ground != null)
        {
            ground.tag = "Ground";
            if (enableLogging) Debug.Log("[TagManager] ground 태그를 Ground로 설정");
        }
    }
    
    private void CreateTagIfNotExists(string tagName)
    {
        // 태그가 존재하지 않으면 생성하려고 시도
        // (에디터에서만 작동하므로 런타임에서는 기존 태그를 사용)
        try
        {
            GameObject tempObj = new GameObject();
            tempObj.tag = tagName;
            DestroyImmediate(tempObj);
        }
        catch
        {
            if (enableLogging) Debug.LogWarning($"[TagManager] '{tagName}' 태그가 존재하지 않습니다. Tag Manager에서 추가해주세요.");
        }
    }
    
    // 수동으로 태그를 다시 설정하는 메서드 (디버그용)
    [ContextMenu("태그 다시 설정")]
    public void ResetTags()
    {
        SetupTagsAutomatically();
    }
}