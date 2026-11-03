using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PirateUIManager : MonoBehaviour
{
    [Header("Pirate Log Texts")]
    public Text Pirate1text;
    public Text Pirate2text;

    [Header("Log Settings")]
    public int maxLogLines = 1; // Set to 1 to only show latest log
    
    private Queue<string> pirate1Logs = new Queue<string>();
    private Queue<string> pirate2Logs = new Queue<string>();

    // Singleton pattern for easy access
    public static PirateUIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize with empty logs
        UpdateLogTexts();
    }

    public void AddLog(string pirateName, string logMessage)
    {
        Queue<string> targetQueue;
        Text targetText;

        // Determine which pirate's log to update
        if (pirateName.Contains("1") || pirateName.ToLower().Contains("red"))
        {
            targetQueue = pirate1Logs;
            targetText = Pirate1text;
        }
        else if (pirateName.Contains("2") || pirateName.ToLower().Contains("blue"))
        {
            targetQueue = pirate2Logs;
            targetText = Pirate2text;
        }
        else
        {
            Debug.LogWarning($"Unknown pirate name: {pirateName}");
            return;
        }

        // Clear previous logs and only keep the latest one
        targetQueue.Clear(); // Remove all previous logs
        targetQueue.Enqueue($"{logMessage}"); // Add only the new log

        // Update UI text
        UpdateLogText(targetQueue, targetText, pirateName);
    }

    void UpdateLogText(Queue<string> logs, Text textComponent, string pirateName)
    {
        string fullText = $"{pirateName} Log:\n";
        foreach (string log in logs)
        {
            fullText += log + "\n";
        }
        textComponent.text = fullText;
    }

    void UpdateLogTexts()
    {
        UpdateLogText(pirate1Logs, Pirate1text, "Pirate 1");
        UpdateLogText(pirate2Logs, Pirate2text, "Pirate 2");
    }
}