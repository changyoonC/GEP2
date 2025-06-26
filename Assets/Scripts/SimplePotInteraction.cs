using UnityEngine;
using System.Reflection;

public class SimplePotInteraction : MonoBehaviour
{
    public float range = 5f;
    
    private GameObject player;
    private PlayerControl playerControl;
    private GameCore.CookingPot cookingPot;
    private bool inRange = false;
    
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerControl = player.GetComponent<PlayerControl>();
        cookingPot = GetComponent<GameCore.CookingPot>();
    }
    
    void Update()
    {
        CheckRange();
        if (inRange && Input.GetKeyDown(KeyCode.Space))
            AddIngredient();
    }
    
    void CheckRange()
    {
        if (player == null) return;
        
        float dist = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = inRange;
        inRange = dist <= range;
        
        if (inRange && !wasInRange)
            Debug.Log("솥 근처입니다. 스페이스바로 재료 넣기!");
    }
    
    void AddIngredient()
    {
        if (playerControl == null) return;
        
        int count = playerControl.GetCarriedItemCount();
        if (count == 0)
        {
            Debug.Log("들고 있는 재료가 없습니다!");
            return;
        }
        
        // Reflection으로 carried_items 가져오기
        var field = typeof(PlayerControl).GetField("carried_items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            var items = (System.Collections.Generic.List<GameObject>)field.GetValue(playerControl);
            if (items != null && items.Count > 0)
            {
                GameObject item = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                
                item.transform.parent = null;
                
                ProcessDirectly(item);
                Debug.Log($"{item.name}을 솥에 넣었습니다!");
            }
        }
    }
    
    void ProcessDirectly(GameObject item)
    {
        // ProcessItem 메서드 직접 호출
        var method = typeof(GameCore.CookingPot).GetMethod("ProcessItem", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(cookingPot, new object[] { item });
        }
        else
        {
            // 직접 처리
            Destroy(item);
            Debug.Log("재료를 솥에 넣었습니다!");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}