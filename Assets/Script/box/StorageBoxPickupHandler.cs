using UnityEngine;

public class StorageBoxPickupHandler : MonoBehaviour
{
    private ItemPickupSystem pickupSystem;
    private BoxPlacementSystem placementSystem;
    private InventorySystem inventorySystem;

    void Start()
    {
        pickupSystem = GetComponent<ItemPickupSystem>();
        placementSystem = GetComponent<BoxPlacementSystem>();
        inventorySystem = GetComponent<InventorySystem>();

        if (placementSystem == null)
        {
            placementSystem = gameObject.AddComponent<BoxPlacementSystem>();
        }
    }

    void Update()
    {
        // Проверяем, держим ли мы ящик в руках
        if (pickupSystem != null && pickupSystem.GetHeldItem() != null)
        {
            // ИСПРАВЛЕНО: Получаем PickupItem, затем ItemData
            PickupItem heldPickup = pickupSystem.GetHeldItem();
            ItemData heldItem = heldPickup != null ? heldPickup.GetItemData() : null;

            // Если это ящик хранения и нажата ЛКМ
            if (heldItem is StorageBoxItem && Input.GetMouseButtonDown(0))
            {
                StorageBoxItem boxItem = heldItem as StorageBoxItem;

                // Входим в режим размещения
                EnterBoxPlacementMode(boxItem);
            }
        }
    }

    private void EnterBoxPlacementMode(StorageBoxItem boxItem)
    {
        if (placementSystem == null) return;

        // Убираем предмет из рук
        if (pickupSystem != null)
        {
            pickupSystem.DropHeldItem();
        }

        // Активируем режим размещения
        placementSystem.EnterPlacementMode(boxItem);

        Debug.Log($"Режим размещения ящика: {boxItem.itemName}");
    }

    // Альтернативный метод - использование прямо из инвентаря
    public void UseStorageBoxFromInventory(StorageBoxItem boxItem)
    {
        if (boxItem == null || placementSystem == null) return;

        // ИСПРАВЛЕНО: Используем правильный метод HasItem
        if (inventorySystem != null && inventorySystem.GetItemCount(boxItem) > 0)
        {
            placementSystem.EnterPlacementMode(boxItem);
        }
    }
}