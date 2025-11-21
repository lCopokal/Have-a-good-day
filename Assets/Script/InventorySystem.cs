using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public InventorySlot(ItemData item, int quantity)
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
    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20;
    [SerializeField] public float maxWeight = 50f; // public для доступа

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
        // Инициализируем слоты
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot(null, 0));
        }

        // Создаём UI слоты
        CreateInventoryUI();

        // Скрываем инвентарь при старте
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }
    }

    void Update()
    {
        // Открытие/закрытие инвентаря на Tab или I
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // Использование выбранного предмета на E (только когда инвентарь открыт)
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.E))
        {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
            {
                UseItem(selectedSlotIndex);
            }
        }

        // Быстрое использование предметов (1-9) когда инвентарь закрыт
        if (!isInventoryOpen)
        {
            for (int i = 0; i < 9 && i < slots.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    UseItem(i);
                }
            }
        }
    }

    void CreateInventoryUI()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogWarning("Slots Container или Slot Prefab не назначены!");
            return;
        }

        // Создаём UI для каждого слота
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

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isInventoryOpen);
        }

        // Управление курсором
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Отключаем FPS контроллер
            FPSController fps = GetComponent<FPSController>();
            if (fps != null) fps.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Включаем FPS контроллер
            FPSController fps = GetComponent<FPSController>();
            if (fps != null) fps.enabled = true;
        }

        UpdateInventoryUI();
    }

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        // Проверяем вес
        float totalWeight = item.weight * quantity;
        if (currentWeight + totalWeight > maxWeight)
        {
            Debug.Log("Инвентарь переполнен! Слишком тяжело.");
            return false;
        }

        // Если предмет складываемый - ищем существующий стак
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty() && slots[i].item == item)
                {
                    // Проверяем, не превысит ли количество максимум
                    int canAdd = Mathf.Min(quantity, item.maxStackSize - slots[i].quantity);

                    if (canAdd > 0)
                    {
                        slots[i].quantity += canAdd;
                        currentWeight += item.weight * canAdd;
                        quantity -= canAdd;

                        if (quantity <= 0)
                        {
                            UpdateInventoryUI();
                            Debug.Log($"Добавлено: {item.itemName} x{canAdd}");
                            return true;
                        }
                    }
                }
            }
        }

        // Если остались предметы или предмет не складываемый - ищем пустой слот
        while (quantity > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                Debug.Log("Нет свободных слотов!");
                return false;
            }

            int addAmount = item.isStackable ? Mathf.Min(quantity, item.maxStackSize) : 1;

            slots[emptySlot].item = item;
            slots[emptySlot].quantity = addAmount;
            currentWeight += item.weight * addAmount;
            quantity -= addAmount;
        }

        UpdateInventoryUI();
        Debug.Log($"Добавлено: {item.itemName}");
        return true;
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].item == item)
            {
                int removeAmount = Mathf.Min(quantity, slots[i].quantity);
                slots[i].quantity -= removeAmount;
                currentWeight -= item.weight * removeAmount;
                quantity -= removeAmount;

                if (slots[i].quantity <= 0)
                {
                    slots[i].Clear();
                }

                if (quantity <= 0)
                {
                    UpdateInventoryUI();
                    return true;
                }
            }
        }

        UpdateInventoryUI();
        return false;
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty()) return;

        // Используем предмет
        slot.item.Use(gameObject);

        // Уменьшаем количество
        slot.quantity--;
        currentWeight -= slot.item.weight;

        if (slot.quantity <= 0)
        {
            slot.Clear();
        }

        UpdateInventoryUI();
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty()) return;

        ItemData itemToDrop = slot.item;

        // Уменьшаем количество на 1
        slot.quantity--;
        currentWeight -= slot.item.weight;

        // Если количество стало 0 - очищаем слот
        if (slot.quantity <= 0)
        {
            slot.Clear();
        }

        UpdateInventoryUI();

        // Создаём ОДИН предмет в мире
        if (itemToDrop.worldPrefab != null)
        {
            Vector3 dropPosition = transform.position + transform.forward * 2f + Vector3.up * 1f;

            GameObject droppedItem = Instantiate(
                itemToDrop.worldPrefab,
                dropPosition,
                Random.rotation
            );

            // Добавляем силу броска
            Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 force = (transform.forward + Vector3.up * 0.3f) * 3f;
                rb.AddForce(force, ForceMode.Impulse);
            }
        }

        Debug.Log($"Выброшен 1 предмет: {itemToDrop.itemName}");
    }

    public void SwapSlots(int slotA, int slotB)
    {
        if (slotA < 0 || slotA >= slots.Count) return;
        if (slotB < 0 || slotB >= slots.Count) return;
        if (slotA == slotB) return;

        // Меняем содержимое слотов местами
        InventorySlot temp = new InventorySlot(slots[slotA].item, slots[slotA].quantity);

        slots[slotA].item = slots[slotB].item;
        slots[slotA].quantity = slots[slotB].quantity;

        slots[slotB].item = temp.item;
        slots[slotB].quantity = temp.quantity;

        UpdateInventoryUI();
        Debug.Log($"Предметы поменяны местами: слот {slotA} <-> слот {slotB}");
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        if (slots[index].IsEmpty()) return;

        // Снимаем выделение с предыдущего слота
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIElements.Count)
        {
            slotUIElements[selectedSlotIndex].SetSelected(false);
        }

        // Если кликнули на уже выбранный слот - снимаем выделение
        if (selectedSlotIndex == index)
        {
            selectedSlotIndex = -1;
        }
        else
        {
            // Выбираем новый слот
            selectedSlotIndex = index;
            if (selectedSlotIndex < slotUIElements.Count)
            {
                slotUIElements[selectedSlotIndex].SetSelected(true);
            }
        }

        Debug.Log(selectedSlotIndex >= 0 ? $"Выбран слот {selectedSlotIndex}" : "Выделение снято");
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

    void UpdateInventoryUI()
    {
        for (int i = 0; i < slotUIElements.Count && i < slots.Count; i++)
        {
            slotUIElements[i].UpdateSlot(slots[i]);
        }
    }

    // Геттеры
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
            {
                count += slot.quantity;
            }
        }
        return count;
    }

    public float GetCurrentWeight() { return currentWeight; }
    public float GetMaxWeight() { return maxWeight; }
    public bool IsInventoryOpen() { return isInventoryOpen; }

    public bool HasItem(ItemData item)
    {
        return GetItemCount(item) > 0;
    }

    public List<InventorySlot> GetInventorySlots()
    {
        return slots;
    }
}