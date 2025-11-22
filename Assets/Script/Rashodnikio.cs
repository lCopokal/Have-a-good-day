using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable Item")]
public class ConsumableItem : ItemData
{
    [Header("Эффекты при использовании")]
    [Tooltip("Сколько HP восстанавливает (может быть отрицательным для урона)")]
    public float healthRestore = 0f;

    [Tooltip("Сколько голода восстанавливает")]
    public float hungerRestore = 0f;

    [Tooltip("Сколько жажды восстанавливает")]
    public float thirstRestore = 0f;

    [Tooltip("Сколько радиации убирает (отрицательное = добавляет)")]
    public float radiationChange = 0f;

    [Header("Дополнительные эффекты")]
    [Tooltip("Время действия эффекта (0 = мгновенный)")]
    public float effectDuration = 0f;

    [Tooltip("Эффект со временем (например регенерация HP)")]
    public bool hasOverTimeEffect = false;
    public float healthPerSecond = 0f;

    public override void Use(GameObject player)
    {
        SurvivalSystem survival = player.GetComponent<SurvivalSystem>();

        if (survival == null)
        {
            Debug.LogWarning("SurvivalSystem не найдена на игроке!");
            return;
        }

        // Применяем эффекты
        if (healthRestore != 0)
        {
            if (healthRestore > 0)
                survival.Heal(healthRestore);
            else
                survival.TakeDamage(-healthRestore);

            Debug.Log($"{itemName}: HP {(healthRestore > 0 ? "+" : "")}{healthRestore}");
        }

        if (hungerRestore != 0)
        {
            survival.Eat(hungerRestore);
            Debug.Log($"{itemName}: Голод +{hungerRestore}");
        }

        if (thirstRestore != 0)
        {
            survival.Drink(thirstRestore);
            Debug.Log($"{itemName}: Жажда +{thirstRestore}");
        }

        if (radiationChange != 0)
        {
            if (radiationChange > 0)
                survival.AddRadiation(radiationChange);
            else
                survival.RemoveRadiation(-radiationChange);

            Debug.Log($"{itemName}: Радиация {(radiationChange > 0 ? "+" : "")}{radiationChange}");
        }

        // Если есть эффект со временем - запускаем корутину
        if (hasOverTimeEffect && effectDuration > 0)
        {
            MonoBehaviour playerMono = player.GetComponent<MonoBehaviour>();
            if (playerMono != null)
            {
                playerMono.StartCoroutine(ApplyOverTimeEffect(survival));
            }
        }
    }

    private System.Collections.IEnumerator ApplyOverTimeEffect(SurvivalSystem survival)
    {
        float elapsed = 0f;

        while (elapsed < effectDuration)
        {
            if (healthPerSecond != 0)
            {
                if (healthPerSecond > 0)
                    survival.Heal(healthPerSecond * Time.deltaTime);
                else
                    survival.TakeDamage(-healthPerSecond * Time.deltaTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public override string GetItemInfo()
    {
        string info = base.GetItemInfo();

        info += "\n\n[Эффекты]";
        if (healthRestore != 0)
            info += $"\nЗдоровье: {(healthRestore > 0 ? "+" : "")}{healthRestore}";
        if (hungerRestore != 0)
            info += $"\nГолод: +{hungerRestore}";
        if (thirstRestore != 0)
            info += $"\nЖажда: +{thirstRestore}";
        if (radiationChange != 0)
            info += $"\nРадиация: {(radiationChange > 0 ? "+" : "")}{radiationChange}";

        if (hasOverTimeEffect)
            info += $"\n\nЭффект длится {effectDuration} сек";

        return info;
    }
}