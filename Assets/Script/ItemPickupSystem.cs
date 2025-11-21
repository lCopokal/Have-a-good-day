using UnityEngine;
using TMPro;

public class ItemPickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private Transform holdPosition; // Позиция перед камерой
    [SerializeField] private float holdDistance = 1.5f; // Расстояние от камеры
    [SerializeField] private LayerMask pickupLayer;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float smoothSpeed = 10f; // Плавность движения

    [Header("UI")]
    [SerializeField] private TMP_Text pickupPromptText; // Текст подсказки

    private Camera playerCamera;
    private PickupItem currentItem;
    private PickupItem currentLookAtItem; // Предмет на который смотрим
    private bool isHoldingItem = false;
    private bool isRotating = false;

    // Ссылка на FPS контроллер для отключения при вращении
    private FPSController fpsController;
    private InventorySystem inventory;

    // Для плавного вращения
    private Vector3 lastMousePosition;
    private Quaternion targetRotation;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        fpsController = GetComponent<FPSController>();
        inventory = GetComponent<InventorySystem>();

        // Если нет holdPosition - создаём
        if (holdPosition == null)
        {
            GameObject holdPoint = new GameObject("HoldPosition");
            holdPoint.transform.SetParent(playerCamera.transform);
            holdPoint.transform.localPosition = new Vector3(0, -0.3f, holdDistance);
            holdPosition = holdPoint.transform;
        }

        // Скрываем текст подсказки
        if (pickupPromptText != null)
        {
            pickupPromptText.text = "";
        }
    }

    void Update()
    {
        if (isHoldingItem)
        {
            HandleHeldItem();
        }
        else
        {
            CheckForPickup();
        }
    }

    void CheckForPickup()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance, pickupLayer))
        {
            PickupItem item = hit.collider.GetComponent<PickupItem>();

            if (item != null && !item.IsPickedUp())
            {
                currentLookAtItem = item;

                // Показываем подсказку
                ShowPrompt(item.GetInteractPrompt());

                // E - добавить в инвентарь
                if (Input.GetKeyDown(KeyCode.E))
                {
                    AddToInventory(item);
                }
                // ЛКМ - взять в руки
                else if (Input.GetMouseButtonDown(0))
                {
                    PickupObject(item);
                }
                return;
            }
        }

        // Если ничего не нашли - убираем подсказку
        currentLookAtItem = null;
        HidePrompt();
    }

    void AddToInventory(PickupItem item)
    {
        if (inventory == null)
        {
            Debug.LogWarning("InventorySystem не найдена!");
            return;
        }

        ItemData itemData = item.GetItemData();
        if (itemData == null)
        {
            Debug.LogWarning("У предмета нет ItemData!");
            return;
        }

        // Добавляем в инвентарь
        bool added = inventory.AddItem(itemData, item.GetQuantity());

        if (added)
        {
            Debug.Log($"Добавлено в инвентарь: {itemData.itemName} x{item.GetQuantity()}");

            // Удаляем предмет из мира
            Destroy(item.gameObject);

            HidePrompt();
        }
    }

    void PickupObject(PickupItem item)
    {
        currentItem = item;
        isHoldingItem = true;

        // Вызываем метод подбора
        currentItem.OnPickup();

        // Устанавливаем начальную ротацию
        targetRotation = currentItem.transform.rotation;

        Debug.Log($"Подобран предмет: {currentItem.GetItemName()}");
    }

    void HandleHeldItem()
    {
        if (currentItem == null)
        {
            isHoldingItem = false;
            HidePrompt();
            return;
        }

        // Показываем подсказку "Выбросить"
        ShowPrompt("[ЛКМ] Выбросить | [ПКМ] Вращать");

        // Левая кнопка мыши для отпускания - ПРОВЕРЯЕМ В ПЕРВУЮ ОЧЕРЕДЬ
        if (Input.GetMouseButtonDown(0))
        {
            DropItem();
            return; // Выходим из метода после отпускания
        }

        // Плавно перемещаем предмет к позиции удержания
        currentItem.transform.position = Vector3.Lerp(
            currentItem.transform.position,
            holdPosition.position,
            Time.deltaTime * smoothSpeed
        );

        // Правая кнопка мыши для вращения
        if (Input.GetMouseButton(1))
        {
            if (!isRotating)
            {
                isRotating = true;
                lastMousePosition = Input.mousePosition;

                // Отключаем FPS контроллер
                if (fpsController != null)
                {
                    fpsController.enabled = false;
                }
            }

            RotateItem();
        }
        else
        {
            if (isRotating)
            {
                isRotating = false;

                // Включаем FPS контроллер обратно
                if (fpsController != null)
                {
                    fpsController.enabled = true;
                }
            }

            // Плавно поворачиваем к целевой ротации
            currentItem.transform.rotation = Quaternion.Slerp(
                currentItem.transform.rotation,
                targetRotation,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    void RotateItem()
    {
        // Получаем движение мыши
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // Вращаем предмет относительно камеры
        currentItem.transform.Rotate(playerCamera.transform.up, -mouseX, Space.World);
        currentItem.transform.Rotate(playerCamera.transform.right, mouseY, Space.World);

        // Обновляем целевую ротацию
        targetRotation = currentItem.transform.rotation;
    }

    void DropItem()
    {
        if (currentItem == null) return;

        Debug.Log($"Отпущен предмет: {currentItem.GetItemName()}");

        // Включаем FPS контроллер если он был выключен
        if (fpsController != null)
        {
            fpsController.enabled = true;
        }

        // Вызываем метод отпускания
        currentItem.OnDrop();

        currentItem = null;
        isHoldingItem = false;
        isRotating = false;

        // Убираем подсказку
        HidePrompt();
    }

    void ShowPrompt(string text)
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.text = text;
        }
    }

    void HidePrompt()
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.text = "";
        }
    }

    // Публичные методы для других систем
    public PickupItem GetHeldItem()
    {
        return currentItem;
    }

    public void DropHeldItem()
    {
        if (currentItem != null)
        {
            DropItem();
        }
    }

    public bool IsHoldingItem()
    {
        return isHoldingItem;
    }

    // Визуализация дистанции подбора в редакторе
    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = isHoldingItem ? Color.green : Color.cyan;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * pickupDistance);

            if (holdPosition != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(holdPosition.position, 0.1f);
            }
        }
    }
}