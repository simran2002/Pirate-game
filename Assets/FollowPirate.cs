using UnityEngine;
using UnityEngine.UI;

public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 5f, -10f);
    public float smoothSpeed = 5f;
    private Transform target;

    // UI message objects for different targets
    public GameObject companionMessageBox;
    public GameObject pirate1MessageBox;
    public GameObject pirate2MessageBox;

    void Start()
    {
        Camera.main.gameObject.tag = "MainCamera";
        // Default to companion at start
        SetCameraTarget(GameObject.Find("Pirate_AI_2").transform);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        transform.LookAt(target);
    }

    public void SetCameraTarget(Transform newTarget)
    {
        target = newTarget;

        // Hide all message boxes
        if (companionMessageBox != null)
            companionMessageBox.SetActive(false);
        if (pirate1MessageBox != null)
            pirate1MessageBox.SetActive(false);
        if (pirate2MessageBox != null)
            pirate2MessageBox.SetActive(false);

        // Show only the relevant one
        if (newTarget.name == "Companion" && companionMessageBox != null)
            companionMessageBox.SetActive(true);
        else if (newTarget.name == "Pirate_AI_1" && pirate1MessageBox != null)
            pirate1MessageBox.SetActive(true);
        else if (newTarget.name == "Pirate_AI_2" && pirate2MessageBox != null)
            pirate2MessageBox.SetActive(true);
    }
}
