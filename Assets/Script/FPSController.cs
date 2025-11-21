using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController controller;
    private Camera playerCamera;

    private float xRotation = 0f;
    private float verticalVelocity = 0f;
    private bool isGrounded = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();

        // Вращаем камеру только когда инвентарь закрыт
        if (!InventorySystem.IsOpen)
        {
            HandleMouseLook();
        }
    }

    private void HandleMovement()
    {
        // Проверка земли
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // прижимаем к земле
        }

        // Ввод по осям
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Ходьба / спринт
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Прыжок
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Гравитация
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Вверх/вниз – крутим камеру
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // Влево/вправо – крутим персонажа
        transform.Rotate(Vector3.up * mouseX);
    }
}
