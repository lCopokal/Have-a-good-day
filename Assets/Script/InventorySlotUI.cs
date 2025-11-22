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

    // Drag & Drop
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

        // Пустой слот
        if (slot == null || slot.IsEmpty())
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = false;
                itemIcon.sprite = null;
                itemIcon.rectTransform.localEulerAngles = Vector3.zero;
            }

            if (quantityText != null)
                quantityText.text = "";

            if (backgroundImage != null)
                backgroundImage.color = emptyColor;

            isSelected = false;
            return;
        }

        // Хвост многоклеточного предмета – без иконки, только фон/подсветка
        if (slot.isPartOfComposite && !slot.isRoot)
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = false;
                itemIcon.sprite = null;
                itemIcon.rectTransform.localEulerAngles = Vector3.zero;
            }

            if (quantityText != null)
                quantityText.text = "";

            if (backgroundImage != null)
                backgroundImage.color = isSelected ? selectedColor : normalColor;

            return;
        }

        // Корень многоклеточного или обычный 1x1
        if (itemIcon != null)
        {
            itemIcon.enabled = true;

            if (slot.item != null)
                itemIcon.sprite = slot.item.icon;

            itemIcon.color = Color.white;

            RectTransform slotRect = transform as RectTransform;
            RectTransform iconRect = itemIcon.rectTransform;

            // размер одной клетки
            Vector2 cellSize = slotRect.rect.size;

            int wSlots = 1;
            int hSlots = 1;
            bool rotated = false;

            // только корень многоклеточного использует width/height и поворот
            if (slot.isRoot && slot.item != null)
            {
                int baseW = Mathf.Max(1, slot.item.widthInSlots);
                int baseH = Mathf.Max(1, slot.item.heightInSlots);

                rotated = slot.rotated;

                // если повернут – ширина/высота меняются местами
                wSlots = rotated ? baseH : baseW;
                hSlots = rotated ? baseW : baseH;
            }

            // Привязка иконки к левому верхнему углу корневого слота
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = Vector2.zero;

            // Размер в слотах (w × h)
            iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cellSize.x * wSlots);
            iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cellSize.y * hSlots);

            // Поворот спрайта (90°), чтобы прямоугольная картинка не растягивалась, а именно крутилась
            iconRect.localEulerAngles = rotated ? new Vector3(0f, 0f, 90f) : Vector3.zero;
        }

        if (quantityText != null)
        {
            if (slot.item != null && slot.item.isStackable && slot.quantity > 1)
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

    // ---------- DRAG & DROP ----------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty()) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        draggedSlot = this;

        // Создаём визуальную иконку для перетаскивания
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(transform.root);
        draggedIcon.transform.SetAsLastSibling();

        Image dragImage = draggedIcon.AddComponent<Image>();

        // Берём иконку напрямую из предмета,
        // чтобы даже при хватании "хвоста" многоклеточного предмета иконка была.
        Sprite sprite = null;
        if (currentSlot != null && currentSlot.item != null)
            sprite = currentSlot.item.icon;
        else if (itemIcon != null)
            sprite = itemIcon.sprite;

        dragImage.sprite = sprite;
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
        }    // Сбрасываем выделение, чтобы старое место не светилось синим
        if (inventory != null)
        {
            inventory.SelectSlot(-1);
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
            Destroy(draggedIcon);

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

        // ВСЕ перемещения — через MoveItem.
        // InventorySystem сам разберётся:
        // - 1×1 → swap/merge
        // - многоклеточный → перенос прямоугольника, если есть место.
        inventory.MoveItem(draggedSlot.slotIndex, slotIndex);
    }
}
