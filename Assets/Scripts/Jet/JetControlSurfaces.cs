using UnityEngine;

[RequireComponent(typeof(JetController))]
public class JetControlSurfaces : MonoBehaviour
{
    [Tooltip("Drag the bones in from the jet model.")]
    [SerializeField] private Transform leftElevon, rightElevon, rudder, nozzle;
    [Tooltip("How far the trailing edges move at full input, in meters.")]
    [SerializeField] private float elevonTravel = 0.3f;
    [SerializeField] private float rudderTravel = 0.3f;
    [Tooltip("Nozzle width at idle and at full throttle: it closes as the engine spools up.")]
    [SerializeField] private Vector2 nozzleWidth = new Vector2(1.2f, 0.85f);
    [Tooltip("How quickly the surfaces follow the controls.")]
    [SerializeField] private float sharpness = 10f;

    private JetController jet;
    private Vector3 leftElevonRest, rightElevonRest, rudderRest;
    private Vector3 nozzleRestScale;
    private float pitch, roll, yaw, throttle;

    private void Awake()
    {
        jet = GetComponent<JetController>();
        leftElevonRest = transform.InverseTransformPoint(leftElevon.position);
        rightElevonRest = transform.InverseTransformPoint(rightElevon.position);
        rudderRest = transform.InverseTransformPoint(rudder.position);
        nozzleRestScale = nozzle.localScale;
    }

    private void LateUpdate()
    {
        float blend = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        pitch = Mathf.Lerp(pitch, jet.Controls.Pitch, blend);
        roll = Mathf.Lerp(roll, jet.Controls.Roll, blend);
        yaw = Mathf.Lerp(yaw, jet.Controls.Yaw, blend);
        throttle = Mathf.Lerp(throttle, jet.Throttle, blend);

        // Elevons
        rightElevon.position = transform.TransformPoint(rightElevonRest + Vector3.up * Mathf.Clamp(-pitch + roll, -1f, 1f) * elevonTravel);
        leftElevon.position = transform.TransformPoint(leftElevonRest + Vector3.up * Mathf.Clamp(-pitch - roll, -1f, 1f) * elevonTravel);

        // Rudder
        rudder.position = transform.TransformPoint(rudderRest + Vector3.right * yaw * rudderTravel);

        // Nozzle
        float width = Mathf.Lerp(nozzleWidth.x, nozzleWidth.y, throttle);
        nozzle.localScale = Vector3.Scale(nozzleRestScale, new Vector3(width, 1f, width));
    }
}