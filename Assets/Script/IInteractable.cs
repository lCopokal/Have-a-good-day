using UnityEngine;

/// <summary>
/// Простейший интерфейс для объектов, с которыми можно взаимодействовать (E и т.п.).
/// Сейчас достаточно одного метода Interact().
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Вызывается, когда игрок взаимодействует с объектом (нажимает E и т.п.).
    /// </summary>
    void Interact();
}
