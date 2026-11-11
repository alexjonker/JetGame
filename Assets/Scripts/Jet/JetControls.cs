// One frame of input for a jet. Kept small so it can be sent over the network.
[System.Serializable]
public struct JetControls
{
    public float Pitch;      // -1..1, positive = nose down
    public float Roll;       // -1..1, positive = roll right
    public float Yaw;        // -1..1, positive = yaw right
    public float Accelerate; // 0..1
    public float Brake;      // 0..1
}
