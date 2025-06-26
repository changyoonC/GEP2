using UnityEngine;
using System.Collections.Generic;

public class Item
{
    public enum TYPE
    {  // ������ ����.
        NONE = -1, IRON = 0, APPLE, PLANT, // ����, ö����, ���, �Ĺ�.
        NUM,
    };
    // �������� �� �����ΰ� ��Ÿ����(=3).
};
public class ItemRoot : MonoBehaviour
{
    // �������� ������ Item.TYPE������ ��ȯ�ϴ� �޼ҵ�.
    public Item.TYPE getItemType(GameObject item_go)
    {
        Item.TYPE type = Item.TYPE.NONE;
        if (item_go != null)
        {

            // �μ��� ���� GameObject�� ������� ������.
            switch (item_go.tag)
            {  // �±׷� �б�.
                case "Iron": type = Item.TYPE.IRON; break;
                case "Apple": type = Item.TYPE.APPLE; break;
                case "Plant": type = Item.TYPE.PLANT; break;
            }
        }
        return (type);
    }
}

