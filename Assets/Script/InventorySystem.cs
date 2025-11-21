using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public InventorySlot(ItemData item = null, int quantity = 0)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public bool IsEmpty()
    {
        return item == null || quantity <= 0;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

public class InventorySystem : MonoBehaviour
{
    // === ОБЩИЙ ФЛАГ, ЧТОБЫ ДРУГИЕ СКРИПТЫ ЗНАЛИ, ОТКРЫТ ЛИ ИНВЕНТАРЬ ===
    public static bool IsOpen { get; private set; }

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20;
    [SerializeField] public float maxWeight = 50f;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private float currentWeight = 0f;
    private int selectedSlotIndex = -1;

    [Header("UI")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;

    private List<InventorySlotUI> slotUIElements = new List<InventorySlotUI>();
    private bool isInventoryOpen = false;

    void Start()
    {
        // создаём пустые слоты
        for (int i = 0; i < inventorySize; i++)
            slots.Add(new InventorySlot());

        CreateInventoryUI();

        if (inventoryUI != null)
            inventoryUI.SetActive(false);

        // курсор по умолчанию спрятан
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // открыть/закрыть на Tab или I
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // использовать выбранный слот (E) когда инвентарь открыт
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.E))
        {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
                UseItem(selectedSlotIndex);
        }

        // быстрые слоты (1–9) только когда инвентарь закрыт
        if (!isInventoryOpen)
        {
            for (int i = 0; i < 9 && i < slots.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    UseItem(i);
            }
        }
    }

    // ---------- UI ----------

    void CreateInventoryUI()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogWarning("Slots Container или Slot Prefab не назначены!");
            return;
        }

        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.SetSlotIndex(i);
                slotUI.SetInventory(this);
                slotUIElements.Add(slotUI);
            }
        }

        UpdateInventoryUI();
    }

    void UpdateInventoryUI()
    {
        for (int i = 0; i < slotUIElements.Count && i < slots.Count; i++)
        {
            slotUIElements[i].UpdateSlot(slots[i]);
        }
    }

    // ---------- ОТКРЫТИЕ / ЗАКРЫТИЕ ----------

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        IsOpen = isInventoryOpen; // статический флаг для других скриптов

        if (inventoryUI != null)
            inventoryUI.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // сбрасываем выделение слота при закрытии (по желанию)
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIElements.Count)
                slotUIElements[selectedSlotIndex].SetSelected(false);

            selectedSlotIndex = -1;
        }

        UpdateInventoryUI();
    }

    // ---------- РАБОТА С ПРЕДМЕТАМИ ----------

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        float totalWeight = item.weight * quantity;
        if (currentWeight + totalWeight > maxWeight)
        {
            Debug.Log("Инвентарь переполнен по весу");
            return false;
        }

        // если стакается – сначала дополняем существующие стаки
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                if (!slots[i].IsEmpty() && slots[i].item == item &&
                    slots[i].quantity < item.maxStackSize)
                {
                    int canAdd = Mathf.Min(quantity, item.maxStackSize - slots[i].quantity);
                    slots[i].quantity += canAdd;
                    quantity -= canAdd;
                    currentWeight += item.weight * canAdd;
                }
            }
        }

        // остаток или нестакаемый – в пустые слоты
        while (quantity > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                Debug.Log("Нет свободных слотов");
                UpdateInventoryUI();
                return false;
            }

            int addAmount = item.isStackable ? Mathf.Min(quantity, item.maxStackSize) : 1;
            slots[emptySlot].item = item;
            slots[emptySlot].quantity = addAmount;
            quantity -= addAmount;
            currentWeight += item.weight * addAmount;
        }

        UpdateInventoryUI();
        return true;
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        for (int i = 0; i < slots.Count && quantity > 0; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].item == item)
            {
                int removeAmount = Mathf.Min(quantity, slots[i].quantity);
                slots[i].quantity -= removeAmount;
                currentWeight -= item.weight * removeAmount;
                quantity -= removeAmount;

                if (slots[i].quantity <= 0)
                    slots[i].Clear();
            }
        }

        UpdateInventoryUI();
        return quantity <= 0;
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty()) return;

        // применяем эффект
        slot.item.Use(gameObject);

        slot.quantity--;
        currentWeight -= slot.item.weight;

        if (slot.quantity <= 0)
            slot.Clear();

        UpdateInventoryUI();
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty()) return;

        ItemData itemToDrop = slot.item;

        slot.quantity--;
        currentWeight -= slot.item.weight;

        if (slot.quantity <= 0)
            slot.Clear();

        UpdateInventoryUI();

        if (itemToDrop.worldPrefab != null)
        {
            Vector3 dropPos = transform.position + transform.forward * 2f + Vector3.up;
            GameObject dropped = Instantiate(itemToDrop.worldPrefab, dropPos, Random.rotation);

            Rigidbody rb = dropped.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 force = (transform.forward + Vector3.up * 0.3f) * 3f;
                rb.AddForce(force, ForceMode.Impulse);
            }
        }

        Debug.Log($"Выброшен: {itemToDrop.itemName}");
    }

    public void SwapSlots(int slotA, int slotB)
    {
        if (slotA < 0 || slotA >= slots.Count) return;
        if (slotB < 0 || slotB >= slots.Count) return;
        if (slotA == slotB) return;

        InventorySlot temp = new InventorySlot(slots[slotA].item, slots[slotA].quantity);

        slots[slotA].item = slots[slotB].item;
        slots[slotA].quantity = slots[slotB].quantity;

        slots[slotB].item = temp.item;
        slots[slotB].quantity = temp.quantity;

        UpdateInventoryUI();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        if (slots[index].IsEmpty()) return;

        // снять выделение
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIElements.Count)
            slotUIElements[selectedSlotIndex].SetSelected(false);

        if (selectedSlotIndex == index)
        {
            // повторный клик — снимаем выделение
            selectedSlotIndex = -1;
        }
        else
        {
            selectedSlotIndex = index;
            if (selectedSlotIndex < slotUIElements.Count)
                slotUIElements[selectedSlotIndex].SetSelected(true);
        }
    }

    int FindEmptySlot()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty())
                return i;
        }
        return -1;
    }

    // ---------- ГЕТТЕРЫ ----------

    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
            return slots[index];
        return null;
    }

    public int GetItemCount(ItemData item)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.item == item)
                count += slot.quantity;
        }
        return count;
    }

    public float GetCurrentWeight() => currentWeight;
    public float GetMaxWeight() => maxWeight;
    public bool IsInventoryOpenInstance() => isInventoryOpen; // если где-то используется старый метод

    public bool HasItem(ItemData item) => GetItemCount(item) > 0;

    public List<InventorySlot> GetInventorySlots() => slots;
}
