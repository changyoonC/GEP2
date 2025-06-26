using UnityEngine;

public class FollowTargetFixedRotation : MonoBehaviour
{
    public Transform target;         // ���� ������Ʈ
    public Vector3 offset = new Vector3(0, 5, -10); // ������Ʈ���� �Ÿ�(��ġ ������)
    private Quaternion fixedRotation; // ������ ȸ����

    void Start()
    {
        // ���� ī�޶��� ȸ���� ���������� ����
        fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // ��ġ�� ���󰡰� ȸ���� ����
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = fixedRotation;
        }
    }
}
