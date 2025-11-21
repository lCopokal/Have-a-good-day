using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIRaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"Клик по позиции: {Input.mousePosition}");
            Debug.Log($"Найдено UI элементов: {results.Count}");

            foreach (RaycastResult result in results)
            {
                Debug.Log($"  → {result.gameObject.name} ({result.gameObject.GetType()})");
            }
        }
    }
}