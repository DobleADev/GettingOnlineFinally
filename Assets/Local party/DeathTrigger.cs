using UnityEngine;
using UnityEngine.Networking;

public class DeathTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Asegúrate de que el objeto que colisionó sea un jugador
        LanPlayer player = other.GetComponent<LanPlayer>();
        if (player != null)
        {
            // Solo el jugador local debe morir y reaparecer
            if (player.isLocalPlayer)
            {
                Debug.Log("Jugador local ha tocado un DeathTrigger. Reiniciando...");
                player.CmdRespawnPlayer();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}