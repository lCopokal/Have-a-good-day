using UnityEngine;

[CreateAssetMenu(fileName = "New Chest Item", menuName = "Inventory/Chest Item")]
public class ChestItemData : ItemData
{
    [Header("Сундук")]
    [Tooltip("Сколько слотов у сундука (для UI)")]
    public int chestSlots = 12;

    [Tooltip("Можно ли крепить сундук к стене (для размещения)")]
    public bool canAttachToWall = false;
}
