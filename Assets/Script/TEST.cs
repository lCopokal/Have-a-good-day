using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    public ItemData testItem;
    private InventorySystem inventory;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
    }

    void Update()
    {
        // Нажмите K чтобы добавить тестовый предмет
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (testItem != null && inventory != null)
            {
                inventory.AddItem(testItem, 1);
                Debug.Log("Добавлен тестовый предмет");
            }
        }
    }
}