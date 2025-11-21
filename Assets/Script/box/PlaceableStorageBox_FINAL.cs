using UnityEngine;
using System.Collections.Generic;

public class PlaceableStorageBox : MonoBehaviour, IInteractable
{
    [Header("Storage Settings")]
    [SerializeField] private int storageSlots = 10;
    [SerializeField] private float maxStorageWeight = 0f;

    [Header("Interaction")]
    [SerializeField] private string interactPrompt = "[E] Открыть ящик";
    [SerializeField] private float interactionDistance = 2f;

    // Хранилище предметов
    private List<InventorySlot> storageItems = new List<InventorySlot>();

    // Ссылка на UI (будет установлена StorageBoxUI)
    private StorageBoxUI storageUI;

    // ID ящика для сохранения
    private string boxID;

    void Awake()
    {
        // Генерируем уникальный ID для ящика
        boxID = System.Guid.NewGuid().ToString();

        // Инициализируем слоты хранения
        InitializeStorage();
    }

    void Start()
    {
        // Находим UI менеджер
        storageUI = FindFirstObjectByType<StorageBoxUI>();
        if (storageUI == null)
        {
            Debug.LogError("StorageBoxUI не найден в сцене!");
        }
    }

    private void InitializeStorage()
    {
        storageItems.Clear();
        for (int i = 0; i < storageSlots; i++)
        {
            // ИСПРАВЛЕНО: Передаём два параметра в конструктор
            storageItems.Add(new InventorySlot(null, 0));
        }
    }

    // Установка настроек ящика при создании
    public void SetupBox(int slots, float maxWeight)
    {
        storageSlots = slots;
        maxStorageWeight = maxWeight;
        InitializeStorage();
    }

    #region IInteractable Implementation

    public string GetInteractPrompt()
    {
        // Показываем количество заполненных слотов
        int usedSlots = 0;
        foreach (var slot in storageItems)
        {
            if (!slot.IsEmpty()) usedSlots++;
        }

        return $"{interactPrompt} [{usedSlots}/{storageSlots}]";
    }

    public void Interact()
    {
        // Находим игрока через InteractionSystem
        InteractionSystem interactionSystem = FindFirstObjectByType<InteractionSystem>();
        if (interactionSystem != null)
        {
            GameObject player = interactionSystem.gameObject;
            OpenStorage(player);
        }
        else
        {
            Debug.LogError("InteractionSystem не найден!");
        }
    }

    #endregion

    private void OpenStorage(GameObject player)
    {
        if (storageUI != null)
        {
            storageUI.OpenStorage(this, player);
        }
        else
        {
            Debug.LogError("StorageBoxUI не назначен!");
        }
    }

    #region Storage Management

    public List<InventorySlot> GetStorageSlots()
    {
        return storageItems;
    }

    public int GetStorageCapacity()
    {
        return storageSlots;
    }

    public float GetMaxStorageWeight()
    {
        return maxStorageWeight;
    }

    public float GetCurrentWeight()
    {
        float totalWeight = 0f;
        foreach (var slot in storageItems)
        {
            if (!slot.IsEmpty())
            {
                totalWeight += slot.item.weight * slot.quantity;
            }
        }
        return totalWeight;
    }

    // Добавить предмет в хранилище
    public bool AddItem(ItemData item, int quantity = 1)
    {
        // Проверка веса
        if (maxStorageWeight > 0)
        {
            float newWeight = GetCurrentWeight() + (item.weight * quantity);
            if (newWeight > maxStorageWeight)
            {
                Debug.Log("Ящик перегружен!");
                return false;
            }
        }

        // Попытка добавить в существующий стак
        if (item.isStackable)
        {
            foreach (var slot in storageItems)
            {
                if (slot.item == item && slot.quantity < item.maxStackSize)
                {
                    int spaceLeft = item.maxStackSize - slot.quantity;
                    int amountToAdd = Mathf.Min(quantity, spaceLeft);
                    slot.quantity += amountToAdd;
                    quantity -= amountToAdd;

                    if (quantity <= 0)
                        return true;
                }
            }
        }

        // Добавить в пустые слоты
        while (quantity > 0)
        {
            InventorySlot emptySlot = storageItems.Find(s => s.IsEmpty());
            if (emptySlot == null)
            {
                Debug.Log("Ящик полон!");
                return false;
            }

            int amountToAdd = item.isStackable ? Mathf.Min(quantity, item.maxStackSize) : 1;
            emptySlot.item = item;
            emptySlot.quantity = amountToAdd;
            quantity -= amountToAdd;
        }

        return true;
    }

    // Убрать предмет из хранилища
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        int remaining = quantity;

        for (int i = storageItems.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = storageItems[i];
            if (slot.item == item)
            {
                int removeAmount = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= removeAmount;
                remaining -= removeAmount;

                if (slot.quantity <= 0)
                {
                    slot.Clear();
                }
            }
        }

        return remaining == 0;
    }

    #endregion

    // Для отладки
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }

    // Сохранение/загрузка (для будущей реализации)
    public string GetBoxID()
    {
        return boxID;
    }
}