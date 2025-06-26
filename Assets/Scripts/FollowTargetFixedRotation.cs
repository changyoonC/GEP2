using UnityEngine;

public class FollowTargetFixedRotation : MonoBehaviour
{
    public Transform target;         // 따라갈 오브젝트
    public Vector3 offset = new Vector3(0, 5, -10); // 오브젝트와의 거리(위치 오프셋)
    private Quaternion fixedRotation; // 고정할 회전값

    void Start()
    {
        // 현재 카메라의 회전을 고정값으로 저장
        fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // 위치만 따라가고 회전은 고정
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = fixedRotation;
        }
    }
}
