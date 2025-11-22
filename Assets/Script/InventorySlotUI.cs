using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
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

    // перетаскивание
    private static InventorySlotUI draggedSlot;
    private static GameObject draggedIcon;
    private CanvasGroup canvasGroup;

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public void SetInventory(InventorySystem inv)
    {
        inventory = inv;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void UpdateSlot(InventorySlot slot)
    {
        currentSlot = slot;

        if (slot == null || slot.IsEmpty())
        {
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

        // Многоклеточный предмет, но не корневой слот — показываем только фон
        if (slot.isPartOfComposite && !slot.isRoot)
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = false;
                itemIcon.sprite = null;
            }

            if (quantityText != null)
                quantityText.text = "";

            if (backgroundImage != null)
                backgroundImage.color = isSelected ? selectedColor : normalColor;

            return;
        }

        // Обычный предмет или корень многоклеточного
        if (itemIcon != null)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = slot.item.icon;
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
            backgroundImage.color = isSelected ? selectedColor : normalColor;
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

    // ---------- Drag & Drop (пока только для одноклеточных) ----------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty()) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Для многоклеточных — перетаскивать можно ТОЛЬКО за корневой слот
        if (currentSlot.isPartOfComposite && !currentSlot.isRoot)
        {
            Debug.Log("Перетаскивать многоклеточный предмет можно только за верхний левый слот");
            return;
        }

        draggedSlot = this;

        // Создаём визуальную иконку для перетаскивания
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(transform.root);
        draggedIcon.transform.SetAsLastSibling();

        Image dragImage = draggedIcon.AddComponent<Image>();
        dragImage.sprite = itemIcon != null ? itemIcon.sprite : null;
        dragImage.raycastTarget = false;

        RectTransform dragRect = draggedIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = (transform as RectTransform).sizeDelta;

        if (itemIcon != null)
            itemIcon.enabled = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 0.6f;

        if (quantityText != null)
        {
            var c = quantityText.color;
            c.a = 0.3f;
            quantityText.color = c;
        }
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (draggedSlot != this || draggedIcon == null) return;
        draggedIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedSlot != this) return;

        if (draggedIcon != null)
            Object.Destroy(draggedIcon);

        draggedIcon = null;
        draggedSlot = null;

        if (itemIcon != null && currentSlot != null && !currentSlot.IsEmpty())
            itemIcon.enabled = true;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (quantityText != null)
        {
            var c = quantityText.color;
            c.a = 1f;
            quantityText.color = c;
        }

        if (inventory != null)
        {
            InventorySlot slot = inventory.GetSlot(slotIndex);
            if (slot != null)
                UpdateSlot(slot);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot == null || draggedSlot == this) return;
        if (inventory == null) return;

        // Теперь ВСЁ перетаскивание идёт через MoveItem
        inventory.MoveItem(draggedSlot.slotIndex, slotIndex);
    }

}
