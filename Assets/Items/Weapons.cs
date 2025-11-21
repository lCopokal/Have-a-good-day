using UnityEngine;

// Тип оружия
public enum WeaponType
{
    Pistol,      // Пистолет
    Rifle,       // Винтовка
    Shotgun,     // Дробовик
    Melee        // Ближний бой
}

// Тип стрельбы
public enum FireMode
{
    Single,      // Одиночный
    Burst,       // Очередями
    Auto         // Автоматический
}

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponItem : ItemData
{
    [Header("Характеристики оружия")]
    public WeaponType weaponType = WeaponType.Pistol;
    public FireMode fireMode = FireMode.Single;

    [Tooltip("Урон за выстрел")]
    public float damage = 25f;

    [Tooltip("Дальность стрельбы")]
    public float range = 100f;

    [Tooltip("Скорострельность (выстрелов в минуту)")]
    public float fireRate = 300f;

    [Tooltip("Разброс (0 = идеальная точность)")]
    public float accuracy = 0.02f;

    [Header("Патроны")]
    public ItemData ammoType; // Ссылка на тип патронов
    public int magazineSize = 30; // Размер магазина
    public float reloadTime = 2f; // Время перезарядки

    [Header("Отдача")]
    public float recoilForce = 2f; // Сила отдачи
    public float recoilRecoverySpeed = 5f; // Скорость возврата прицела

    [Header("Звуки и эффекты")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab; // Вспышка выстрела
    public GameObject impactEffectPrefab; // Эффект попадания

    [Header("Модель оружия")]
    public GameObject weaponModelPrefab; // Модель в руках
    public Vector3 weaponPosition = new Vector3(0.2f, -0.2f, 0.5f); // Позиция в руках
    public Vector3 weaponRotation = Vector3.zero;

    // Текущее состояние (сохраняется при экипировке)
    [System.NonSerialized]
    public int currentAmmo;

    public override void Use(GameObject player)
    {
        Debug.Log($"Экипировано оружие: {itemName}");
        // Логика экипировки оружия будет в системе оружия
    }

    public override string GetItemInfo()
    {
        string info = base.GetItemInfo();

        info += "\n\n[Характеристики]";
        info += $"\nУрон: {damage}";
        info += $"\nДальность: {range}м";
        info += $"\nСкорострельность: {fireRate} в/мин";
        info += $"\nМагазин: {magazineSize}";

        if (ammoType != null)
            info += $"\nПатроны: {ammoType.itemName}";

        return info;
    }

    // Вспомогательный метод для получения задержки между выстрелами
    public float GetFireDelay()
    {
        return 60f / fireRate; // Конвертируем выстрелы/минуту в секунды
    }
}