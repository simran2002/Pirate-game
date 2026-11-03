using UnityEngine;

public class SmellSource : MonoBehaviour
{
    public enum SmellType { Treasure, EnemyPirate }
    public SmellType smellType = SmellType.Treasure;
    public float smellRange = 50f;

    [Header("Treasure Settings")]
    public int minCoins = 50;
    public int maxCoins = 100;
    public float digRadius = 3f;

    private int coins_in_chest;
    private bool player_in_range = false;
    private bool is_being_dug = false;

    void Start()
    {
        coins_in_chest = Random.Range(minCoins, maxCoins + 1);
        Debug.Log("Treasure chest contains: " + coins_in_chest + " coins!");

        // Adding trigger collider for digging range
        SphereCollider digCollider = gameObject.AddComponent<SphereCollider>();
        digCollider.isTrigger = true;
        digCollider.radius = digRadius;
    }

    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player") && !is_being_dug)
        {
            player_in_range = true;
            Debug.Log("Press 'M' to dig for treasure!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (smellType != SmellType.Treasure) return;

        if (other.CompareTag("Player"))
        {
            player_in_range = false;
            Debug.Log("Too far to dig!");
        }
    }

    public void StartDigging(PlayerControl player)
    {

        if (player_in_range && !is_being_dug && coins_in_chest > 0)
        {
            is_being_dug = true;
            player.DisableControls();
            Debug.Log("Started digging... 5 seconds remaining!");

            // Start the 5-second dig process
            Invoke("CompleteDigging", 5f);
        }
    }

    void CompleteDigging()
    {

        PlayerControl player = FindObjectOfType<PlayerControl>();
        if (player != null)
        {
            player.AddCoins(coins_in_chest);
            player.EnableControls();
            Debug.Log("Digging complete! Found " + coins_in_chest + " coins!");
        }

        coins_in_chest = 0;
        is_being_dug = false;

        Destroy(gameObject);
    }

    // THis calculates how strong the smell is at any position
    public float getSmellIntensityAt(Vector3 detector_position)
    {
        float distance = Vector3.Distance(transform.position, detector_position);

        // If too far away, no smell
        if (distance > smellRange)
            return 0f;

        // Smell gets weaker with distance
        // Close: 1.0, Far: 0.1
        float intensity = 1f / (1f + distance * 0.1f);

        return intensity;
    }
}