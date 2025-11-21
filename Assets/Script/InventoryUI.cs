using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.5f, 0.3f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.6f, 1f, 0.9f);

    private int slotIndex;
    private InventorySystem inventory;
    private InventorySlot currentSlot;
    private bool isSelected = false;

    // Для перетаскивания
    private static GameObject draggedItem;
    private static InventorySlotUI draggedSlot;
    private GameObject draggedIcon; // Иконка которая следует за курсором
    private CanvasGroup canvasGroup;

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public void SetInventory(InventorySystem inv)
    {
        inventory = inv;
    }

    void Awake()
    {
        // Добавляем CanvasGroup если его нет (для прозрачности при перетаскивании)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void UpdateSlot(InventorySlot slot)
    {
        currentSlot = slot;

        if (slot.IsEmpty())
        {
            // Пустой слот
            if (itemIcon != null)
            {
                itemIcon.enabled = false;
            }

            if (quantityText != null)
            {
                quantityText.text = "";
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = emptyColor;
            }

            isSelected = false;
        }
        else
        {
            // Слот с предметом
            if (itemIcon != null)
            {
                itemIcon.enabled = true;
                itemIcon.sprite = slot.item.icon;
            }

            if (quantityText != null)
            {
                // Показываем количество только если > 1
                quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
            }

            if (backgroundImage != null)
            {
                // Показываем выделение если слот выбран
                backgroundImage.color = isSelected ? selectedColor : normalColor;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (backgroundImage != null && !currentSlot.IsEmpty())
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty()) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // ЛКМ - выбрать слот
            if (inventory != null)
            {
                inventory.SelectSlot(slotIndex);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // ПКМ - выбросить предмет
            inventory.DropItem(slotIndex);
        }
    }

    // Начало перетаскивания
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty()) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        draggedItem = gameObject;
        draggedSlot = this;

        // Создаём временную иконку для перетаскивания
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(transform.root); // Родитель - Canvas
        draggedIcon.transform.SetAsLastSibling(); // Поверх всего

        Image dragImage = draggedIcon.AddComponent<Image>();
        dragImage.sprite = itemIcon.sprite;
        dragImage.raycastTarget = false;

        RectTransform dragRect = draggedIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = new Vector2(80, 80); // Размер иконки

        // СКРЫВАЕМ оригинальную иконку в слоте
        if (itemIcon != null)
        {
            itemIcon.enabled = false;
        }

        // Делаем текст количества полупрозрачным
        if (quantityText != null)
        {
            Color textColor = quantityText.color;
            textColor.a = 0.3f;
            quantityText.color = textColor;
        }

        // Делаем фон слота полупрозрачным
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
        }
    }

    // Во время перетаскивания
    public void OnDrag(PointerEventData eventData)
    {
        if (draggedItem != gameObject || draggedIcon == null) return;

        // Иконка следует за курсором
        draggedIcon.transform.position = eventData.position;
    }

    // Конец перетаскивания
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedItem != gameObject) return;

        // Удаляем временную иконку
        if (draggedIcon != null)
        {
            Destroy(draggedIcon);
        }

        // Возвращаем прозрачность текста
        if (quantityText != null)
        {
            Color textColor = quantityText.color;
            textColor.a = 1f;
            quantityText.color = textColor;
        }

        // Возвращаем прозрачность слота
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        draggedItem = null;
        draggedSlot = null;

        // ВАЖНО: Обновляем UI слота чтобы показать правильное содержимое
        // после обмена (если обмен произошёл в OnDrop)
        if (inventory != null)
        {
            InventorySlot slot = inventory.GetSlot(slotIndex);
            if (slot != null)
            {
                UpdateSlot(slot);
            }
        }
    }

    // Когда предмет "бросают" на этот слот
    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot == null || draggedSlot == this) return;

        // Меняем предметы местами
        if (inventory != null)
        {
            inventory.SwapSlots(draggedSlot.slotIndex, this.slotIndex);
        }
    }
}