using UnityEngine;
using UnityEngine.InputSystem;

// Reads keyboard and gamepad for the local player.
[RequireComponent(typeof(JetController))]
public class PlayerJetInput : MonoBehaviour
{
    [Tooltip("Flip pitch so W / stick up climbs instead of dives.")]
    [SerializeField] private bool invertPitch;

    private JetController jet;
    private InputAction pitch;
    private InputAction roll;
    private InputAction yaw;
    private InputAction accelerate;
    private InputAction brake;
    private InputAction[] allActions;

    private void Awake()
    {
        jet = GetComponent<JetController>();

        pitch = CreateAxis("<Keyboard>/s", "<Keyboard>/w");
        pitch.AddBinding("<Gamepad>/leftStick/y").WithProcessor("axisDeadzone");

        roll = CreateAxis("<Keyboard>/a", "<Keyboard>/d");
        roll.AddBinding("<Gamepad>/leftStick/x").WithProcessor("axisDeadzone");

        yaw = CreateAxis("<Keyboard>/q", "<Keyboard>/e");
        yaw.AddCompositeBinding("1DAxis")
            .With("Negative", "<Gamepad>/leftShoulder")
            .With("Positive", "<Gamepad>/rightShoulder");

        accelerate = CreateTrigger("<Keyboard>/shift", "<Gamepad>/rightTrigger");
        brake = CreateTrigger("<Keyboard>/ctrl", "<Gamepad>/leftTrigger");

        allActions = new[] { pitch, roll, yaw, accelerate, brake };
    }

    private void OnEnable()
    {
        foreach (InputAction action in allActions)
        {
            action.Enable();
        }
    }

    private void OnDisable()
    {
        foreach (InputAction action in allActions)
        {
            action.Disable();
        }
    }

    private void OnDestroy()
    {
        foreach (InputAction action in allActions)
        {
            action.Dispose();
        }
    }

    private void Update()
    {
        jet.Controls = new JetControls
        {
            Pitch = pitch.ReadValue<float>() * (invertPitch ? -1f : 1f),
            Roll = roll.ReadValue<float>(),
            Yaw = yaw.ReadValue<float>(),
            Accelerate = accelerate.ReadValue<float>(),
            Brake = brake.ReadValue<float>(),
        };
    }

    private static InputAction CreateAxis(string negativeKey, string positiveKey)
    {
        var action = new InputAction(type: InputActionType.Value, expectedControlType: "Axis");
        action.AddCompositeBinding("1DAxis")
            .With("Negative", negativeKey)
            .With("Positive", positiveKey);
        return action;
    }

    private static InputAction CreateTrigger(string key, string gamepadTrigger)
    {
        var action = new InputAction(type: InputActionType.Value, expectedControlType: "Axis");
        action.AddBinding(key);
        action.AddBinding(gamepadTrigger);
        return action;
    }
}
