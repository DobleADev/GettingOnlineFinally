using UnityEngine;
using UnityEngine.Networking;

public class Checkpoint : MonoBehaviour
{
    public int checkpointID;

    void OnTriggerEnter(Collider other)
    {
        // Asegúrate de que el objeto que colisionó sea un jugador
        LanPlayer player = other.GetComponent<LanPlayer>();
        if (player != null)
        {
            // Solo el jugador local debe actualizar su propio checkpoint
            if (player.isLocalPlayer)
            {
                player.CmdSetSafePoint(transform.position);
                Debug.Log("Checkpoint " + checkpointID + " alcanzado por el jugador local.");
            }
        }
    }
}