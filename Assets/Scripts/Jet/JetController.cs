using System;
using UnityEngine;

// Semi-realistic flight physics. All forces are accelerations, so mass doesn't affect handling.
// Input comes in through Controls (local player or network); crashing only raises Crashed.
[RequireComponent(typeof(Rigidbody))]
public class JetController : MonoBehaviour
{
    private const float StallAngle = 18f;

    [Header("Engine")]
    [SerializeField] private float maxThrust = 60f;
    [SerializeField] private float throttleChangeRate = 0.6f;
    [SerializeField, Range(0f, 1f)] private float startThrottle = 0.6f;
    [SerializeField] private float startSpeed = 150f;

    [Header("Lift")]
    [Tooltip("Lift by angle of attack in degrees. The drop after the peak is the stall.")]
    [SerializeField] private AnimationCurve liftCurve = new AnimationCurve(
        new Keyframe(-90f, 0f),
        new Keyframe(-30f, -0.5f),
        new Keyframe(-18f, -1.1f),
        new Keyframe(0f, 0.15f),
        new Keyframe(18f, 1.25f),
        new Keyframe(30f, 0.5f),
        new Keyframe(90f, 0f));
    [SerializeField] private float liftPower = 0.0012f;
    [Tooltip("Speed lost while pulling hard turns.")]
    [SerializeField] private float inducedDrag = 0.15f;

    [Header("Drag")]
    [SerializeField] private float dragPower = 0.00096f;
    [SerializeField] private float airbrakeDrag = 3f;
    [Tooltip("Stops the jet sliding sideways.")]
    [SerializeField] private float sideForce = 0.004f;

    [Header("Steering")]
    [SerializeField] private float pitchTorque = 3f;
    [SerializeField] private float rollTorque = 9f;
    [SerializeField] private float yawTorque = 1f;
    [Tooltip("Below this speed (m/s) steering gets weaker.")]
    [SerializeField] private float fullControlSpeed = 110f;
    [Tooltip("Pitch, yaw, roll. Higher stops rotating sooner.")]
    [SerializeField] private Vector3 angularDamping = new Vector3(2.5f, 2.5f, 3f);

    [Header("Stability")]
    [Tooltip("How strongly the nose turns into the airflow (pitch, yaw).")]
    [SerializeField] private Vector2 stability = new Vector2(3f, 3f);
    [Tooltip("Angle of attack the jet settles at when flying straight.")]
    [SerializeField] private float trimAngleOfAttack = 2.5f;

    private Rigidbody body;

    public event Action<JetController> Crashed;

    public JetControls Controls { get; set; }
    public float Throttle { get; private set; }
    public float Speed => body.linearVelocity.magnitude;
    public float AngleOfAttack { get; private set; }
    public bool IsStalling => Mathf.Abs(AngleOfAttack) > StallAngle;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        Throttle = startThrottle;
    }

    private void FixedUpdate()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
        AngleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg;

        UpdateThrottle();
        ApplyThrust();
        ApplyAerodynamics(localVelocity);
        ApplyRotation(localVelocity);
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

    private void ApplyThrust()
    {
        body.AddForce(transform.forward * (Throttle * maxThrust), ForceMode.Acceleration);
    }

    private void ApplyAerodynamics(Vector3 localVelocity)
    {
        float speed = localVelocity.magnitude;
        if (speed < 0.1f)
        {
            return;
        }

        Vector3 airflow = body.linearVelocity / speed;
        float liftCoefficient = liftCurve.Evaluate(AngleOfAttack);

        float lift = liftPower * speed * speed * liftCoefficient;
        Vector3 liftDirection = Vector3.Cross(airflow, transform.right).normalized;
        body.AddForce(liftDirection * lift, ForceMode.Acceleration);

        float airbrake = 1f + Controls.Brake * airbrakeDrag;
        float drag = dragPower * speed * speed * airbrake
                     + inducedDrag * Mathf.Abs(lift) * liftCoefficient * liftCoefficient;
        body.AddForce(-airflow * drag, ForceMode.Acceleration);

        float slide = -localVelocity.x * speed * sideForce;
        body.AddRelativeForce(new Vector3(slide, 0f, 0f), ForceMode.Acceleration);
    }

    private void ApplyRotation(Vector3 localVelocity)
    {
        float airflowStrength = Mathf.Clamp01(localVelocity.magnitude / fullControlSpeed);

        // Local axes: +X pitches nose down, +Y yaws right, -Z rolls right.
        Vector3 steering = new Vector3(
            Controls.Pitch * pitchTorque,
            Controls.Yaw * yawTorque,
            -Controls.Roll * rollTorque);
        Vector3 torque = steering * (airflowStrength * airflowStrength);

        float sideslip = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;
        torque.x += (AngleOfAttack - trimAngleOfAttack) * Mathf.Deg2Rad * stability.x * airflowStrength;
        torque.y += sideslip * Mathf.Deg2Rad * stability.y * airflowStrength;

        Vector3 localSpin = transform.InverseTransformDirection(body.angularVelocity);
        torque -= Vector3.Scale(localSpin, angularDamping);

        body.AddRelativeTorque(torque, ForceMode.Acceleration);
    }
}
