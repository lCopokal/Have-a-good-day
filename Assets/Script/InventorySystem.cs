using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    // Для многоклеточных предметов
    public bool isPartOfComposite; // этот слот занят большим предметом
    public bool isRoot;            // корневой (левый верхний) слот предмета
    public int rootIndex = -1;     // индекс корня в списке слотов
    public bool rotated;           // повернут ли предмет (ширина/высота местами)

    public InventorySlot(ItemData item = null, int quantity = 0)
    {
        this.item = item;
        this.quantity = quantity;
        isPartOfComposite = false;
        isRoot = false;
        rootIndex = -1;
        rotated = false;
    }

    public bool IsEmpty()
    {
        // Пустой только если не часть предмета и нет количества/предмета
        if (isPartOfComposite)
            return false;

        return item == null || quantity <= 0;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
        isPartOfComposite = false;
        isRoot = false;
        rootIndex = -1;
        rotated = false;
    }
}

public class InventorySystem : MonoBehaviour
{
    /// <summary>
    /// Открыт ли сейчас инвентарь (для других скриптов, типа FPSController).
    /// </summary>
    public static bool IsOpen { get; private set; }

    [Header("Inventory Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] public float maxWeight = 50f;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private float currentWeight = 0f;

    // выбранный предмет — индекс КОРНЕВОГО слота
    private int selectedRootIndex = -1;

    [Header("UI")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;

    private List<InventorySlotUI> slotUIElements = new List<InventorySlotUI>();
    private bool isInventoryOpen = false;

    void Start()
    {
        int inventorySize = Mathf.Max(1, columns * rows);

        slots.Clear();
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

        if (isInventoryOpen)
        {
            // использовать выбранный предмет
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (selectedRootIndex >= 0 && selectedRootIndex < slots.Count)
                    UseItem(selectedRootIndex);
            }

            // повернуть выбранный предмет (как в Таркове)
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateSelectedItem();
            }
        }
        else
        {
            // быстрые слоты 1–9 при закрытом инвентаре
            for (int i = 0; i < 9 && i < slots.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    int root = GetRootIndex(i);
                    if (root != -1)
                        UseItem(root);
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
        // Сначала снимаем выделение со всех слотов
        for (int i = 0; i < slotUIElements.Count; i++)
            slotUIElements[i].SetSelected(false);

        if (selectedRootIndex < 0 || selectedRootIndex >= slots.Count)
            return;

        InventorySlot rootSlot = slots[selectedRootIndex];

        // Если предмет многоклеточный – подсвечиваем ВСЕ его клетки
        if (rootSlot.isPartOfComposite)
        {
            for (int i = 0; i < slots.Count && i < slotUIElements.Count; i++)
            {
                InventorySlot s = slots[i];
                if (s.isPartOfComposite && s.rootIndex == selectedRootIndex)
                {
                    slotUIElements[i].SetSelected(true);
                }
            }
        }
        else
        {
            // Обычный одиночный предмет 1×1 – подсвечиваем только один слот
            if (!rootSlot.IsEmpty() && selectedRootIndex < slotUIElements.Count)
            {
                slotUIElements[selectedRootIndex].SetSelected(true);
            }
        }
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
            selectedRootIndex = -1;
        }

        UpdateInventoryUI();
    }

    // ---------- ВНУТРЕННЯЯ ГЕОМЕТРИЯ СЕТКИ ----------

    int GetIndex(int x, int y)
    {
        return y * columns + x;
    }

    void GetXY(int index, out int x, out int y)
    {
        y = index / columns;
        x = index % columns;
    }

    bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < columns && y < rows;
    }

    bool IsAreaFree(int startX, int startY, int w, int h)
    {
        for (int yy = 0; yy < h; yy++)
        {
            for (int xx = 0; xx < w; xx++)
            {
                int x = startX + xx;
                int y = startY + yy;
                if (!IsInsideGrid(x, y))
                    return false;

                int idx = GetIndex(x, y);
                if (!slots[idx].IsEmpty())
                    return false;
            }
        }
        return true;
    }

    bool FindSpaceFor(ItemData item, int w, int h, out int foundX, out int foundY)
    {
        for (int y = 0; y <= rows - h; y++)
        {
            for (int x = 0; x <= columns - w; x++)
            {
                if (IsAreaFree(x, y, w, h))
                {
                    foundX = x;
                    foundY = y;
                    return true;
                }
            }
        }

        foundX = -1;
        foundY = -1;
        return false;
    }

    void PlaceItemAt(ItemData item, int startX, int startY, int w, int h, bool rotated, int quantity)
    {
        int rootIdx = GetIndex(startX, startY);

        for (int yy = 0; yy < h; yy++)
        {
            for (int xx = 0; xx < w; xx++)
            {
                int x = startX + xx;
                int y = startY + yy;
                int idx = GetIndex(x, y);

                InventorySlot slot = slots[idx];
                slot.item = item;
                slot.isPartOfComposite = true;
                slot.rootIndex = rootIdx;
                slot.rotated = rotated;

                if (idx == rootIdx)
                {
                    slot.isRoot = true;
                    slot.quantity = quantity;
                }
                else
                {
                    slot.isRoot = false;
                    slot.quantity = 0;
                }
            }
        }
    }

    void ClearComposite(int rootIndex)
    {
        if (rootIndex < 0 || rootIndex >= slots.Count) return;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot s = slots[i];
            if (s.isPartOfComposite && s.rootIndex == rootIndex)
                s.Clear();
        }
    }

    int GetRootIndex(int index)
    {
        if (index < 0 || index >= slots.Count)
            return -1;

        var s = slots[index];
        if (!s.isPartOfComposite)
        {
            if (s.IsEmpty())
                return -1;
            else
                return index;
        }

        if (s.isRoot)
            return index;

        return s.rootIndex;
    }

    // ---------- РАБОТА С ПРЕДМЕТАМИ ----------

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        // Проверка веса
        float totalWeight = item.weight * quantity;
        if (currentWeight + totalWeight > maxWeight)
        {
            Debug.Log("Инвентарь переполнен по весу");
            return false;
        }

        int baseW = Mathf.Max(1, item.widthInSlots);
        int baseH = Mathf.Max(1, item.heightInSlots);

        // ==== СЛУЧАЙ 1: ПРОСТОЙ ПРЕДМЕТ 1×1 (стакуемый) ====
        // Для них НИЧЕГО не трогаем с isPartOfComposite / isRoot,
        // чтобы drag&drop работал как у обычных слотов.
        if (baseW == 1 && baseH == 1 && item.isStackable)
        {
            // Сначала заполняем существующие стеки
            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                var s = slots[i];
                if (!s.IsEmpty() &&
                    s.item == item &&
                    s.quantity < item.maxStackSize)
                {
                    int canAdd = Mathf.Min(quantity, item.maxStackSize - s.quantity);
                    if (canAdd > 0)
                    {
                        s.quantity += canAdd;
                        quantity -= canAdd;
                        currentWeight += item.weight * canAdd;
                    }
                }
            }

            // Остаток — в пустые слоты по 1×1, БЕЗ composite-флагов
            while (quantity > 0)
            {
                int emptyIndex = -1;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].IsEmpty())
                    {
                        emptyIndex = i;
                        break;
                    }
                }

                if (emptyIndex == -1)
                {
                    Debug.Log("Нет места в инвентаре для предмета " + item.itemName);
                    UpdateInventoryUI();
                    return false;
                }

                int addAmount = Mathf.Min(quantity, item.maxStackSize);

                InventorySlot slot = slots[emptyIndex];
                slot.item = item;
                slot.quantity = addAmount;
                // ВАЖНО: для 1×1 НЕ помечаем как composite
                slot.isPartOfComposite = false;
                slot.isRoot = false;
                slot.rootIndex = -1;
                slot.rotated = false;

                currentWeight += item.weight * addAmount;
                quantity -= addAmount;
            }

            UpdateInventoryUI();
            return true;
        }

        // ==== СЛУЧАЙ 2: НЕСТАКУЕМЫЙ ИЛИ МНОГОКЛЕТОЧНЫЙ ПРЕДМЕТ ====

        // Нестакуемый или размер > 1×1 — каждый экземпляр занимает прямоугольник W×H
        while (quantity > 0)
        {
            int x, y;
            if (!FindSpaceFor(item, baseW, baseH, out x, out y))
            {
                Debug.Log("Нет места в инвентаре для предмета (многоклеточный) " + item.itemName);
                UpdateInventoryUI();
                return false;
            }

            // Для этих уже используем PlaceItemAt и composite-флаги
            PlaceItemAt(item, x, y, baseW, baseH, false, 1);
            currentWeight += item.weight;
            quantity--;
        }

        UpdateInventoryUI();
        return true;
    }


    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        for (int i = 0; i < slots.Count && quantity > 0; i++)
        {
            InventorySlot s = slots[i];

            bool isRootSlot =
                !s.IsEmpty() &&
                s.item == item &&
                (!s.isPartOfComposite || s.isRoot);

            if (!isRootSlot)
                continue;

            int remove = Mathf.Min(quantity, s.quantity);
            s.quantity -= remove;
            currentWeight -= item.weight * remove;
            quantity -= remove;

            if (s.quantity <= 0)
            {
                if (s.isPartOfComposite)
                    ClearComposite(i);
                else
                    s.Clear();
            }
        }

        UpdateInventoryUI();
        return quantity <= 0;
    }

    public void UseItem(int rootIndex)
    {
        rootIndex = GetRootIndex(rootIndex);
        if (rootIndex < 0 || rootIndex >= slots.Count) return;

        InventorySlot slot = slots[rootIndex];
        if (slot.IsEmpty()) return;

        slot.item.Use(gameObject);

        slot.quantity--;
        currentWeight -= slot.item.weight;

        if (slot.quantity <= 0)
        {
            if (slot.isPartOfComposite)
                ClearComposite(rootIndex);
            else
                slot.Clear();
        }

        UpdateInventoryUI();
    }

    public void DropItem(int index)
    {
        int rootIndex = GetRootIndex(index);
        if (rootIndex < 0 || rootIndex >= slots.Count) return;

        InventorySlot slot = slots[rootIndex];
        if (slot.IsEmpty()) return;

        ItemData itemToDrop = slot.item;

        slot.quantity--;
        currentWeight -= slot.item.weight;

        if (slot.quantity <= 0)
        {
            if (slot.isPartOfComposite)
                ClearComposite(rootIndex);
            else
                slot.Clear();
        }

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

    public void SelectSlot(int index)
    {
        int root = GetRootIndex(index);
        if (root == -1)
            selectedRootIndex = -1;
        else
            selectedRootIndex = root;

        UpdateSelectionHighlight();
    }
    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= slots.Count) return;
        if (toIndex < 0 || toIndex >= slots.Count) return;

        int rootFrom = GetRootIndex(fromIndex);
        if (rootFrom == -1) return;

        int rootTo = GetRootIndex(toIndex);

        // Если кидаем на тот же предмет – ничего не делаем
        if (rootFrom == rootTo)
        {
            UpdateInventoryUI();
            return;
        }

        InventorySlot rootSlot = slots[rootFrom];
        if (rootSlot.IsEmpty() || rootSlot.item == null) return;

        // ----- СЛУЧАЙ 1: 1×1 предмет -----
        if (!rootSlot.isPartOfComposite)
        {
            InventorySlot targetSlot = slots[toIndex];

            // Нельзя класть поверх многоклеточного
            if (targetSlot.isPartOfComposite)
            {
                Debug.Log("Нельзя положить 1×1 на многоклеточный предмет");
                UpdateInventoryUI();
                return;
            }

            // Если целевой слот пуст — переносим
            if (targetSlot.IsEmpty())
            {
                targetSlot.item = rootSlot.item;
                targetSlot.quantity = rootSlot.quantity;

                rootSlot.Clear();
                UpdateInventoryUI();
                return;
            }

            // Если такой же предмет — стакаем
            if (targetSlot.item == rootSlot.item && rootSlot.item.isStackable)
            {
                int canAdd = targetSlot.item.maxStackSize - targetSlot.quantity;
                if (canAdd > 0)
                {
                    int moved = Mathf.Min(canAdd, rootSlot.quantity);
                    targetSlot.quantity += moved;
                    rootSlot.quantity -= moved;

                    if (rootSlot.quantity <= 0)
                        rootSlot.Clear();

                    UpdateInventoryUI();
                    return;
                }
            }

            // Иначе — swap 1×1
            ItemData tmpItem = targetSlot.item;
            int tmpQty = targetSlot.quantity;

            targetSlot.item = rootSlot.item;
            targetSlot.quantity = rootSlot.quantity;

            rootSlot.item = tmpItem;
            rootSlot.quantity = tmpQty;

            UpdateInventoryUI();
            return;
        }

        // ----- СЛУЧАЙ 2: БОЛЬШОЙ ПРЕДМЕТ -----

        ItemData item = rootSlot.item;
        int baseW = Mathf.Max(1, item.widthInSlots);
        int baseH = Mathf.Max(1, item.heightInSlots);
        bool rotated = rootSlot.rotated;

        int curW = rotated ? baseH : baseW;
        int curH = rotated ? baseW : baseH;

        GetXY(rootFrom, out int rootX, out int rootY);
        GetXY(fromIndex, out int fromX, out int fromY);
        GetXY(toIndex, out int toX, out int toY);

        int offsetX = fromX - rootX;
        int offsetY = fromY - rootY;

        int newRootX = toX - offsetX;
        int newRootY = toY - offsetY;

        // Проверяем выход за сетку
        if (newRootX < 0 || newRootY < 0 ||
            newRootX + curW > columns || newRootY + curH > rows)
        {
            Debug.Log("Нельзя переместить предмет — выходит за границы");
            UpdateInventoryUI();
            return;
        }

        // Проверяем, свободно ли новое место
        if (!CanRotateAt(item, rootFrom, newRootX, newRootY, curW, curH))
        {
            Debug.Log("Нельзя переместить предмет — место занято");
            UpdateInventoryUI();
            return;
        }

        int quantity = rootSlot.quantity;

        // Удаляем старый прямоугольник
        ClearComposite(rootFrom);

        // Ставим предмет в новую позицию
        PlaceItemAt(item, newRootX, newRootY, curW, curH, rotated, quantity);

        int newRootIndex = GetIndex(newRootX, newRootY);

        if (selectedRootIndex == rootFrom)
            selectedRootIndex = newRootIndex;

        UpdateInventoryUI();
    }


    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count) return;
        if (indexB < 0 || indexB >= slots.Count) return;
        if (indexA == indexB) return;

        // не поддерживаем драгендроп для многоклеточных пока
        if (slots[indexA].isPartOfComposite || slots[indexB].isPartOfComposite)
        {
            Debug.Log("Swap для многоклеточных предметов пока не реализован");
            return;
        }

        InventorySlot temp = new InventorySlot();
        temp.item = slots[indexA].item;
        temp.quantity = slots[indexA].quantity;

        slots[indexA].item = slots[indexB].item;
        slots[indexA].quantity = slots[indexB].quantity;

        slots[indexB].item = temp.item;
        slots[indexB].quantity = temp.quantity;

        UpdateInventoryUI();
    }

    // ---------- ПОВОРОТ ПРЕДМЕТА ----------

    void RotateSelectedItem()
    {
        if (selectedRootIndex < 0 || selectedRootIndex >= slots.Count) return;

        InventorySlot rootSlot = slots[selectedRootIndex];
        if (rootSlot.IsEmpty() || !rootSlot.isRoot || rootSlot.item == null)
            return;

        ItemData item = rootSlot.item;

        int baseW = Mathf.Max(1, item.widthInSlots);
        int baseH = Mathf.Max(1, item.heightInSlots);

        bool currentRot = rootSlot.rotated;
        int curW = currentRot ? baseH : baseW;
        int curH = currentRot ? baseW : baseH;

        int newW = curH;
        int newH = curW;

        GetXY(selectedRootIndex, out int startX, out int startY);

        // Проверяем, влезает ли новый прямоугольник в том же месте
        if (!CanRotateAt(item, selectedRootIndex, startX, startY, newW, newH))
        {
            Debug.Log("Нет места для поворота предмета");
            return;
        }

        int quantity = rootSlot.quantity;

        // Очищаем старый прямоугольник
        ClearComposite(selectedRootIndex);

        // Ставим в новом ориентации на том же месте
        PlaceItemAt(item, startX, startY, newW, newH, !currentRot, quantity);

        // Новый rootIndex тот же (левый верхний)
        int newRootIndex = GetIndex(startX, startY);
        selectedRootIndex = newRootIndex;

        UpdateInventoryUI();
    }

    bool CanRotateAt(ItemData item, int rootIndex, int startX, int startY, int newW, int newH)
    {
        // Проверяем, влезет ли прямоугольник в сетку
        if (startX + newW > columns || startY + newH > rows)
            return false;

        for (int yy = 0; yy < newH; yy++)
        {
            for (int xx = 0; xx < newW; xx++)
            {
                int x = startX + xx;
                int y = startY + yy;
                int idx = GetIndex(x, y);

                InventorySlot s = slots[idx];

                // Можно использовать либо свои прежние клетки, либо пустые
                if (s.isPartOfComposite && s.rootIndex == rootIndex)
                    continue; // своя часть — норм

                if (!s.IsEmpty())
                    return false;
            }
        }
        return true;
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
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot s = slots[i];
            if (!s.IsEmpty() && s.isRoot && s.item == item)
                count += s.quantity;
        }
        return count;
    }

    public float GetCurrentWeight() => currentWeight;
    public float GetMaxWeight() => maxWeight;
    public bool IsInventoryOpenInstance() => isInventoryOpen;
    public bool HasItem(ItemData item) => GetItemCount(item) > 0;
    public List<InventorySlot> GetInventorySlots() => slots;
}
