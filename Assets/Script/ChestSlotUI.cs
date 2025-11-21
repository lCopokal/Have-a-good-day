using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ChestSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

    private ChestUIManager manager;
    private int slotIndex;
    private InventorySlot slot;

    public void Setup(ChestUIManager manager, int index)
    {
        this.manager = manager;
        this.slotIndex = index;
    }

    public void UpdateSlot(InventorySlot slot)
    {
        this.slot = slot;

        if (slot == null || slot.IsEmpty())
        {
            if (itemIcon != null) itemIcon.enabled = false;
            if (quantityText != null) quantityText.text = "";
            if (backgroundImage != null) backgroundImage.color = emptyColor;
        }
        else
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = true;
                itemIcon.sprite = slot.item.icon;
            }

            if (quantityText != null)
            {
                quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
            }

            if (backgroundImage != null)
                backgroundImage.color = normalColor;
        }
    }

    // Пока просто заглушка — позже сюда можно добавить перенос предметов
    public void OnPointerClick(PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty()) return;

        // Пример на будущее:
        // ЛКМ — забрать предмет из сундука в инвентарь игрока
        // var chest = manager.GetCurrentChest();
        // var inv = manager.GetCurrentInventory();
    }
}
