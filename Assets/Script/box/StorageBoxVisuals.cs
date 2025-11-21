using UnityEngine;

// ОПЦИОНАЛЬНЫЙ скрипт для улучшения визуала ящиков
// Добавь его на префаб ящика для подсветки при наведении

public class StorageBoxVisuals : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    [SerializeField] private float highlightIntensity = 1.5f;
    [SerializeField] private float highlightSpeed = 5f;
    
    [Header("Animation")]
    [SerializeField] private GameObject lidObject; // Крышка ящика (если есть)
    [SerializeField] private float lidOpenAngle = 90f;
    [SerializeField] private float lidAnimationSpeed = 3f;
    
    [Header("Particles")]
    [SerializeField] private ParticleSystem placeEffect;
    
    private Renderer[] renderers;
    private Color[] originalColors;
    private Color[] originalEmissionColors;
    private bool isHighlighted = false;
    private bool isOpen = false;
    private Quaternion lidClosedRotation;
    private Quaternion lidOpenRotation;
    
    void Start()
    {
        // Получаем все рендереры
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        originalEmissionColors = new Color[renderers.Length];
        
        // Сохраняем оригинальные цвета
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
            
            if (renderers[i].material.HasProperty("_EmissionColor"))
            {
                originalEmissionColors[i] = renderers[i].material.GetColor("_EmissionColor");
            }
        }
        
        // Настраиваем крышку
        if (lidObject != null)
        {
            lidClosedRotation = lidObject.transform.localRotation;
            lidOpenRotation = Quaternion.Euler(lidOpenAngle, 0, 0) * lidClosedRotation;
        }
    }
    
    void Update()
    {
        // Анимация подсветки
        if (isHighlighted)
        {
            float pulse = Mathf.PingPong(Time.time * highlightSpeed, 1f);
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material.HasProperty("_EmissionColor"))
                {
                    Color emissionColor = highlightColor * (highlightIntensity + pulse * 0.5f);
                    renderers[i].material.SetColor("_EmissionColor", emissionColor);
                    renderers[i].material.EnableKeyword("_EMISSION");
                }
            }
        }
        
        // Анимация крышки
        if (lidObject != null)
        {
            Quaternion targetRotation = isOpen ? lidOpenRotation : lidClosedRotation;
            lidObject.transform.localRotation = Quaternion.Lerp(
                lidObject.transform.localRotation,
                targetRotation,
                Time.deltaTime * lidAnimationSpeed
            );
        }
    }
    
    // Вызывается при наведении (через InteractionSystem)
    public void OnHighlight()
    {
        isHighlighted = true;
    }
    
    // Вызывается при убирании наведения
    public void OnUnhighlight()
    {
        isHighlighted = false;
        
        // Возвращаем оригинальные цвета
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_EmissionColor"))
            {
                renderers[i].material.SetColor("_EmissionColor", originalEmissionColors[i]);
            }
        }
    }
    
    // Вызывается при открытии ящика
    public void OnBoxOpen()
    {
        isOpen = true;
    }
    
    // Вызывается при закрытии ящика
    public void OnBoxClose()
    {
        isOpen = false;
    }
    
    // Эффект при размещении
    public void PlayPlaceEffect()
    {
        if (placeEffect != null)
        {
            placeEffect.Play();
        }
    }
    
    void OnDestroy()
    {
        // Очищаем созданные материалы
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                Destroy(renderer.material);
            }
        }
    }
}
