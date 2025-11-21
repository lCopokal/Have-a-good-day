using UnityEngine;

public class BoxPlacementSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask placementLayerMask;

    [Header("Placement Settings")]
    [SerializeField] private float rotationSpeed = 90f;

    private GameObject ghostBox;
    private StorageBoxItem currentBoxItem;
    private bool isPlacementMode = false;
    private float currentRotation = 0f;

    private Material ghostMaterial;
    private bool isValidPlacement = false;

    private InventorySystem inventorySystem;

    // Флаг для отложенного создания/удаления
    private bool pendingGhostCreation = false;
    private bool pendingGhostDestruction = false;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        inventorySystem = GetComponent<InventorySystem>();

        CreateGhostMaterial();
    }

    void Update()
    {
        // Обрабатываем отложенные операции в Update (безопасно)
        if (pendingGhostCreation)
        {
            CreateGhostInternal();
            pendingGhostCreation = false;
        }

        if (pendingGhostDestruction)
        {
            DestroyGhostInternal();
            pendingGhostDestruction = false;
        }

        if (isPlacementMode)
        {
            UpdateGhostPosition();
            HandlePlacementInput();
        }
    }

    private void CreateGhostMaterial()
    {
        ghostMaterial = new Material(Shader.Find("Standard"));
        ghostMaterial.SetFloat("_Mode", 3);
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.DisableKeyword("_ALPHATEST_ON");
        ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
        ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMaterial.renderQueue = 3000;
    }

    public void EnterPlacementMode(StorageBoxItem boxItem)
    {
        if (boxItem == null || boxItem.boxPrefab == null)
        {
            Debug.LogError("Неверный предмет ящика или отсутствует prefab!");
            return;
        }

        currentBoxItem = boxItem;
        isPlacementMode = true;
        currentRotation = 0f;

        // Запланировать создание призрака в следующем Update
        pendingGhostCreation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Режим размещения активирован. ЛКМ - поставить, ПКМ - отменить, Q/E - вращать");
    }

    private void CreateGhost()
    {
        // Эта функция вызывается извне - помечаем для отложенного создания
        pendingGhostCreation = true;
    }

    private void CreateGhostInternal()
    {
        // Удаляем старый призрак если есть
        if (ghostBox != null)
        {
            Destroy(ghostBox);
        }

        ghostBox = Instantiate(currentBoxItem.boxPrefab);

        // Отключаем коллайдеры и скрипты у призрака
        Collider[] colliders = ghostBox.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        MonoBehaviour[] scripts = ghostBox.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            script.enabled = false;
        }

        // Применяем призрачный материал
        Renderer[] renderers = ghostBox.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Material[] newMats = new Material[renderer.materials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = ghostMaterial;
            }
            renderer.materials = newMats;
        }

        ghostBox.SetActive(false);
    }

    private void UpdateGhostPosition()
    {
        if (ghostBox == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, currentBoxItem.placementDistance, placementLayerMask))
        {
            bool canPlaceHere = true;

            if (!currentBoxItem.canPlaceOnWalls)
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle > 30f)
                {
                    canPlaceHere = false;
                }
            }

            if (canPlaceHere)
            {
                Bounds ghostBounds = GetGhostBounds();
                Collider[] overlaps = Physics.OverlapBox(
                    hit.point + Vector3.up * ghostBounds.extents.y,
                    ghostBounds.extents,
                    Quaternion.Euler(0, currentRotation, 0),
                    placementLayerMask
                );

                if (overlaps.Length > 0)
                {
                    canPlaceHere = false;
                }
            }

            isValidPlacement = canPlaceHere;

            Vector3 placementPos = hit.point + Vector3.up * currentBoxItem.placementHeight;
            ghostBox.transform.position = placementPos;
            ghostBox.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

            Color targetColor = isValidPlacement ?
                currentBoxItem.validPlacementColor :
                currentBoxItem.invalidPlacementColor;

            ghostMaterial.color = targetColor;

            if (!ghostBox.activeSelf)
                ghostBox.SetActive(true);
        }
        else
        {
            if (ghostBox.activeSelf)
                ghostBox.SetActive(false);
            isValidPlacement = false;
        }
    }

    private Bounds GetGhostBounds()
    {
        Bounds bounds = new Bounds(ghostBox.transform.position, Vector3.zero);
        Renderer[] renderers = ghostBox.GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private void HandlePlacementInput()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            currentRotation -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            currentRotation += rotationSpeed * Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (isValidPlacement)
            {
                PlaceBox();
            }
            else
            {
                Debug.Log("Невозможно разместить ящик здесь!");
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    private void PlaceBox()
    {
        if (ghostBox == null) return;

        GameObject realBox = Instantiate(
            currentBoxItem.boxPrefab,
            ghostBox.transform.position,
            ghostBox.transform.rotation
        );

        PlaceableStorageBox storageBox = realBox.GetComponent<PlaceableStorageBox>();
        if (storageBox != null)
        {
            storageBox.SetupBox(currentBoxItem.storageSlots, currentBoxItem.maxStorageWeight);
        }
        else
        {
            Debug.LogError("На префабе ящика отсутствует компонент PlaceableStorageBox!");
        }

        if (inventorySystem != null)
        {
            inventorySystem.RemoveItem(currentBoxItem, 1);
        }

        Debug.Log($"Ящик размещён: {currentBoxItem.itemName}");

        ExitPlacementMode();
    }

    private void CancelPlacement()
    {
        Debug.Log("Размещение отменено");
        ExitPlacementMode();
    }

    private void ExitPlacementMode()
    {
        isPlacementMode = false;

        // Запланировать удаление призрака в следующем Update
        if (ghostBox != null)
        {
            pendingGhostDestruction = true;
        }

        currentBoxItem = null;
    }

    private void DestroyGhostInternal()
    {
        if (ghostBox != null)
        {
            Destroy(ghostBox);
            ghostBox = null;
        }
    }

    public bool IsInPlacementMode()
    {
        return isPlacementMode;
    }

    void OnDestroy()
    {
        if (ghostMaterial != null)
        {
            Destroy(ghostMaterial);
        }

        if (ghostBox != null)
        {
            Destroy(ghostBox);
        }
    }
}