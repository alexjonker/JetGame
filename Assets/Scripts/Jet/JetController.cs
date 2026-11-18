using System;
using UnityEngine;

// Semi-realistic flight physics. All forces are accelerations, so mass doesn't affect handling.
// Input comes in through Controls (local player or network); crashing only raises Crashed.
[RequireComponent(typeof(Rigidbody))]
public class JetController : MonoBehaviour
{
    private const float AirDensity = 1.225f;
    private const float StallAngle = 28f;

    [Header("Engine")]
    [Tooltip("Newtons at full throttle.")]
    [SerializeField] private float maxThrust = 69600f;
    [SerializeField] private float throttleChangeRate = 0.6f;
    [SerializeField, Range(0f, 1f)] private float startThrottle = 0.6f;
    [SerializeField] private float startSpeed = 150f;

    [Header("Lift and Drag")]
    [SerializeField] private float wingArea = 23f;
    [Tooltip("Lift coefficient by angle of attack in degrees. Past the peak the wing stalls.")]
    [SerializeField] private AnimationCurve liftCurve = new AnimationCurve(
        new Keyframe(-90f, 0f),
        new Keyframe(-40f, -0.75f),
        new Keyframe(-28f, -1.15f),
        new Keyframe(0f, 0f),
        new Keyframe(28f, 1.15f),
        new Keyframe(40f, 0.75f),
        new Keyframe(90f, 0f));
    [SerializeField] private float baseDrag = 0.038f;
    [Tooltip("Extra drag from making lift, so hard turns cost speed.")]
    [SerializeField] private float liftDrag = 0.18f;
    [SerializeField] private float airbrakeDrag = 0.15f;
    [SerializeField] private float sideForce = 1f;

    [Header("Steering")]
    [Tooltip("Max turn rate in degrees per second (pitch, yaw, roll).")]
    [SerializeField] private Vector3 turnSpeed = new Vector3(30f, 10f, 150f);
    [Tooltip("How fast the turn rate builds up and dies down, in degrees per second² (pitch, yaw, roll).")]
    [SerializeField] private Vector3 turnAcceleration = new Vector3(60f, 20f, 360f);
    [Tooltip("Speed (m/s) for full control. Slower than this the controls get weaker.")]
    [SerializeField] private float fullControlSpeed = 100f;
    [SerializeField] private float maxPullG = 7f;
    [SerializeField] private float maxPushG = 3f;
    [Tooltip("Pulls are limited to this angle of attack, below the stall angle.")]
    [SerializeField] private float maxAngleOfAttack = 24f;

    [Header("Stall")]
    [Tooltip("How hard the airflow swings the nose around when the controls can't hold it.")]
    [SerializeField] private float airflowAlignment = 4f;
    [SerializeField] private float stallRotationDamping = 2f;

    private Rigidbody body;

    public event Action<JetController> Crashed;

    public JetControls Controls { get; set; }
    public float Throttle { get; private set; }
    public float Speed => body.linearVelocity.magnitude;
    public float AngleOfAttack { get; private set; }
    public bool IsStalling => Mathf.Abs(AngleOfAttack) > StallAngle && Speed > 1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        Throttle = startThrottle;
    }

    private void FixedUpdate()
    {
        UpdateThrottle();
        body.AddForce(transform.forward * (Throttle * maxThrust));

        Vector3 velocity = body.linearVelocity;
        float speed = velocity.magnitude;
        if (speed < 1f)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        AngleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg;
        float sideslip = Mathf.Atan2(localVelocity.x, localVelocity.z);
        float dynamicPressure = 0.5f * AirDensity * speed * speed;
        float controlAuthority = Mathf.Clamp01(localVelocity.z / fullControlSpeed);

        ApplyAerodynamicForces(velocity / speed, sideslip, dynamicPressure);
        Steer(controlAuthority, dynamicPressure, speed);
        AlignWithAirflow(controlAuthority, sideslip);
    }

    public void Place(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        body.position = position;
        body.rotation = rotation;
        body.linearVelocity = rotation * Vector3.forward * startSpeed;
        body.angularVelocity = Vector3.zero;
        Throttle = startThrottle;
        Controls = default;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Crashed?.Invoke(this);
    }

    private void UpdateThrottle()
    {
        float change = (Controls.Accelerate - Controls.Brake) * throttleChangeRate * Time.fixedDeltaTime;
        Throttle = Mathf.Clamp01(Throttle + change);
    }

    private void ApplyAerodynamicForces(Vector3 airflow, float sideslip, float dynamicPressure)
    {
        float force = dynamicPressure * wingArea;
        float liftCoefficient = liftCurve.Evaluate(AngleOfAttack);
        float dragCoefficient = baseDrag + liftDrag * liftCoefficient * liftCoefficient + airbrakeDrag * Controls.Brake;

        Vector3 liftDirection = Vector3.Cross(airflow, transform.right).normalized;
        Vector3 lift = liftDirection * (force * liftCoefficient);
        Vector3 drag = -airflow * (force * dragCoefficient);
        Vector3 side = -transform.right * (force * sideForce * sideslip);

        body.AddForce(lift + drag + side);
    }

    private void Steer(float controlAuthority, float dynamicPressure, float speed)
    {
        // Local axes: +X pitches nose down, +Y yaws right, -Z rolls right.
        Vector3 input = new Vector3(Controls.Pitch, Controls.Yaw, -Controls.Roll);
        Vector3 targetRate = Vector3.Scale(input, turnSpeed) * (Mathf.Deg2Rad * controlAuthority);
        targetRate.x = Mathf.Clamp(targetRate.x, -MaxPitchRate(true, dynamicPressure, speed), MaxPitchRate(false, dynamicPressure, speed));

        Vector3 currentRate = transform.InverseTransformDirection(body.angularVelocity);
        Vector3 maxChange = turnAcceleration * (Mathf.Deg2Rad * controlAuthority * Time.fixedDeltaTime);
        Vector3 change = new Vector3(
            Mathf.Clamp(targetRate.x - currentRate.x, -maxChange.x, maxChange.x),
            Mathf.Clamp(targetRate.y - currentRate.y, -maxChange.y, maxChange.y),
            Mathf.Clamp(targetRate.z - currentRate.z, -maxChange.z, maxChange.z));

        body.AddRelativeTorque(change, ForceMode.VelocityChange);
    }

    // Fastest pitch rate the wings can follow: limited by G and by the lift left after holding up the weight.
    private float MaxPitchRate(bool pullingUp, float dynamicPressure, float speed)
    {
        float wingAcceleration = dynamicPressure * wingArea * liftCurve.Evaluate(maxAngleOfAttack) / body.mass;
        float weightOnWings = Vector3.Dot(-Physics.gravity, transform.up);
        float available = pullingUp ? wingAcceleration - weightOnWings : wingAcceleration + weightOnWings;
        float gLimit = (pullingUp ? maxPullG : maxPushG) * -Physics.gravity.y;
        return Mathf.Max(Mathf.Min(available, gLimit), 0f) / speed;
    }

    // When the controls lose authority, the airflow swings the nose towards the direction of travel.
    private void AlignWithAirflow(float controlAuthority, float sideslip)
    {
        Vector3 currentRate = transform.InverseTransformDirection(body.angularVelocity);
        Vector3 airflowTorque = new Vector3(AngleOfAttack * Mathf.Deg2Rad, sideslip, 0f) * airflowAlignment
                                - currentRate * stallRotationDamping;
        body.AddRelativeTorque(airflowTorque * (1f - controlAuthority), ForceMode.Acceleration);
    }
}
