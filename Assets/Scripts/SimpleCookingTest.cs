using UnityEngine;

public class SimpleCookingTest : MonoBehaviour
{
    public float detectionRange = 10f;
    
    void Update()
    {
        // 스페이스바 입력 확인
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("스페이스바 눌림!");
            CheckForPlayer();
        }
    }
    
    void CheckForPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            Debug.Log($"플레이어와 거리: {distance:F2}m");
            
            if (distance <= detectionRange)
            {
                Debug.Log("상호작용 성공!");
            }
            else
            {
                Debug.Log("플레이어가 너무 멀리 있습니다!");
            }
        }
        else
        {
            Debug.Log("플레이어를 찾을 수 없습니다!");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}