using UnityEngine;

// ДОПОЛНЕНИЕ к существующему InteractionSystem
// Если у тебя уже есть InteractionSystem, добавь эти методы туда

/* 
   Добавь этот код в свой InteractionSystem.cs:
   
   void Update()
   {
       // ... существующий код ...
       
       // Добавь проверку режима размещения ящика
       BoxPlacementSystem placementSystem = GetComponent<BoxPlacementSystem>();
       if (placementSystem != null && placementSystem.IsInPlacementMode())
       {
           // Не проверяем взаимодействия во время размещения
           return;
       }
       
       // ... остальной код проверки взаимодействий ...
   }
*/

// Этот скрипт можно использовать как референс или добавить в проект отдельно

public class StorageBoxInteractionHelper : MonoBehaviour
{
    private InteractionSystem interactionSystem;
    private BoxPlacementSystem placementSystem;
    
    void Start()
    {
        interactionSystem = GetComponent<InteractionSystem>();
        placementSystem = GetComponent<BoxPlacementSystem>();
    }
    
    void Update()
    {
        // Блокируем обычные взаимодействия во время размещения ящика
        if (placementSystem != null && placementSystem.IsInPlacementMode())
        {
            // Отключаем взаимодействия
            if (interactionSystem != null)
            {
                // Скрываем подсказку взаимодействия
                // interactionSystem.HidePrompt(); // если есть такой метод
            }
        }
    }
    
    // Вспомогательный метод для проверки, можно ли взаимодействовать
    public bool CanInteract()
    {
        if (placementSystem != null && placementSystem.IsInPlacementMode())
        {
            return false;
        }
        
        return true;
    }
}

/* 
   ВАЖНО: Интеграция с ItemPickupSystem
   
   В твоём ItemPickupSystem.cs добавь проверку типа предмета:
   
   void Update()
   {
       if (isHoldingItem)
       {
           // Если держим ящик хранения
           if (heldItemData is StorageBoxItem)
           {
               // Показываем подсказку "ЛКМ - Разместить ящик"
               // ПКМ работает как обычно (вращение)
               
               if (Input.GetMouseButtonDown(0))
               {
                   // Логика размещения обрабатывается в StorageBoxPickupHandler
                   return;
               }
           }
           
           // ... остальная логика для обычных предметов ...
       }
   }
*/
