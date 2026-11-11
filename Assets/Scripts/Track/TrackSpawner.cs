using UnityEngine;

// Add to every track scene via the TrackSetup prefab.
// Spawns the player's jet, aims the camera at it and respawns it after a crash.
public class TrackSpawner : MonoBehaviour
{
    [SerializeField] private JetController jetPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private ChaseCamera chaseCamera;

    private void Start()
    {
        JetController jet = Instantiate(jetPrefab);
        jet.Crashed += Respawn;
        Respawn(jet);
    }

    private void Respawn(JetController jet)
    {
        jet.Place(spawnPoint.position, spawnPoint.rotation);
        chaseCamera.Target = jet.transform;
        chaseCamera.Snap();
    }
}
