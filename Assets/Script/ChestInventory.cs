using UnityEngine;
using System.Collections.Generic;

public class ChestInventory : MonoBehaviour, IInteractable
{
    [Header("Настройки сундука")]
    [SerializeField] private bool canBePickedUp = true;    // можно ли забрать сундук в инвентарь
    [SerializeField] private ChestItemData chestItemData;  // какой предмет появится в инвентаре

    [Header("Содержимое сундука (стартовое)")]
    [SerializeField] private List<InventorySlot> startingItems = new List<InventorySlot>();

    // внутренние слоты сундука (пока только для отображения)
    private List<InventorySlot> slots = new List<InventorySlot>();

    public bool CanBePickedUp => canBePickedUp && chestItemData != null;

    void Awake()
    {
        // создаём пустые слоты на основе настройки в SO (если есть)
        int size = chestItemData != null ? Mathf.Max(1, chestItemData.chestSlots) : 12;

        for (int i = 0; i < size; i++)
            slots.Add(new InventorySlot());

        // копируем стартовые предметы (по-простому, без стака)
        if (startingItems != null)
        {
            for (int i = 0; i < startingItems.Count && i < slots.Count; i++)
            {
                if (startingItems[i] != null && !startingItems[i].IsEmpty())
                {
                    slots[i].item = startingItems[i].item;
                    slots[i].quantity = startingItems[i].quantity;
                }
            }
        }
    }

    // ---------- IInteractable ----------

    public string GetInteractPrompt()
    {
        if (CanBePickedUp)
            return "[E] Открыть сундук (удерживайте E, чтобы забрать)";
        else
            return "[E] Открыть сундук";
    }

    // Короткое нажатие E — открыть сундук (показать UI)
    public void Interact()
    {
        InventorySystem playerInventory = FindFirstObjectByType<InventorySystem>();
        if (playerInventory == null)
        {
            Debug.LogWarning("ChestInventory: не найден InventorySystem игрока");
            return;
        }

        ChestUIManager.Instance?.OpenChest(this, playerInventory);
    }

    // Долгое удержание E — забрать сундук в инвентарь и удалить из мира
    public void PickupChest(InventorySystem inventory)
    {
        if (!CanBePickedUp || inventory == null) return;

        bool added = inventory.AddItem(chestItemData, 1);
        if (added)
        {
            // при желании здесь можно выкидывать содержимое сундука на землю
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("ChestInventory: не удалось добавить сундук в инвентарь (нет веса/слотов)");
        }
    }

    // ---------- Доступ для UI ----------

    public int GetSize()
    {
        return slots.Count;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
            return slots[index];
        return null;
    }

    public List<InventorySlot> GetAllSlots()
    {
        return slots;
    }
}
