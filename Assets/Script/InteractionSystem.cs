using UnityEngine;
using TMPro;

public interface IInteractable
{
    string GetInteractPrompt();
    void Interact();
}

public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI")]
    [SerializeField] private GameObject interactionPromptUI;
    [SerializeField] private TMP_Text promptText;

    [Header("Chest Pickup")]
    [SerializeField] private float chestPickupHoldTime = 1.0f; // сколько держать E чтобы поднять сундук

    private Camera playerCamera;
    private IInteractable currentInteractable;
    private InventorySystem inventory;

    // для удержания E на сундуке
    private float holdTimer = 0f;
    private bool chestPickupTriggered = false;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<InventorySystem>();

        if (promptText != null)
            promptText.text = "";
    }

    void Update()
    {
        CheckForInteractable();
        HandleInteractionInput();
    }

    void HandleInteractionInput()
    {
        if (currentInteractable == null)
        {
            holdTimer = 0f;
            chestPickupTriggered = false;
            return;
        }

        ChestInventory chest = currentInteractable as ChestInventory;

        // особый режим для сундуков
        if (chest != null && chest.CanBePickedUp && inventory != null)
        {
            if (Input.GetKey(KeyCode.E))
            {
                holdTimer += Time.deltaTime;

                if (!chestPickupTriggered && holdTimer >= chestPickupHoldTime)
                {
                    chestPickupTriggered = true;
                    chest.PickupChest(inventory);
                    // сундук мог уничтожиться — сбрасываем ссылку
                    SetCurrentInteractable(null);
                }
            }

            if (Input.GetKeyUp(KeyCode.E))
            {
                if (!chestPickupTriggered)
                {
                    // короткое нажатие — открыть сундук
                    chest.Interact();
                }

                holdTimer = 0f;
                chestPickupTriggered = false;
            }
        }
        else
        {
            // обычные объекты (двери и т.п.) — по нажатию E
            holdTimer = 0f;
            chestPickupTriggered = false;

            if (Input.GetKeyDown(KeyCode.E))
            {
                currentInteractable.Interact();
            }
        }
    }

    void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                SetCurrentInteractable(interactable);
                return;
            }
        }

        SetCurrentInteractable(null);
    }

    void SetCurrentInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;

        if (promptText != null)
        {
            if (currentInteractable != null)
                promptText.text = currentInteractable.GetInteractPrompt();
            else
                promptText.text = "";
        }
    }

    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance);
        }
    }
}
