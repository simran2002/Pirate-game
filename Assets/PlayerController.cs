using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    private Vector2 moveInput;
    private CharacterController characterController;
    private PlayerInput playerInput;

    void Start()
    {
        // Get components
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        HandleMovement();
        HandleActions();
    }

    // This is called automatically by Input System when WASD is pressed
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // This is called automatically when Left Mouse is clicked
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Player attacked!");
            // Add attack logic here
        }
    }

    // This is called automatically when M key is pressed
    public void OnMine(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Player started mining!");
            // Add mining logic here
        }
    }

    void HandleMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            // Calculate movement direction
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);

            // Move the character
            characterController.Move(movement * moveSpeed * Time.deltaTime);

            // Rotate to face movement direction
            if (movement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void HandleActions()
    {
        // Additional action handling if needed
    }
}