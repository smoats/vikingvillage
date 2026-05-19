using UnityEngine;

public class TriggerToggle : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag the GameObject you want to show/hide into this slot.")]
    public GameObject targetObject;

    [Tooltip("Only objects with this tag will activate the trigger.")]
    public string triggerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering has the correct tag
        if (other.CompareTag(triggerTag))
        {
            if (targetObject != null)
            {
                targetObject.SetActive(true); // Turn ON
            }
            else
            {
                Debug.LogWarning("Target Object is not assigned in the Inspector!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object exiting has the correct tag
        if (other.CompareTag(triggerTag))
        {
            if (targetObject != null)
            {
                targetObject.SetActive(false); // Turn OFF
            }
        }
    }
}