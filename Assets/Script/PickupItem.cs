using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    [SerializeField] private ItemData itemData; // ������ �� ScriptableObject
    [SerializeField] private int quantity = 1; // ���������� ���������

    private Rigidbody rb;
    private Collider itemCollider;
    private bool isPickedUp = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();

        // ���� ��� Rigidbody - ���������
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    public string GetInteractPrompt()
    {
        if (itemData != null)
        {
            return $"[E] ��������� {itemData.itemName} | [���] ����� � ����";
        }
        return "[E] ��������� | [���] ����� � ����";
    }

    public void Interact()
    {
        if (isPickedUp)
        {
            return;
        }

        InventorySystem inventory = FindObjectOfType<InventorySystem>();
        if (inventory == null)
        {
            Debug.LogWarning("InventorySystem  !");
            return;
        }

        if (itemData == null)
        {
            Debug.LogWarning("ItemData   !");
            return;
        }

        if (inventory.AddItem(itemData, quantity))
        {
            Debug.Log($"  : {itemData.itemName} x{quantity}");
            Destroy(gameObject);
        }
    }

    public void OnPickup()
    {
        isPickedUp = true;

        // �������� ������ ���� ������� ��� ������ � ��� ��������� �����
        CancelInvoke(nameof(MakeStatic));

        // ��������� ������
        rb.isKinematic = true;
        rb.useGravity = false;

        // ��������� ��������� ����� �� �����
        itemCollider.enabled = false;
    }

    public void OnDrop()
    {
        isPickedUp = false;

        // �������� ������ �������
        rb.isKinematic = false;
        rb.useGravity = true;

        // �������� ���������
        itemCollider.enabled = true;

        // ��������� ��������� ������� �����
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }

        if (playerCamera != null)
        {
            rb.AddForce(playerCamera.transform.forward * 2f, ForceMode.Impulse);
        }

        // ������ ������� ��������� ����� 2 ������� ����� ������
        Invoke(nameof(MakeStatic), 2f);
    }

    void MakeStatic()
    {
        if (!isPickedUp && rb != null) // ������ ���� ������� �� �������� �����
        {
            // ��������� ��� ������� ����� �� ���������
            if (rb.velocity.magnitude < 0.1f)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                // ���� ��� ��������� - �������� �����
                Invoke(nameof(MakeStatic), 0.5f);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // �������� ������ ���� ������� ���� ������
        if (!isPickedUp)
        {
            CancelInvoke(nameof(MakeStatic));
            Invoke(nameof(MakeStatic), 1f); // ��� 1 ��� ����� ������� �����
        }
    }

    public string GetItemName()
    {
        return itemData != null ? itemData.itemName : "����������� �������";
    }

    public string GetDescription()
    {
        return itemData != null ? itemData.description : "";
    }

    public ItemData GetItemData()
    {
        return itemData;
    }

    public int GetQuantity()
    {
        return quantity;
    }

    public bool IsPickedUp()
    {
        return isPickedUp;
    }
}
