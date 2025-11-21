using UnityEngine;

[CreateAssetMenu(fileName = "New Storage Box", menuName = "Inventory/Storage Box Item")]
public class StorageBoxItem : ItemData
{
    [Header("Storage Box Settings")]
    [Tooltip("Prefab ящика для размещения в мире")]
    public GameObject boxPrefab;
    
    [Tooltip("Количество слотов хранения")]
    [Range(1, 50)]
    public int storageSlots = 10;
    
    [Tooltip("Максимальный вес предметов в ящике (0 = без ограничений)")]
    public float maxStorageWeight = 0f;
    
    [Header("Placement Settings")]
    [Tooltip("Максимальная дистанция размещения")]
    public float placementDistance = 3f;
    
    [Tooltip("Высота от земли при размещении")]
    public float placementHeight = 0f;
    
    [Tooltip("Может ли размещаться на стенах")]
    public bool canPlaceOnWalls = false;
    
    [Tooltip("Цвет призрака при валидной позиции")]
    public Color validPlacementColor = new Color(0f, 1f, 0f, 0.5f);
    
    [Tooltip("Цвет призрака при невалидной позиции")]
    public Color invalidPlacementColor = new Color(1f, 0f, 0f, 0.5f);
}
