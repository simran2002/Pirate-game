using UnityEngine;
using UnityEngine.UI;

public class CompanionAI : MonoBehaviour
{
    [Header("Companion Behavior")]
    public Transform player;
    public float followDistance = 10f;
    public float maxDistanceFromPlayer = 20f;
    public float waitTime = 3f;
    public Text companionMessageText;
    public float stopNearChestDistance = 1.5f;
    public float farBehindDistance = 15f;

    [Header("STate Management")]
    public CompanionState current_state = CompanionState.FollowingPlayer;
    private CompanionState previous_state_before_warning;
    private float wait_timer = 0f;

    private Vector3 current_direction = Vector3.forward;
    private SmellSource strongest_treasure;
    private float treasure_smell_intensity;
    private float enemy_smell_intensity = 0f;

    public enum CompanionState
    {
        FollowingPlayer,
        LeadingToTreasure,
        WaitingForPlayer,
        ReturningToPlayer,
        WarningPlayer
    }

    void Update()
    {
        checkDistanceToPlayer();
        updateState();
        executeStateBehavior();
    }

    void showCompanionMessage(string message)
    {
        if (companionMessageText != null)
        {
            companionMessageText.text = message;
        }
    }

    void hideCompanionMessage()
    {
        if (companionMessageText != null)
        {
            companionMessageText.text = "";
        }
    }

    void checkDistanceToPlayer()
    {
        if (player == null) return;

        float distance_to_player = Vector3.Distance(transform.position, player.position);

        // We only trigger return if not already returning and not in warning state
        // If tooo far, return to player
        if (distance_to_player > maxDistanceFromPlayer && current_state != CompanionState.ReturningToPlayer && current_state != CompanionState.WarningPlayer)
        {
            previous_state_before_warning = current_state;
            current_state = CompanionState.ReturningToPlayer;
            showCompanionMessage("Too far from player! Returning!");
        }
    }

    void updateState()
    {
        checkAllSmells();

        // State transitions Logic, added switch here instead of if_else.
        // Another reason is that I am not too well versed with classes and inheritance :(
        switch (current_state)
        {
            case CompanionState.FollowingPlayer:
                hideCompanionMessage();
                if (treasure_smell_intensity > 0.2f && enemy_smell_intensity < 0.5f)
                {
                    current_state = CompanionState.LeadingToTreasure;
                    showCompanionMessage("I smell treasure! Follow me! I am the Pied Piper");
                }
                break;

            case CompanionState.LeadingToTreasure:
                if (treasure_smell_intensity < 0.1f)
                {
                    current_state = CompanionState.FollowingPlayer;
                    showCompanionMessage("Lost the scent because of you! Ahhhhhhhhh! Follow me next time!!");
                }
                else if (isPlayerFarBehind())
                {
                    current_state = CompanionState.WaitingForPlayer;
                    wait_timer = waitTime;
                    showCompanionMessage("Great Scott! You are too far behind!");
                }
                break;

            case CompanionState.WaitingForPlayer:
                wait_timer -= Time.deltaTime;
                if (wait_timer <= 0f || !isPlayerFarBehind())
                {
                    current_state = CompanionState.LeadingToTreasure;
                    showCompanionMessage("Done fooling around mate? Now follow me and I will take you to the treasure. Thank me with a pint of Guiness later! Your treat!");
                }
                else
                {
                    showCompanionMessage("Follow me mate, I aint got all day! I want my Guiness!");
                }
                break;

            case CompanionState.ReturningToPlayer:
                if (Vector3.Distance(transform.position, player.position) < followDistance)
                {
                    if (treasure_smell_intensity > 0.2f && enemy_smell_intensity < 0.5f)
                    {
                        current_state = CompanionState.LeadingToTreasure;
                        showCompanionMessage("I smell treasure! Follow me! I am the Pied Piper");
                    }
                    else
                    {
                        current_state = CompanionState.FollowingPlayer;
                    }
                        
                }
                else
                {
                    showCompanionMessage("Too far from player! Returning!");
                }
                break;

            case CompanionState.WarningPlayer:
                // Check if enemy smell has faded
                if (enemy_smell_intensity < 0.5f)
                {
                    // Return to previous state before warning
                    current_state = previous_state_before_warning;
                }
                else if (isPlayerFarBehind())
                {
                    current_state = CompanionState.ReturningToPlayer;
                    showCompanionMessage("You evaded! Noice! Following you again");
                }
                else
                {
                    showCompanionMessage("WARNING! Enemy pirates nearby! I can smell their stench! Wonder how much rum they drank! Huhu");
                }
                break;
        }
    }

    void executeStateBehavior()
    {
        switch (current_state)
        {
            case CompanionState.FollowingPlayer:
                followPlayer();
                break;
            case CompanionState.LeadingToTreasure:
                if (strongest_treasure != null)
                {
                    moveTowardSmell(strongest_treasure.transform.position);
                }
                break;
            case CompanionState.WaitingForPlayer:
                waitForPlayer();
                break;
            case CompanionState.ReturningToPlayer:
                returnToPlayer();
                break;
            case CompanionState.WarningPlayer:
                warnPlayer();
                break;
        }
    }

    void followPlayer()
    {
        // When following player, use wandering logic if no treasure smell is detecetd
        if (treasure_smell_intensity < 0.2f)
        {
            wanderRandomly();
        }
        else
        {
            // The doggo will keep a fixed followDistance from the playr
            Vector3 target_position = player.position - player.forward * followDistance;
            moveToPosition(target_position);

            // Look at player
            Vector3 look_direction = (player.position - transform.position).normalized;
            look_direction.y = 0;
            if (look_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(look_direction) * Quaternion.Euler(0, 180, 0);
            }
        }
    }

    void waitForPlayer()
    {
        // Stop moving, look toward player
        Vector3 look_direction = (player.position - transform.position).normalized;
        look_direction.y = 0;
        if (look_direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(look_direction) * Quaternion.Euler(0, 180, 0);
        }
    }

    void returnToPlayer()
    {
        Vector3 direction_to_player = (player.position - transform.position).normalized;
        Vector3 target_position = player.position - direction_to_player * followDistance;
        moveToPosition(target_position);

        transform.rotation = Quaternion.LookRotation(direction_to_player) * Quaternion.Euler(0, 180, 0);
    }

    void warnPlayer()
    {
        // Look at player and bark warning or maybe that german cat! haha
        Vector3 look_direction = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(look_direction) * Quaternion.Euler(0, 180, 0);
    }

    void moveToPosition(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * Time.deltaTime * 6f;
    }

    bool isPlayerFarBehind()
    {
        return Vector3.Distance(transform.position, player.position) > farBehindDistance;
    }

    void checkAllSmells()
    {
        // FEtches all smell sources in the scene.
        SmellSource[] all_smells = FindObjectsOfType<SmellSource>();

        strongest_treasure = null;
        treasure_smell_intensity = 0f;
        enemy_smell_intensity = 0f;
        float treasure_smell_count = 0f;

        foreach (SmellSource smell in all_smells)
        {
            float intensity = smell.getSmellIntensityAt(transform.position);

            if (smell.smellType == SmellSource.SmellType.Treasure)
            {
                treasure_smell_count++;
                if (intensity > treasure_smell_intensity)
                {
                    treasure_smell_intensity = intensity;
                    strongest_treasure = smell;

                    NotifyMyPirateOfTreasure(smell.transform.position, intensity, smell);
                }
            }
            else if (smell.smellType == SmellSource.SmellType.EnemyPirate)
            {
                if (intensity > enemy_smell_intensity)
                {
                    enemy_smell_intensity = intensity;
                }
            }
        }

        if (treasure_smell_count == 0)
        {
            triggerGameOver();
        }

        if (enemy_smell_intensity > 0.5f && current_state != CompanionState.WarningPlayer)
        {
            // Save current state, so that ince the threat has been neutralised, it can go to prevoius state.
            previous_state_before_warning = current_state;
            current_state = CompanionState.WarningPlayer;
            showCompanionMessage("WARNING! There are enemies nearby! There are enemies nearby!");
        }
    }

    public void NotifyMyPirateOfTreasure(Vector3 treasurePosition, float intensity, SmellSource treasureChest)
    {
        if (intensity > 0.3f)
        {
            PirateAINavMesh myPirate = GetComponentInParent<PirateAINavMesh>();
            if (myPirate == null)
            {
                // Alternative: find by team association
                PirateAINavMesh[] pirates = FindObjectsOfType<PirateAINavMesh>();
                foreach (PirateAINavMesh pirate in pirates)
                {
                    if (pirate.myCompanion == this)
                    {
                        myPirate = pirate;
                        break;
                    }
                }
            }

            if (myPirate != null)
            {
                myPirate.OnTreasureFoundByCompanion(treasurePosition, intensity, treasureChest);
            }
            moveTowardSmell(treasurePosition);
        }
    }

    void triggerGameOver()
    {
        PlayerControl playerControl = FindObjectOfType<PlayerControl>();
        if (playerControl != null)
        {
            playerControl.EndGame();
            showCompanionMessage("No more treasures found! Game Over!");
        }
    }

    void moveTowardSmell(Vector3 target_position)
    {
        // Calculate direction to the treasure
        Vector3 direction_to_treasure = (target_position - transform.position).normalized;

        // Compare where the dog's facing direction to the computed direction above
        float dot_product = Vector3.Dot(current_direction, direction_to_treasure);

        // This was an interesting fix. The problem was that the below if condition was failing ever since I moved the treasure underground.
        // Reaason being: The dog and treasure had different y values causing distance to be larger than expected.
        // Hence made y as 0, since then it would behave as if they were same as before, when the treasure was on the ground.
        Vector3 transform_position_copy = transform.position;
        transform_position_copy.y = 0;
        Vector3 target_position_copy = target_position;
        target_position_copy.y = 0;
        float distanceToTreasure = Vector3.Distance(transform_position_copy, target_position_copy);
        if (distanceToTreasure <= stopNearChestDistance)
        {
            showCompanionMessage("Bow-wow! Treasure found!");
            return;
        }

        // (1.0 = perfect, -1.0 = opposite), everything in between means a turn at some angle
        if (dot_product > 0.8f)
        {
            // (0.8 to 1.0): Facing directly toward treasure
            moveForward();
        }
        else if (dot_product > 0.3f)
        {
            // (0.3 to 0.8): Facing somewhat toward treasure
            turnSlightlyTowardTarget(direction_to_treasure);
            moveForward();
        }
        else if (dot_product > -0.5f)
        {
            // (-0.5 to 0.3): Facing sideways to treasure
            turnTowardTarget(direction_to_treasure);
        }
        else
        {
            // (-1.0 to -0.5): Facing opposite to the treasure.
            makeUTurn();
        }
    }

    void moveForward()
    {
        // Move in the direction we're facing
        current_direction.y = 0;
        transform.position += current_direction * Time.deltaTime * 3f;
    }

    void turnSlightlyTowardTarget(Vector3 target_direction)
    {
        // We are not computing the exact angle but instead ading an angle close to the target direction.
        current_direction = Vector3.Slerp(current_direction, target_direction, 0.25f);
        current_direction.y = 0;
        transform.rotation = Quaternion.LookRotation(current_direction) * Quaternion.Euler(0, 180, 0);
    }

    void turnTowardTarget(Vector3 target_direction)
    {
        // Turn directly toward target
        current_direction = target_direction;
        current_direction.y = 0;
        transform.rotation = Quaternion.LookRotation(current_direction) * Quaternion.Euler(0, 180, 0);
    }

    void makeUTurn()
    {
        // Turn around 180 degrees
        current_direction = -current_direction;
        current_direction.y = 0;
        transform.rotation = Quaternion.LookRotation(current_direction) * Quaternion.Euler(0, 180, 0);
    }

    void wanderRandomly()
    {
        // Occasionally change direction when wandering, 1% chance each frame
        if (Random.Range(0, 100) < 1)
        {
            current_direction = Random.insideUnitSphere.normalized;
            //  We only want horizontal movement. It would be weird to say the dog fly! lol! TO infinity and beyond!
            current_direction.y = 0;
            transform.rotation = Quaternion.LookRotation(current_direction) * Quaternion.Euler(0, 180, 0);
        }
        moveForward();
    }
}