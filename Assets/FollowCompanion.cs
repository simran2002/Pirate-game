// using UnityEngine;

// public class CameraFollow : MonoBehaviour
// {
//     public Vector3 offset = new Vector3(0f, 8f, -12f); // Good for seeing both characters
//     public float smoothSpeed = 5f;
//     private Transform target;

//     void Start()
//     {
//         Camera.main.gameObject.tag = "MainCamera";

//         // Find companion
//         target = GameObject.Find("Companion").transform;

//         if (target == null)
//             Debug.LogError("Could not find Companion object!");
//     }

//     void LateUpdate()
//     {
//         if (target == null) return;

//         Vector3 desired_position = target.position + offset;
//         Vector3 smoothed_position = Vector3.Lerp(transform.position, desired_position, smoothSpeed * Time.deltaTime);
//         transform.position = smoothed_position;
//         transform.LookAt(target);
//     }
// }