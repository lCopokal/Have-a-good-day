using UnityEngine;

// Типы предметов
public enum ItemType
{
    Consumable,  // Расходник (еда, вода, аптечка)
    Weapon,      // Оружие
    Ammo,        // Патроны
    KeyItem,     // Ключевой предмет (ключи, документы)
    Material     // Материал для крафта
}

// Базовый класс для всех предметов
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Base Item")]
public class ItemData : ScriptableObject
{
    [Header("Основная информация")]
    public string itemName = "Новый предмет";
    [TextArea(3, 5)]
    public string description = "Описание предмета";
    public Sprite icon;            // Иконка для UI
    public GameObject worldPrefab; // Префаб в мире (3D модель)

    [Header("Тип и свойства")]
    public ItemType itemType = ItemType.Consumable;
    public bool isStackable = true;   // Можно ли складывать в стопку
    public int maxStackSize = 99;    // Максимальный размер стопки
    public float weight = 0.5f;      // Вес (для ограничения инвентаря)

    [Header("Размер в инвентаре (в слотах)")]
    [Min(1)]
    public int widthInSlots = 1;     // Сколько слотов по горизонтали
    [Min(1)]
    public int heightInSlots = 1;    // Сколько слотов по вертикали

    /// <summary>
    /// Общее "количество слотов", которое занимает предмет (ширина * высота).
    /// Пока это просто вспомогательная инфа для будущей логики.
    /// </summary>
    public int GetSlotVolume()
    {
        return widthInSlots * heightInSlots;
    }

    // Виртуальный метод - каждый тип предмета переопределяет его
    public virtual void Use(GameObject player)
    {
        Debug.Log($"Использован предмет: {itemName}");
    }

    public virtual string GetItemInfo()
    {
        string info = $"{itemName}\n{description}\nВес: {weight} кг";

        // Добавим инфу о размере, чтобы видеть это в тултипах
        info += $"\nРазмер: {widthInSlots}×{heightInSlots} слотов";

        return info;
    }
}
