using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FMODAreaFollower : MonoBehaviour
{
    public Transform playerTransform;
    public Transform fmodEmitter;
    public float lerpSpeed = 0f;

    private Bounds localBounds;

    void Start()
    {
        localBounds = GetComponent<MeshFilter>().sharedMesh.bounds;
    }

    void Update()
    {
        if (playerTransform == null || fmodEmitter == null) return;

        Vector3 localPlayerPos = transform.InverseTransformPoint(playerTransform.position);

        localPlayerPos.x = Mathf.Clamp(localPlayerPos.x, localBounds.min.x, localBounds.max.x);
        localPlayerPos.y = Mathf.Clamp(localPlayerPos.y, localBounds.min.y, localBounds.max.y);
        localPlayerPos.z = Mathf.Clamp(localPlayerPos.z, localBounds.min.z, localBounds.max.z);

        Vector3 targetPosition = transform.TransformPoint(localPlayerPos);

        if (lerpSpeed > 0f)
        {
            fmodEmitter.position = Vector3.Lerp(fmodEmitter.position, targetPosition, Time.deltaTime * lerpSpeed);
        }
        else
        {
            fmodEmitter.position = targetPosition;
        }
    }
}