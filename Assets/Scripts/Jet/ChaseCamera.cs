using UnityEngine;
using UnityEngine.InputSystem;

// Orbits around the center of the target jet and rolls along with it.
// Look around with the mouse or the right stick; the view returns behind the jet on its own.
public class ChaseCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -22f);
    [SerializeField] private float aimHeight = 2f;
    [SerializeField] private float rotationSharpness = 6f;

    [Header("Look Around")]
    [SerializeField] private Vector2 maxLookAngles = new Vector2(180f, 60f);
    [SerializeField] private float mouseSensitivity = 0.15f;
    [Tooltip("Seconds without moving the mouse before the view returns behind the jet.")]
    [SerializeField] private float recenterDelay = 1.5f;
    [SerializeField] private float lookSharpness = 10f;

    private InputAction mouseLook;
    private InputAction stickLook;
    private Quaternion followRotation;
    private Vector2 lookAngles;
    private float lastMouseLookTime;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public void Snap()
    {
        if (target == null)
        {
            return;
        }

        followRotation = target.rotation;
        lookAngles = Vector2.zero;
        PlaceCamera();
    }

    private void Awake()
    {
        mouseLook = new InputAction(type: InputActionType.Value, binding: "<Mouse>/delta");
        stickLook = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/rightStick", processors: "stickDeadzone");
    }

    private void OnEnable()
    {
        mouseLook.Enable();
        stickLook.Enable();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        mouseLook.Disable();
        stickLook.Disable();
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDestroy()
    {
        mouseLook.Dispose();
        stickLook.Dispose();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateLookAngles();
        followRotation = Quaternion.Slerp(followRotation, target.rotation, Blend(rotationSharpness));
        PlaceCamera();
    }

    private void UpdateLookAngles()
    {
        Vector2 stick = stickLook.ReadValue<Vector2>();
        Vector2 mouse = mouseLook.ReadValue<Vector2>();
        if (mouse != Vector2.zero)
        {
            lastMouseLookTime = Time.time;
        }

        Vector2 targetAngles = Vector2.zero;
        if (stick != Vector2.zero)
        {
            targetAngles = Vector2.Scale(stick, maxLookAngles);
        }
        else if (Time.time - lastMouseLookTime < recenterDelay)
        {
            targetAngles = Vector2.Min(maxLookAngles, Vector2.Max(-maxLookAngles, lookAngles + mouse * mouseSensitivity));
        }

        lookAngles = Vector2.Lerp(lookAngles, targetAngles, Blend(lookSharpness));
    }

    private void PlaceCamera()
    {
        Quaternion orbit = followRotation * Quaternion.Euler(-lookAngles.y, lookAngles.x, 0f);
        Vector3 aimPoint = target.position + orbit * Vector3.up * aimHeight;

        transform.position = target.position + orbit * offset;
        transform.rotation = Quaternion.LookRotation(aimPoint - transform.position, orbit * Vector3.up);
    }

    private static float Blend(float sharpness)
    {
        return 1f - Mathf.Exp(-sharpness * Time.deltaTime);
    }
}
