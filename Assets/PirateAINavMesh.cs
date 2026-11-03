using UnityEngine;
using UnityEngine.AI;

public class PirateAINavMesh : MonoBehaviour
{
    [Header("Forced Combat Settings")]
    public static bool EnableForcedCombat = false;
    public float forcedCombatStartTime = 15f;
    private bool forcedCombatTriggered = false;

    [Header("Settings")]
    public float movementRadius = 10f;
    public float detectionDistance = 6f;
    public int maxHealth = 100;
    public float attackCooldown = 1.5f;
    public Animator animator;

    [Header("Team Setup")]
    public CompanionAI myCompanion;
    public string teamName = "PirateTeam";

    [Header("Personality Settings")]
    [SerializeField] private float aggression;
    [SerializeField] private float greediness;

    [Header("Treasure Digging")]
    public int pirateCoins = 0;
    public float digTime = 5f;

    [Header("State Management")]
    public PirateState currentState = PirateState.Roaming;

    private NavMeshAgent agent;
    public int currentHealth;
    private PirateAINavMesh enemyPirate;
    private bool isDead = false;
    private bool hasAttacked = false;
    private float lastAttackTime;

    //. defalt distance to detect enmy pirate 
    private float combatEngageDistance = 8f;

    private TreasureInfo currentTreasureTarget;
    private SmellSource currentTreasureChest;
    private bool isDigging = false;
    private float digStartTime;

    public enum PirateState
    {
        Roaming,
        FollowingCompanionToTreasure,
        InvestigatingTreasure,
        DiggingTreasure,
        FightingEnemyPirate,
        Dead
    }

    [System.Serializable]
    public class TreasureInfo
    {
        public Vector3 position;
        public float confidence;
        public SmellSource treasureChest;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // to avoid obstacle in way
        // it was not movig initially when encountering companino
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 80;   
        
        // initialise random health and greediness
        currentHealth = Random.Range(30, 100);
        aggression = Random.Range(0.3f, 0.9f);
        greediness = Random.Range(0.3f, 0.9f);

        PirateAINavMesh[] pirates = FindObjectsOfType<PirateAINavMesh>();
        foreach (PirateAINavMesh pirate in pirates)
        {
            if (pirate != this && pirate.teamName != this.teamName)
            {
                enemyPirate = pirate;
                break;
            }
        }

        pickNewDirection();
        add_ui_log($"Health: {currentHealth},\nAggression: {aggression:F2},\nGreediness: {greediness:F2}");

        // test condition to enable forec combat
        if (EnableForcedCombat)
        {
            Invoke(nameof(start_forced_combat), forcedCombatStartTime);
        }
    }

    void Update()
    {
        if (isDead) return;

        if (enemyPirate != null && !enemyPirate.isDead && !isDead)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemyPirate.transform.position);
            if (distanceToEnemy <= combatEngageDistance && currentState != PirateState.FightingEnemyPirate)
            {
                StartCombat();
            }
        }

        if (!isDigging)
        {
            UpdateState();
            executeStates();
        }

        if (isDigging)
        {
            check_digging();
        }

        updateAnimations();
    }

    void StartCombat()
    {
        if (isDigging)
            stop_digging();
            
        currentState = PirateState.FightingEnemyPirate;
        add_ui_log($"Combat Mode: Come here you {enemyPirate.gameObject.name}!");
    }

    void UpdateState()
    {
        if (isDead) 
        {
            currentState = PirateState.Dead;
            return;
        }

        switch (currentState)
        {
            case PirateState.Roaming:
                
                if (myCompanion != null && myCompanion.current_state == CompanionAI.CompanionState.LeadingToTreasure)
                {
                    if (0.5 < greediness)
                    {
                        currentState = PirateState.FollowingCompanionToTreasure;
                        add_ui_log($"Let's follow the companion. I want treasure!!  (Greediness: {greediness:F2})");
                    }
                }
                break;

            case PirateState.FollowingCompanionToTreasure:
                if (myCompanion == null || myCompanion.current_state != CompanionAI.CompanionState.LeadingToTreasure)
                {
                    currentState = PirateState.Roaming;
                }
                else if (isReachedTreasure())
                {   
                    currentState = PirateState.InvestigatingTreasure;
                    add_ui_log("Investigating Treasure ......");
                }
                break;

            case PirateState.InvestigatingTreasure:
                if (at_treasure())
                {
                    currentState = PirateState.DiggingTreasure;
                }
                else if (currentTreasureTarget == null)
                {
                    currentState = PirateState.Roaming;
                }
                break;

            case PirateState.DiggingTreasure:
                break;

            case PirateState.FightingEnemyPirate:
                if (enemyPirate == null || enemyPirate.isDead)
                {
                    currentState = PirateState.Roaming;
                }
                break;
        }
    }

    void executeStates()
    {
        switch (currentState)
        {
            case PirateState.Roaming:
                Roam();
                break;
            case PirateState.FollowingCompanionToTreasure:
                FollowCompanion();
                break;
            case PirateState.InvestigatingTreasure:
                investigate();
                break;
            case PirateState.DiggingTreasure:
                startDigging();
                break;
            case PirateState.FightingEnemyPirate:
                fight_enemy();
                break;
        }
    }

    void Roam()
    {
        if ((!agent.hasPath || agent.remainingDistance < 0.5f) && Random.Range(0, 100) < 1)
        {
            pickNewDirection();
        }
    }

    void FollowCompanion()
    {
        if (myCompanion != null)
        {
            agent.isStopped = false;
            Vector3 followPosition = myCompanion.transform.position - myCompanion.transform.forward * 2f;
            agent.SetDestination(followPosition);
        }
    }

    void investigate()
    {
        if (currentTreasureTarget != null)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTreasureTarget.position);
            
            if (currentTreasureTarget.treasureChest != null)
            {
                faceEachOther(currentTreasureTarget.treasureChest.transform.position);
            }
        }
    }

    void fight_enemy()
    {
        if (enemyPirate == null || enemyPirate.isDead) 
        {
            currentState = PirateState.Roaming;
            return;
        }

        float distance = Vector3.Distance(transform.position, enemyPirate.transform.position);

        if (distance > detectionDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(enemyPirate.transform.position);
            hasAttacked = false;
        }
        else
        {
            // stop if you have faced the eneny - and then fight
            agent.isStopped = true;
            faceEachOther(enemyPirate.transform.position);
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }

            if (!hasAttacked)
            {
                Invoke(nameof(attack_enemy), 4f);
            }
        }
    }

    void attack_enemy()
    {
        faceEachOther(enemyPirate.transform.position);
        if (animator != null)
        {
            add_ui_log($"Attacking Enemey!");
            animator.SetBool("isSwordAttack", true);
            Invoke(nameof(ResetAttack), 5f);
        }

        // Simple combat: health * aggression decides winner
        float myPower = currentHealth * aggression;
        float enemyPower = enemyPirate.currentHealth * enemyPirate.aggression;

        add_ui_log($"Combat Power = {myPower:F1}");

        if (myPower < enemyPower)
        {
            add_ui_log("AAAAAAAHHHHHHHHH!!!!!!!!");
            Die();
            add_ui_log($"{gameObject.name} lost!\nCoins Transferred to {enemyPirate.gameObject.name}: {pirateCoins}");
            enemyPirate.currentState = PirateState.Roaming;
        }
        else
        {
            add_ui_log($"{gameObject.name} WON!\n Total Coins Now: {pirateCoins}");
            
            currentState = PirateState.Roaming;

        }
        hasAttacked = true;
    }

    public void OnTreasureFoundByCompanion(Vector3 treasurePosition, float confidence, SmellSource treasureChest = null)
    {
        currentTreasureTarget = new TreasureInfo()
        {
            position = treasurePosition,
            confidence = confidence,
            treasureChest = treasureChest
        };

        add_ui_log($"Companion found the treasure for {gameObject.name}");
        if (0.5 < greediness) 
        {
            add_ui_log($"Let's follow the companion. I want treasure!!  (Greediness: {greediness:F2})");
            currentState = PirateState.FollowingCompanionToTreasure;
        }
        
    }

    void startDigging()
    {
        add_ui_log("Let's dig that out :P");
        if (currentTreasureChest != null && !isDigging)
        {
            isDigging = true;
            digStartTime = Time.time;
            agent.isStopped = true;

            faceEachOther(currentTreasureChest.transform.position);

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetBool("isMining", isDigging);
                add_ui_log("DIGGING...");
            }
        }
    }

    void check_digging()
    {
        if (isDigging && Time.time - digStartTime >= digTime)
        {
            CompleteDigging();
        }
    }

    void CompleteDigging()
    {
        if (currentTreasureChest != null)
        {
            int coinsFound = Random.Range(currentTreasureChest.minCoins, currentTreasureChest.maxCoins + 1);
            pirateCoins += coinsFound;

            add_ui_log($"Coins Collected: {coinsFound} coins! Total: {pirateCoins}");
            Destroy(currentTreasureChest.gameObject);
        }

        stop_digging();
        currentState = PirateState.Roaming;
        currentTreasureTarget = null;
        currentTreasureChest = null;
    }

    void stop_digging()
    {
        isDigging = false;
        agent.isStopped = false;

        if (animator != null)
        {
            animator.SetBool("isMining", isDigging);
            animator.SetBool("isWalking", true);
            add_ui_log("HUHHH!! TIRED NOW...");
        }
    }

   bool at_treasure()
    {
        if (currentTreasureTarget == null || currentTreasureTarget.treasureChest == null)
        {
            Debug.Log("Treasure target or chest is null");
            return false;
        }

        float distance = Vector3.Distance(transform.position, currentTreasureTarget.position);
        if (distance < 2f)
        {
            currentTreasureChest = currentTreasureTarget.treasureChest;
            return true;
        }
        return false;
    }

    bool isReachedTreasure()
    {
        if (myCompanion == null) return true;
        return Vector3.Distance(transform.position, myCompanion.transform.position) < 5f;
    }


    // Test function to start force combat for display
    void start_forced_combat()
    {
        if (!forcedCombatTriggered && enemyPirate != null && !enemyPirate.isDead && !isDead)
        {
            forcedCombatTriggered = true;
            Debug.Log($"FORCED COMBAT: {gameObject.name} and {enemyPirate.gameObject.name} forced to fight!");
            
            agent.SetDestination(enemyPirate.transform.position);
            enemyPirate.agent.SetDestination(transform.position);
        }
    }

    void ResetAttack()
    {
        if (animator != null)
        {
            animator.SetBool("isSwordAttack", false);
        }
    }

    void faceEachOther(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void pickNewDirection()
    {
        Vector3 randomDirection = Random.insideUnitSphere * movementRadius;
        randomDirection.y = 0;
        Vector3 targetPosition = transform.position + randomDirection;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, movementRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = PirateState.Dead;
        agent.isStopped = true;
        
        if (isDigging)
            stop_digging();
            
        if (animator != null)
        {
            add_ui_log("AAAAAAAHHHHHHHHH!!!!!!!!");
            animator.SetBool("isDeath", true);
        }
        
        if (enemyPirate != null && !enemyPirate.isDead)
        {
            enemyPirate.pirateCoins += pirateCoins;
        }
        // it is because the detah animation was not happening otherwise
        Invoke(nameof(VanishPirateAndCompanion), 5f);
    }

    void VanishPirateAndCompanion()
    {
        if (myCompanion != null)
            Destroy(myCompanion.gameObject);

        Destroy(gameObject);

        enemyPirate.agent.isStopped = false;
    }

    void updateAnimations()
    {
        if (animator != null)
        {
            bool isWalking = agent.velocity.magnitude > 0.1f && !agent.isStopped && !isDigging;
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("isMining", isDigging);
        }
    }

    private void add_ui_log(string message)
    {
        if (PirateUIManager.Instance != null)
        {
            PirateUIManager.Instance.AddLog(gameObject.name, message);
        }
    }

}