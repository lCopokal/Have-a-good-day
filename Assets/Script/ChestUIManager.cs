using UnityEngine;
using System.Collections.Generic;

public class ChestUIManager : MonoBehaviour
{
    public static ChestUIManager Instance { get; private set; }

    [Header("UI сундука")]
    [SerializeField] private GameObject chestUIRoot;      // панель сундука
    [SerializeField] private Transform chestSlotsParent;  // контейнер для слотов сундука
    [SerializeField] private GameObject chestSlotPrefab;  // префаб одного слота сундука

    private ChestInventory currentChest;
    private InventorySystem currentInventory;

    private readonly List<ChestSlotUI> chestSlotsUI = new List<ChestSlotUI>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (chestUIRoot != null)
            chestUIRoot.SetActive(false);
    }

    public void OpenChest(ChestInventory chest, InventorySystem inventory)
    {
        if (chest == null || inventory == null) return;

        currentChest = chest;
        currentInventory = inventory;

        if (chestUIRoot != null)
            chestUIRoot.SetActive(true);

        CreateSlotsIfNeeded();
        RefreshChestUI();

        // гарантируем, что инвентарь игрока тоже открыт
        if (!inventory.IsInventoryOpenInstance())
            inventory.ToggleInventory();
    }

    public void CloseChest()
    {
        currentChest = null;
        currentInventory = null;

        if (chestUIRoot != null)
            chestUIRoot.SetActive(false);
    }

    void CreateSlotsIfNeeded()
    {
        if (currentChest == null || chestSlotsParent == null || chestSlotPrefab == null)
            return;

        int needed = currentChest.GetSize();

        // если уже создано — ничего не делаем
        if (chestSlotsUI.Count == needed) return;

        // очищаем старые
        foreach (Transform child in chestSlotsParent)
            Destroy(child.gameObject);
        chestSlotsUI.Clear();

        // создаём новые
        for (int i = 0; i < needed; i++)
        {
            GameObject slotObj = Instantiate(chestSlotPrefab, chestSlotsParent);
            ChestSlotUI ui = slotObj.GetComponent<ChestSlotUI>();
            if (ui != null)
            {
                ui.Setup(this, i);
                chestSlotsUI.Add(ui);
            }
        }
    }

    public void RefreshChestUI()
    {
        if (currentChest == null) return;

        for (int i = 0; i < chestSlotsUI.Count; i++)
        {
            chestSlotsUI[i].UpdateSlot(currentChest.GetSlot(i));
        }
    }

    public ChestInventory GetCurrentChest() => currentChest;
    public InventorySystem GetCurrentInventory() => currentInventory;
}
