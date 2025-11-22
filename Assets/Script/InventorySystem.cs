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
    /// <summary>
    /// Открыт ли сейчас ИНВЕНТАРЬ (для FPSController).
    /// </summary>
    public static bool IsOpen { get; private set; }

    [Header("Inventory Settings")]
    [SerializeField] private int columns = 5;   // сколько слотов по горизонтали
    [SerializeField] private int rows = 4;      // сколько слотов по вертикали
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
        int inventorySize = Mathf.Max(1, columns * rows);

        // создаём пустые слоты
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot());
        }

        CreateInventoryUI();

        if (inventoryUI != null)
            inventoryUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // открыть/закрыть инвентарь
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // использовать выбранный предмет (E) при открытом инвентаре
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.E))
        {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
                UseItem(selectedSlotIndex);
        }

        // быстрые слоты 1–9 при закрытом инвентаре
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

    // ---------- UI ----------

    void CreateInventoryUI()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogWarning("InventorySystem: SlotsContainer или SlotPrefab не назначены!");
            return;
        }

        foreach (Transform child in slotsContainer)
            Destroy(child.gameObject);

        slotUIElements.Clear();

        int inventorySize = Mathf.Max(1, columns * rows);

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

        UpdateSelectionHighlight();
    }

    void UpdateSelectionHighlight()
    {
        // снять выделение со всех
        for (int i = 0; i < slotUIElements.Count; i++)
            slotUIElements[i].SetSelected(false);

        // выделить текущий
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIElements.Count)
            slotUIElements[selectedSlotIndex].SetSelected(true);
    }

    // ---------- ОТКРЫТИЕ / ЗАКРЫТИЕ ----------

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        IsOpen = isInventoryOpen;

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

            // сбросить выделение
            selectedSlotIndex = -1;
        }

        UpdateInventoryUI();
    }

    // ---------- РАБОТА С ПРЕДМЕТАМИ ----------

    /// <summary>
    /// Добавить предмет в инвентарь (простая схема: 1 предмет = 1 слот).
    /// </summary>
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        float totalWeight = item.weight * quantity;
        if (currentWeight + totalWeight > maxWeight)
        {
            Debug.Log("Инвентарь переполнен по весу");
            return false;
        }

        // стакаемые предметы — сначала докидываем в уже имеющиеся стаки
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                var s = slots[i];
                if (!s.IsEmpty() && s.item == item && s.quantity < item.maxStackSize)
                {
                    int canAdd = Mathf.Min(quantity, item.maxStackSize - s.quantity);
                    s.quantity += canAdd;
                    quantity -= canAdd;
                    currentWeight += item.weight * canAdd;
                }
            }
        }

        // остаток — в пустые слоты
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

    /// <summary>
    /// Убрать quantity штук предмета item.
    /// </summary>
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        for (int i = 0; i < slots.Count && quantity > 0; i++)
        {
            var s = slots[i];
            if (!s.IsEmpty() && s.item == item)
            {
                int remove = Mathf.Min(quantity, s.quantity);
                s.quantity -= remove;
                currentWeight -= item.weight * remove;
                quantity -= remove;

                if (s.quantity <= 0)
                    s.Clear();
            }
        }

        UpdateInventoryUI();
        return quantity <= 0;
    }

    /// <summary>
    /// Использовать предмет по индексу слота.
    /// </summary>
    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty()) return;

        slot.item.Use(gameObject);

        slot.quantity--;
        currentWeight -= slot.item.weight;

        if (slot.quantity <= 0)
            slot.Clear();

        UpdateInventoryUI();
    }

    /// <summary>
    /// Выбросить 1 предмет из слота.
    /// </summary>
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

    // ---------- ВЗАИМОДЕЙСТВИЕ С UI ----------

    /// <summary>
    /// Выбор слота (клик по ячейке из InventorySlotUI).
    /// </summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            selectedSlotIndex = -1;
            UpdateSelectionHighlight();
            return;
        }

        if (slots[index].IsEmpty())
        {
            selectedSlotIndex = -1;
        }
        else
        {
            selectedSlotIndex = index;
        }

        UpdateSelectionHighlight();
    }

    /// <summary>
    /// Обмен содержимым двух слотов (Drag & Drop).
    /// </summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count) return;
        if (indexB < 0 || indexB >= slots.Count) return;
        if (indexA == indexB) return;

        InventorySlot temp = new InventorySlot(slots[indexA].item, slots[indexA].quantity);

        slots[indexA].item = slots[indexB].item;
        slots[indexA].quantity = slots[indexB].quantity;

        slots[indexB].item = temp.item;
        slots[indexB].quantity = temp.quantity;

        UpdateInventoryUI();
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

    public float GetCurrentWeight() => currentWeight;
    public float GetMaxWeight() => maxWeight;
    public bool IsInventoryOpenInstance() => isInventoryOpen;
    public List<InventorySlot> GetInventorySlots() => slots;
}
