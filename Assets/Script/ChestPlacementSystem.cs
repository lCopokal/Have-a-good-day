using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class ChestPlacementSystem : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private LayerMask placementMask;
    [SerializeField] private float maxPlacementDistance = 5f;

    [Header("Ghost Settings")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    [SerializeField] private float rotationStep = 15f; // градусов за одно движение колЄсика

    private Camera playerCamera;
    private InventorySystem inventory;

    private ChestItemData activeChestItem;
    private GameObject ghostInstance;
    private bool isPlacing = false;
    private bool canPlaceHere = false;
    private float rotationY = 0f;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<InventorySystem>();
    }

    void Update()
    {
        if (!isPlacing) return;

        UpdateGhostPosition();
        HandlePlacementInput();
    }

    public void BeginPlacement(ChestItemData chestItem)
    {
        if (isPlacing) return;
        if (chestItem == null || chestItem.worldPrefab == null)
        {
            Debug.LogWarning("ChestPlacementSystem: ChestItemData или worldPrefab не назначены");
            return;
        }

        activeChestItem = chestItem;
        isPlacing = true;
        rotationY = 0f;

        // закрываем инвентарь, чтобы включилось управление камерой
        if (InventorySystem.IsOpen)
            inventory.ToggleInventory();

        // создаЄм призрак сундука
        ghostInstance = Instantiate(chestItem.worldPrefab);
        SetGhostMode(true);
    }

    void UpdateGhostPosition()
    {
        if (playerCamera == null || ghostInstance == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPlacementDistance, placementMask))
        {
            Vector3 hitPoint = hit.point;
            Vector3 normal = hit.normal;

            // подстраиваем позицию чуть над поверхностью
            ghostInstance.transform.position = hitPoint + normal * 0.01f;

            // вращение вокруг вертикальной оси
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = transform.forward;

            Quaternion baseRot = Quaternion.LookRotation(forward, Vector3.up);
            ghostInstance.transform.rotation = baseRot * Quaternion.Euler(0f, rotationY, 0f);

            // проверка Ч можно ли ставить на эту поверхность
            canPlaceHere = IsValidSurface(normal, activeChestItem.canAttachToWall);
        }
        else
        {
            canPlaceHere = false;
        }

        ApplyGhostMaterial();
    }

    bool IsValidSurface(Vector3 normal, bool canAttachToWall)
    {
        // если нельз€ к стенам Ч требуем почти верхнюю поверхность
        float dotUp = Vector3.Dot(normal.normalized, Vector3.up);
        if (!canAttachToWall)
        {
            return dotUp > 0.7f; // ~ до 45∞ наклона
        }
        else
        {
            // к стене или полу Ч можно
            return true;
        }
    }

    void HandlePlacementInput()
    {
        // крутить сундук колЄсиком мышки
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            rotationY += scroll * rotationStep;
        }

        // Ћ ћ Ч поставить
        if (Input.GetMouseButtonDown(0) && canPlaceHere)
        {
            PlaceChest();
        }

        // ѕ ћ или Escape Ч отмена
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    void PlaceChest()
    {
        if (ghostInstance == null || activeChestItem == null) return;

        // создаЄм реальный сундук
        Instantiate(activeChestItem.worldPrefab,
                    ghostInstance.transform.position,
                    ghostInstance.transform.rotation);

        // убираем один сундук из инвентар€ (по весу/стекам занимаетс€ InventorySystem)
        inventory.RemoveItem(activeChestItem, 1);

        EndPlacement();
    }

    void CancelPlacement()
    {
        EndPlacement();
    }

    void EndPlacement()
    {
        if (ghostInstance != null)
            Destroy(ghostInstance);

        ghostInstance = null;
        activeChestItem = null;
        isPlacing = false;
        canPlaceHere = false;
    }

    void SetGhostMode(bool enable)
    {
        if (ghostInstance == null) return;

        // отключаем физику
        foreach (Collider col in ghostInstance.GetComponentsInChildren<Collider>())
            col.enabled = !enable; // в режиме призрака Ч выключены

        // делаем прозрачным материал
        ApplyGhostMaterial();
    }

    void ApplyGhostMaterial()
    {
        if (ghostInstance == null) return;

        Material mat = canPlaceHere ? validMaterial : invalidMaterial;
        if (mat == null) return;

        foreach (Renderer r in ghostInstance.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterial = mat;
        }
    }
}
