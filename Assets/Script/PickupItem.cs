using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    [SerializeField] private ItemData itemData; // ScriptableObject с данными предмета
    [SerializeField] private int quantity = 1;   // Количество предметов

    private Rigidbody rb;
    private Collider itemCollider;
    private bool isPickedUp = false; // здесь: "находится в руках" (для ItemPickupSystem)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
    }

    // ---------- IInteractable (для InteractionSystem) ----------

    public string GetInteractPrompt()
    {
        if (itemData == null)
            return "E - подобрать (нет ItemData)";

        if (quantity > 1)
            return $"E - подобрать {itemData.itemName} x{quantity}";
        else
            return $"E - подобрать {itemData.itemName}";
    }

    /// <summary>
    /// Взаимодействие через InteractionSystem: сразу пытаемся положить в инвентарь.
    /// </summary>
    public void Interact()
    {
        TryPickupToInventory();
    }

    // ---------- ЛОГИКА «СРАЗУ В ИНВЕНТАРЬ» ----------

    /// <summary>
    /// Подобрать предмет и положить в инвентарь (быстрый подбор, без "в руках").
    /// </summary>
    private void TryPickupToInventory()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"PickupItem на объекте {name}: не назначен ItemData");
            return;
        }

        InventorySystem inventory = FindPlayerInventory();
        if (inventory == null)
        {
            Debug.LogWarning("PickupItem: не найден InventorySystem в сцене!");
            return;
        }

        int addQuantity = Mathf.Max(1, quantity);
        bool added = inventory.AddItem(itemData, addQuantity);

        if (added)
        {
            Debug.Log($"Подобран предмет: {itemData.itemName} x{addQuantity}");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Инвентарь заполнен — предмет не подобран");
        }
    }

    // ---------- Методы для ItemPickupSystem ----------

    /// <summary>
    /// Старый интерфейс: взять предмет "в руки" (НЕ в инвентарь).
    /// ItemPickupSystem вызывает это, когда начинает удерживать объект перед камерой.
    /// </summary>
    public void OnPickup()
    {
        isPickedUp = true;

        // отключаем физику и столкновения, чтобы не дергался
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (itemCollider != null)
            itemCollider.enabled = false;
    }

    /// <summary>
    /// Старый интерфейс: отпустить предмет обратно в мир.
    /// ItemPickupSystem вызывает это, когда мы бросаем удерживаемый предмет.
    /// </summary>
    public void OnDrop()
    {
        isPickedUp = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        if (itemCollider != null)
            itemCollider.enabled = true;
    }

    /// <summary>
    /// Имя предмета для подсказок (используется в ItemPickupSystem).
    /// </summary>
    public string GetItemName()
    {
        return itemData != null ? itemData.itemName : gameObject.name;
    }

    // Эти методы уже ожидает твой ItemPickupSystem:

    public ItemData GetItemData()
    {
        return itemData;
    }

    public int GetQuantity()
    {
        return quantity;
    }

    public bool IsPickedUp()
    {
        return isPickedUp;
    }

    // ---------- Вспомогательное ----------

    private InventorySystem FindPlayerInventory()
    {
#if UNITY_6000_0_OR_NEWER
        return Object.FindFirstObjectByType<InventorySystem>();
#else
        return FindObjectOfType<InventorySystem>();
#endif
    }
}
