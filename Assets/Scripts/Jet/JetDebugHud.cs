using UnityEngine;

// Temporary flight info for testing, until the real HUD exists.
[RequireComponent(typeof(JetController))]
public class JetDebugHud : MonoBehaviour
{
    private JetController jet;
    private GUIStyle style;

    private void Awake()
    {
        jet = GetComponent<JetController>();
    }

    private void OnGUI()
    {
        style ??= new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };

        string info =
            $"Speed:    {jet.Speed * 3.6f:0} km/h\n" +
            $"Altitude: {transform.position.y:0} m\n" +
            $"Throttle: {jet.Throttle * 100f:0}%\n" +
            $"AoA:      {jet.AngleOfAttack:0.0}°";

        if (jet.IsStalling)
        {
            info += "\n<color=red><b>STALL</b></color>";
        }

        GUI.Label(new Rect(20, 20, 400, 200), info, style);
        GUI.Label(new Rect(20, Screen.height - 40, 900, 30), "W/S pitch  A/D roll  Q/E yaw  Shift/Ctrl throttle", style);
    }
}
