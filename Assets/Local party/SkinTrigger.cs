using UnityEngine;
using UnityEngine.Networking;

public class SkinTrigger : MonoBehaviour
{
    public int skinIndexToApply = 0;

    void OnTriggerEnter(Collider other)
    {
        LanPlayer player = other.GetComponent<LanPlayer>();
        if (player != null)
        {
            // Solo el jugador local debe iniciar el cambio de skin
            if (player.isLocalPlayer)
            {
                Debug.Log("Jugador local ha tocado un SkinTrigger. Cambiando a skin " + skinIndexToApply + ".");
                player.CmdChangeSkin(skinIndexToApply);
            }
        }
    }
}