using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private float openAngle = 90f; // Угол открытия
    [SerializeField] private float openSpeed = 2f; // Скорость открытия
    [SerializeField] private bool isLocked = false; // Заблокирована ли дверь

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private AudioSource audioSource;
    private bool isAnimating = false;

    void Start()
    {
        // Сохраняем начальное положение (закрытое)
        closedRotation = transform.rotation;

        // Вычисляем открытое положение
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);

        // Добавляем AudioSource если его нет
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Плавная анимация открытия/закрытия
        if (isAnimating)
        {
            Quaternion targetRotation = isOpen ? openRotation : closedRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);

            // Проверяем, достигли ли цели
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation;
                isAnimating = false;
            }
        }
    }

    public string GetInteractPrompt()
    {
        if (isLocked)
        {
            return "[E] Заблокировано";
        }
        return isOpen ? "[E] Закрыть дверь" : "[E] Открыть дверь";
    }

    public void Interact()
    {
        if (isLocked)
        {
            // Дверь заблокирована
            PlaySound(lockedSound);
            Debug.Log("Дверь заблокирована! Нужен ключ.");
            return;
        }

        // Переключаем состояние
        isOpen = !isOpen;
        isAnimating = true;

        // Воспроизводим звук
        PlaySound(isOpen ? openSound : closeSound);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Публичные методы для управления дверью
    public void Unlock()
    {
        isLocked = false;
        Debug.Log("Дверь разблокирована!");
    }

    public void Lock()
    {
        isLocked = true;
        // Закрываем дверь при блокировке
        if (isOpen)
        {
            isOpen = false;
            isAnimating = true;
        }
    }

    public bool IsLocked()
    {
        return isLocked;
    }
}