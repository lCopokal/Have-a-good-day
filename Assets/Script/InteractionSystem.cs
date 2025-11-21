using UnityEngine;
using TMPro; // Добавляем поддержку TextMeshPro

// Интерфейс для всех интерактивных объектов
public interface IInteractable
{
    string GetInteractPrompt(); // Текст подсказки ("Нажмите E чтобы открыть")
    void Interact(); // Действие при взаимодействии
}

// Система взаимодействия для игрока
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI")]
    [SerializeField] private GameObject interactionPromptUI;
    [SerializeField] private TMP_Text promptText; // Изменено на TextMeshPro

    private Camera playerCamera;
    private IInteractable currentInteractable;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();

        // Оставляем UI включённым, но делаем текст пустым
        if (promptText != null)
        {
            promptText.text = "";
        }
    }

    void Update()
    {
        CheckForInteractable();

        // Нажатие E для взаимодействия
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Проверяем луч от камеры
        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            // Ищем IInteractable на объекте или его родителях
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                SetCurrentInteractable(interactable);
                return;
            }
        }

        // Если ничего не нашли - убираем текущий объект
        SetCurrentInteractable(null);
    }

    void SetCurrentInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;

        if (promptText != null)
        {
            if (currentInteractable != null)
            {
                promptText.text = currentInteractable.GetInteractPrompt();
            }
            else
            {
                promptText.text = ""; // Очищаем текст вместо выключения UI
            }
        }
    }

    // Визуализация луча взаимодействия в редакторе
    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance);
        }
    }
}