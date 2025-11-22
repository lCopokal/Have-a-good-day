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
    [TextArea(2, 4)]
    public string description = "Описание предмета";

    [Header("Визуал")]
    public Sprite icon;             // иконка в инвентаре
    public GameObject worldPrefab;  // префаб в мире

    [Header("Тип и свойства")]
    public ItemType itemType = ItemType.Consumable;
    public bool isStackable = true;   // можно ли складывать в стопку
    public int maxStackSize = 99;     // максимальный размер стопки
    public float weight = 0.5f;       // вес

    [Header("Размер в инвентаре (в слотах)")]
    [Tooltip("Ширина предмета в слотах (например, 1, 2, 3...)")]
    public int widthInSlots = 1;

    [Tooltip("Высота предмета в слотах (например, 1, 2, 3...)")]
    public int heightInSlots = 1;

    // Виртуальные методы — переопределяются в дочерних классах
    public virtual void Use(GameObject player)
    {
        Debug.Log($"Использован предмет: {itemName}");
    }

    public virtual string GetItemInfo()
    {
        return $"{itemName}\n{description}\nВес: {weight} кг";
    }
}
