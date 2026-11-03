using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerControl : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Animation")]
    public Animator animator;

    [Header("Treasure System")]
    public int playerCoins = 0;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public TMP_Text coinsText;

    private Vector2 move_input;
    private CharacterController character_controller;
    private PlayerInput player_input;
    private SmellSource current_chest;
    private bool controls_enabled = true;
    private bool game_ended = false;

    void Start()
    {
        character_controller = GetComponent<CharacterController>();
        player_input = GetComponent<PlayerInput>();

        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.Find("GameOverPanel");
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
                Debug.Log("Found GameOverPanel: " + gameOverPanel.name);
            }
        }

        if (coinsText == null)
        {
            GameObject coins_text_obj = GameObject.Find("CoinsText");
            if (coins_text_obj != null)
            {
                coinsText = coins_text_obj.GetComponent<TMPro.TMP_Text>();
                Debug.Log("Found CoinsText: " + coins_text_obj.name);
            }
        }

        if (gameOverPanel == null || coinsText == null)
        {
            Debug.LogError("UI elements not found! Please assign them in Inspector.");
        }
    }

    void Update()
    {
        if (controls_enabled)
        {
            HandleMovement();
        }
    }

    public void OnExit(InputAction.CallbackContext context)
    {
        if (context.performed && !game_ended)
        {
            EndGame();
        }
    }

    public void EndGame()
    {
        game_ended = true;
        controls_enabled = false;

        Debug.Log("Game Over! Total coins collected: " + playerCoins);

        ShowGameOverScreen();

        DisableGameElements();
    }

    void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Update coins text
            if (coinsText != null)
            {
                coinsText.text = "Total Coins: " + playerCoins;
            }
        }
        else
        {
            Debug.LogWarning("Game Over Panel not assigned! Total coins: " + playerCoins);
        }
    }

    void DisableGameElements()
    {
        // Disable companion AI
        GameObject companion = GameObject.Find("Companion");
        if (companion != null)
        {
            CompanionAI companionAI = companion.GetComponent<CompanionAI>();
            if (companionAI != null)
                companionAI.enabled = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (controls_enabled)
        {
            move_input = context.ReadValue<Vector2>();
        }
        else
        {
            move_input = Vector2.zero;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && controls_enabled)
        {
            Debug.Log("Player attacked!");
            // Add attack logic here
        }
    }

    public void OnMine(InputAction.CallbackContext context)
    {
        if (context.performed && controls_enabled)
        {
            Debug.Log("Player started mining!");
            StartDigging();
        }
    }

    void HandleMovement()
    {
        if (move_input.magnitude > 0.1f)
        {
            // Calculate movement direction
            Vector3 movement = new Vector3(move_input.x, 0f, move_input.y);

            // MOve the character
            character_controller.Move(movement * moveSpeed * Time.deltaTime);

            animator.SetBool("isWalking", true);

            // Rotate to face movement direction
            if (movement != Vector3.zero)
            {
                Quaternion target_rotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    void StartDigging()
    {
        if (current_chest != null && controls_enabled)
        {
            // Disable controls
            DisableControls();

            // Play digging animation
            if (animator != null)
            {
                animator.SetBool("isMining", true);
            }

            Debug.Log("Starting to dig... controls disabled for 5 seconds!");

            // Start digging process in treasure chest
            current_chest.StartDigging(this);
        }
        else
        {
            Debug.Log("No treasure chest nearby or controls disabled!");
        }
    }

    public void DisableControls()
    {
        controls_enabled = false;
        move_input = Vector2.zero;
        animator.SetBool("isWalking", false);

        Debug.Log("Controls disabled - digging in progress!");
    }

    public void EnableControls()
    {
        controls_enabled = true;
        animator.SetBool("isMining", false);
        Debug.Log("Controls re-enabled!");
    }

    public void AddCoins(int amount)
    {
        playerCoins += amount;
        Debug.Log("Received " + amount + " coins! Total: " + playerCoins);
    }

    void OnTriggerEnter(Collider other)
    {
        SmellSource smell_source = other.GetComponent<SmellSource>();
        if (smell_source != null && smell_source.smellType == SmellSource.SmellType.Treasure)
        {
            current_chest = smell_source;
            Debug.Log("Treasure chest nearby! Press 'M' to dig.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        SmellSource smell_source = other.GetComponent<SmellSource>();
        if (smell_source != null && smell_source == current_chest)
        {
            current_chest = null;
            Debug.Log("Left treasure chest range.");
        }
    }
}