using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StorageBoxUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject storagePanel;
    [SerializeField] private Transform storageSlotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI storageInfoText;
    [SerializeField] private TextMeshProUGUI playerInventoryInfoText;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    private PlaceableStorageBox currentStorageBox;
    private GameObject currentPlayer;
    private InventorySystem playerInventory;

    private List<InventorySlotUI> storageSlotUIs = new List<InventorySlotUI>();

    private ItemData selectedItem;
    private int selectedQuantity;
    private bool isFromStorage;
    private int selectedSlotIndex;

    // Флаги для отложенных операций
    private bool pendingStorageOpen = false;
    private PlaceableStorageBox pendingStorageBox;
    private GameObject pendingPlayer;

    void Start()
    {
        if (storagePanel != null)
            storagePanel.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseStorage);
        }
    }

    void Update()
    {
        // Обрабатываем отложенное открытие хранилища
        if (pendingStorageOpen)
        {
            OpenStorageInternal(pendingStorageBox, pendingPlayer);
            pendingStorageOpen = false;
            pendingStorageBox = null;
            pendingPlayer = null;
        }

        if (storagePanel != null && storagePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseStorage();
            }

            UpdateStorageInfo();
        }
    }

    public void OpenStorage(PlaceableStorageBox storageBox, GameObject player)
    {
        // Откладываем открытие до следующего Update
        pendingStorageOpen = true;
        pendingStorageBox = storageBox;
        pendingPlayer = player;
    }

    private void OpenStorageInternal(PlaceableStorageBox storageBox, GameObject player)
    {
        currentStorageBox = storageBox;
        currentPlayer = player;
        playerInventory = player.GetComponent<InventorySystem>();

        if (playerInventory == null)
        {
            Debug.LogError("У игрока нет InventorySystem!");
            return;
        }

        if (storagePanel != null)
            storagePanel.SetActive(true);

        CreateStorageSlots();

        FPSController fpsController = player.GetComponent<FPSController>();
        if (fpsController != null)
        {
            fpsController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateStorageInfo();
    }

    public void CloseStorage()
    {
        if (storagePanel != null)
            storagePanel.SetActive(false);

        if (selectedItem != null)
        {
            ReturnSelectedItem();
        }

        if (currentPlayer != null)
        {
            FPSController fpsController = currentPlayer.GetComponent<FPSController>();
            if (fpsController != null)
            {
                fpsController.enabled = true;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Очищаем слоты после закрытия
        StartCoroutine(ClearSlotsDelayed());

        currentStorageBox = null;
        currentPlayer = null;
        playerInventory = null;
    }

    private System.Collections.IEnumerator ClearSlotsDelayed()
    {
        yield return null; // Ждём один кадр

        foreach (Transform child in storageSlotContainer)
        {
            Destroy(child.gameObject);
        }
        storageSlotUIs.Clear();
    }

    private void CreateStorageSlots()
    {
        // Очищаем старые слоты
        foreach (Transform child in storageSlotContainer)
        {
            Destroy(child.gameObject);
        }
        storageSlotUIs.Clear();

        if (currentStorageBox == null) return;

        List<InventorySlot> storageSlots = currentStorageBox.GetStorageSlots();
        for (int i = 0; i < storageSlots.Count; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, storageSlotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                int index = i;

                slotUI.UpdateSlot(storageSlots[i]);

                Button slotButton = slotObj.GetComponent<Button>();
                if (slotButton != null)
                {
                    slotButton.onClick.AddListener(() => OnStorageSlotClick(index));
                }

                storageSlotUIs.Add(slotUI);
            }
        }
    }

    private void OnStorageSlotClick(int slotIndex)
    {
        if (currentStorageBox == null) return;

        List<InventorySlot> storageSlots = currentStorageBox.GetStorageSlots();
        if (slotIndex < 0 || slotIndex >= storageSlots.Count) return;

        InventorySlot slot = storageSlots[slotIndex];

        if (selectedItem == null)
        {
            if (slot.item != null)
            {
                selectedItem = slot.item;
                selectedQuantity = slot.quantity;
                isFromStorage = true;
                selectedSlotIndex = slotIndex;

                slot.item = null;
                slot.quantity = 0;

                RefreshSlots();
                Debug.Log($"Выбран предмет из ящика: {selectedItem.itemName} x{selectedQuantity}");
            }
        }
        else
        {
            if (slot.item == null)
            {
                slot.item = selectedItem;
                slot.quantity = selectedQuantity;
                selectedItem = null;
                selectedQuantity = 0;
            }
            else if (slot.item == selectedItem && selectedItem.isStackable)
            {
                int spaceLeft = selectedItem.maxStackSize - slot.quantity;
                int amountToAdd = Mathf.Min(selectedQuantity, spaceLeft);

                slot.quantity += amountToAdd;
                selectedQuantity -= amountToAdd;

                if (selectedQuantity <= 0)
                {
                    selectedItem = null;
                    selectedQuantity = 0;
                }
            }
            else
            {
                ItemData tempItem = slot.item;
                int tempQuantity = slot.quantity;

                slot.item = selectedItem;
                slot.quantity = selectedQuantity;

                selectedItem = tempItem;
                selectedQuantity = tempQuantity;
                isFromStorage = true;
                selectedSlotIndex = slotIndex;
            }

            RefreshSlots();
        }
    }

    private void ReturnSelectedItem()
    {
        if (selectedItem == null) return;

        if (isFromStorage && currentStorageBox != null)
        {
            currentStorageBox.AddItem(selectedItem, selectedQuantity);
        }
        else if (!isFromStorage && playerInventory != null)
        {
            playerInventory.AddItem(selectedItem, selectedQuantity);
        }

        selectedItem = null;
        selectedQuantity = 0;
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        if (currentStorageBox != null)
        {
            List<InventorySlot> storageSlots = currentStorageBox.GetStorageSlots();
            for (int i = 0; i < storageSlotUIs.Count && i < storageSlots.Count; i++)
            {
                storageSlotUIs[i].UpdateSlot(storageSlots[i]);
            }
        }
    }

    private void UpdateStorageInfo()
    {
        if (currentStorageBox != null && storageInfoText != null)
        {
            float currentWeight = currentStorageBox.GetCurrentWeight();
            float maxWeight = currentStorageBox.GetMaxStorageWeight();
            int usedSlots = 0;

            foreach (var slot in currentStorageBox.GetStorageSlots())
            {
                if (slot.item != null) usedSlots++;
            }

            string weightInfo = maxWeight > 0 ? $" | {currentWeight:F1}/{maxWeight:F1} кг" : "";
            storageInfoText.text = $"Ящик: {usedSlots}/{currentStorageBox.GetStorageCapacity()} слотов{weightInfo}";
        }

        if (playerInventory != null && playerInventoryInfoText != null)
        {
            playerInventoryInfoText.text = "Инвентарь игрока";
        }
    }

    private void ClearAllSlots()
    {
        storageSlotUIs.Clear();
        selectedItem = null;
        selectedQuantity = 0;
    }
}