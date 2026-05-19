using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

// This forces Unity to ensure an FMOD Emitter is attached to this object
[RequireComponent(typeof(FMODUnity.StudioEventEmitter))]
public class FMODSplineFollower : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The spline representing the path of the sound (e.g., a river).")]
    public SplineContainer splineContainer;

    [Tooltip("The player or audio listener that the sound should stay close to.")]
    public Transform player;

    void Update()
    {
        if (splineContainer == null || player == null)
        {
            return;
        }

        // 1. Convert the player's world position into the spline's local space
        float3 localPlayerPos = splineContainer.transform.InverseTransformPoint(player.position);

        // 2. Calculate the mathematically nearest point on the spline to the player
        // 't' represents the normalized position along the spline (0.0 to 1.0) but we only need the position here
        SplineUtility.GetNearestPoint(splineContainer.Spline, localPlayerPos, out float3 nearestLocalPoint, out float t);

        // 3. Convert that local point back into world space
        Vector3 nearestWorldPoint = splineContainer.transform.TransformPoint(nearestLocalPoint);

        // 4. Move this GameObject (and therefore the FMOD Emitter) to that point
        transform.position = nearestWorldPoint;
    }
}