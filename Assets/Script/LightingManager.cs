using UnityEngine;

public class LightingManager : MonoBehaviour
{
    [Header("Directional Light (Sun)")]
    [SerializeField] private Light sunLight;

    [Header("Day/Night Cycle")]
    [SerializeField] private bool enableDayNightCycle = true;
    [SerializeField] private float dayDurationInMinutes = 10f; // Длина полного дня в реальных минутах
    [SerializeField][Range(0f, 24f)] private float currentTimeOfDay = 12f; // 0-24 часа

    [Header("Lighting Presets")]
    [SerializeField] private Color dawnColor = new Color(0.8f, 0.5f, 0.3f); // Рассвет
    [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.8f); // День
    [SerializeField] private Color duskColor = new Color(0.9f, 0.4f, 0.2f); // Закат
    [SerializeField] private Color nightColor = new Color(0.2f, 0.3f, 0.5f); // Ночь

    [Header("Intensity")]
    [SerializeField] private float dayIntensity = 1.2f;
    [SerializeField] private float nightIntensity = 0.3f;

    [Header("Fog Settings")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private float fogDensity = 0.02f;
    [SerializeField] private Color fogColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Ambient Light")]
    [SerializeField] private Color ambientDayColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color ambientNightColor = new Color(0.1f, 0.1f, 0.15f);

    private float timeMultiplier;

    void Start()
    {
        // Если sunLight не назначен, ищем Directional Light
        if (sunLight == null)
        {
            sunLight = FindFirstObjectByType<Light>();
            if (sunLight != null && sunLight.type != LightType.Directional)
            {
                Debug.LogWarning("Sun Light должен быть Directional Light!");
            }
        }

        // Рассчитываем множитель времени
        timeMultiplier = 24f / (dayDurationInMinutes * 60f);

        // Включаем туман
        RenderSettings.fog = enableFog;
        RenderSettings.fogDensity = fogDensity;

        UpdateLighting();
    }

    void Update()
    {
        if (enableDayNightCycle)
        {
            // Обновляем время
            currentTimeOfDay += Time.deltaTime * timeMultiplier;

            // Зацикливаем время (0-24)
            if (currentTimeOfDay >= 24f)
            {
                currentTimeOfDay = 0f;
            }

            UpdateLighting();
        }
    }

    void UpdateLighting()
    {
        if (sunLight == null) return;

        // Рассчитываем угол солнца (0° в полдень, 180° в полночь)
        float sunAngle = (currentTimeOfDay / 24f) * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Определяем текущий период дня и интерполируем цвета
        Color lightColor;
        float lightIntensity;
        Color ambientColor;

        if (currentTimeOfDay < 6f) // Ночь (0-6)
        {
            float t = currentTimeOfDay / 6f;
            lightColor = Color.Lerp(nightColor, dawnColor, t);
            lightIntensity = Mathf.Lerp(nightIntensity, dayIntensity * 0.6f, t);
            ambientColor = Color.Lerp(ambientNightColor, ambientDayColor, t);
        }
        else if (currentTimeOfDay < 8f) // Рассвет (6-8)
        {
            float t = (currentTimeOfDay - 6f) / 2f;
            lightColor = Color.Lerp(dawnColor, dayColor, t);
            lightIntensity = Mathf.Lerp(dayIntensity * 0.6f, dayIntensity, t);
            ambientColor = Color.Lerp(ambientDayColor * 0.7f, ambientDayColor, t);
        }
        else if (currentTimeOfDay < 18f) // День (8-18)
        {
            lightColor = dayColor;
            lightIntensity = dayIntensity;
            ambientColor = ambientDayColor;
        }
        else if (currentTimeOfDay < 20f) // Закат (18-20)
        {
            float t = (currentTimeOfDay - 18f) / 2f;
            lightColor = Color.Lerp(dayColor, duskColor, t);
            lightIntensity = Mathf.Lerp(dayIntensity, dayIntensity * 0.5f, t);
            ambientColor = Color.Lerp(ambientDayColor, ambientDayColor * 0.5f, t);
        }
        else // Ночь (20-24)
        {
            float t = (currentTimeOfDay - 20f) / 4f;
            lightColor = Color.Lerp(duskColor, nightColor, t);
            lightIntensity = Mathf.Lerp(dayIntensity * 0.5f, nightIntensity, t);
            ambientColor = Color.Lerp(ambientDayColor * 0.5f, ambientNightColor, t);
        }

        // Применяем освещение
        sunLight.color = lightColor;
        sunLight.intensity = lightIntensity;
        RenderSettings.ambientLight = ambientColor;

        // Обновляем туман
        if (enableFog)
        {
            RenderSettings.fogColor = Color.Lerp(fogColor, lightColor, 0.5f);
        }
    }

    // Публичные методы для управления временем
    public void SetTimeOfDay(float time)
    {
        currentTimeOfDay = Mathf.Clamp(time, 0f, 24f);
        UpdateLighting();
    }

    public float GetTimeOfDay()
    {
        return currentTimeOfDay;
    }

    public void SetDayNightCycle(bool enabled)
    {
        enableDayNightCycle = enabled;
    }
}