using UnityEngine;
using UnityEngine.UI;

public class SurvivalSystem : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float maxThirst = 100f;
    [SerializeField] private float maxRadiation = 100f;

    private float currentHealth;
    private float currentHunger;
    private float currentThirst;
    private float currentRadiation;

    [Header("Depletion Rates (per second)")]
    [SerializeField] private float hungerDepletionRate = 0.5f; // Голод убывает на 0.5 в секунду
    [SerializeField] private float thirstDepletionRate = 0.7f; // Жажда убывает быстрее
    [SerializeField] private float thirstSprintMultiplier = 2f; // Множитель жажды при беге
    [SerializeField] private float radiationDecayRate = 0.2f; // Радиация медленно уходит

    [Header("Damage from Needs")]
    [SerializeField] private float hungerDamageRate = 2f; // Урон в секунду при голоде = 0
    [SerializeField] private float thirstDamageRate = 3f; // Урон в секунду при жажде = 0
    [SerializeField] private float radiationDamageRate = 1f; // Урон от радиации

    [Header("UI Bars")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image hungerBar;
    [SerializeField] private Image thirstBar;
    [SerializeField] private Image radiationBar;

    [Header("UI Colors")]
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color hungerColor = new Color(1f, 0.6f, 0f); // Оранжевый
    [SerializeField] private Color thirstColor = Color.cyan;
    [SerializeField] private Color radiationColor = Color.green;

    private FPSController fpsController;
    private bool isDead = false;

    void Start()
    {
        // Инициализируем статы на максимум
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        currentRadiation = 0f;

        fpsController = GetComponent<FPSController>();

        // Устанавливаем цвета полосок
        if (healthBar != null) healthBar.color = healthColor;
        if (hungerBar != null) hungerBar.color = hungerColor;
        if (thirstBar != null) thirstBar.color = thirstColor;
        if (radiationBar != null) radiationBar.color = radiationColor;

        UpdateUI();
    }

    void Update()
    {
        if (isDead) return;

        // Убавляем голод и жажду со временем
        DepleteNeeds();

        // Убавляем радиацию со временем
        if (currentRadiation > 0)
        {
            currentRadiation -= radiationDecayRate * Time.deltaTime;
            currentRadiation = Mathf.Max(0, currentRadiation);
        }

        // Урон от голода
        if (currentHunger <= 0)
        {
            TakeDamage(hungerDamageRate * Time.deltaTime);
        }

        // Урон от жажды
        if (currentThirst <= 0)
        {
            TakeDamage(thirstDamageRate * Time.deltaTime);
        }

        // Урон от радиации
        if (currentRadiation > 50) // Радиация начинает вредить после 50%
        {
            float radiationDamage = ((currentRadiation - 50f) / 50f) * radiationDamageRate;
            TakeDamage(radiationDamage * Time.deltaTime);
        }

        UpdateUI();
    }

    void DepleteNeeds()
    {
        // Убавляем голод
        currentHunger -= hungerDepletionRate * Time.deltaTime;
        currentHunger = Mathf.Max(0, currentHunger);

        // Убавляем жажду (быстрее при беге)
        float thirstRate = thirstDepletionRate;

        // Проверяем, бежит ли игрок (Shift зажат)
        if (Input.GetKey(KeyCode.LeftShift))
        {
            thirstRate *= thirstSprintMultiplier;
        }

        currentThirst -= thirstRate * Time.deltaTime;
        currentThirst = Mathf.Max(0, currentThirst);
    }

    void UpdateUI()
    {
        // Обновляем полоски (fillAmount от 0 до 1)
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;

        if (hungerBar != null)
            hungerBar.fillAmount = currentHunger / maxHunger;

        if (thirstBar != null)
            thirstBar.fillAmount = currentThirst / maxThirst;

        if (radiationBar != null)
            radiationBar.fillAmount = currentRadiation / maxRadiation;
    }

    // Публичные методы для управления статами

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }

    public void Eat(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Min(maxHunger, currentHunger);
    }

    public void Drink(float amount)
    {
        currentThirst += amount;
        currentThirst = Mathf.Min(maxThirst, currentThirst);
    }

    public void AddRadiation(float amount)
    {
        currentRadiation += amount;
        currentRadiation = Mathf.Min(maxRadiation, currentRadiation);
    }

    public void RemoveRadiation(float amount)
    {
        currentRadiation -= amount;
        currentRadiation = Mathf.Max(0, currentRadiation);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Игрок умер!");

        // Отключаем управление
        if (fpsController != null)
        {
            fpsController.enabled = false;
        }

        // Здесь можно добавить экран смерти, перезагрузку и т.д.
    }

    // Геттеры для проверки состояния
    public bool IsDead() { return isDead; }
    public float GetHealth() { return currentHealth; }
    public float GetHunger() { return currentHunger; }
    public float GetThirst() { return currentThirst; }
    public float GetRadiation() { return currentRadiation; }
}