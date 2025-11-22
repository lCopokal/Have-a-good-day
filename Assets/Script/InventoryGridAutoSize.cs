using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup), typeof(RectTransform))]
public class InventoryGridAutoSize : MonoBehaviour
{
    [Header("Сетка инвентаря")]
    public int columns = 5;
    public int rows = 4;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
        UpdateGrid();
    }

    void OnRectTransformDimensionsChange()
    {
        // если панель растянули/сузили — пересчитать
        UpdateGrid();
    }

    public void UpdateGrid()
    {
        if (grid == null || rectTransform == null) return;
        if (columns <= 0 || rows <= 0) return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;

        Rect rect = rectTransform.rect;
        var padding = grid.padding;
        var spacing = grid.spacing;

        float width = rect.width - padding.left - padding.right;
        float height = rect.height - padding.top - padding.bottom;

        float cellWidth = (width - spacing.x * (columns - 1)) / columns;
        float cellHeight = (height - spacing.y * (rows - 1)) / rows;

        float size = Mathf.Min(cellWidth, cellHeight); // квадратные и точно влезают

        grid.cellSize = new Vector2(size, size);
    }
}
