using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.6f, 1f, 0.9f);

    private int slotIndex;
    private InventorySystem inventory;
    private InventorySlot currentSlot;
    private bool isSelected = false;

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public void SetInventory(InventorySystem inv)
    {
        inventory = inv;
    }

    public void UpdateSlot(InventorySlot slot)
    {
        currentSlot = slot;

        if (slot == null || slot.IsEmpty())
        {
            // пустой слот
            if (itemIcon != null)
            {
                itemIcon.enabled = false;
                itemIcon.sprite = null;
            }

            if (quantityText != null)
                quantityText.text = "";

            if (backgroundImage != null)
                backgroundImage.color = emptyColor;

            isSelected = false;
            return;
        }

        // слот с предметом
        if (itemIcon != null)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = slot.item.icon;

            // на всякий случай делаем цвет полностью видимым
            itemIcon.color = Color.white;
        }

        if (quantityText != null)
        {
            if (slot.item.isStackable && slot.quantity > 1)
                quantityText.text = slot.quantity.ToString();
            else
                quantityText.text = "";
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (backgroundImage != null && currentSlot != null && !currentSlot.IsEmpty())
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventory == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            inventory.SelectSlot(slotIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            inventory.DropItem(slotIndex);
        }
    }
}
