using UnityEngine;
using UnityEngine.Networking;
using System.Collections; // Necesario para IEnumerator

public class AutoNetworkConnector : MonoBehaviour
{
    public NetworkManager manager;
    public float connectionAttemptDuration = 3f; // Tiempo para intentar la conexión

    void Start()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<NetworkManager>();
            if (manager == null)
            {
                Debug.LogError("NetworkManager no encontrado en la escena. Asegúrate de tener uno.");
                return;
            }
        }

        // Establece la dirección de red a localhost por defecto
        manager.networkAddress = "localhost";

        // Inicia la rutina para intentar conectarse o ser host
        StartCoroutine(TryConnectOrCreateHost());
    }

    IEnumerator TryConnectOrCreateHost()
    {
        Debug.Log("Intentando conectarse a la partida existente en localhost...");
        manager.StartClient(); // Intentamos conectarnos como cliente

        float startTime = Time.time;
        // Espera un tiempo para ver si la conexión tiene éxito
        while (Time.time < startTime + connectionAttemptDuration)
        {
            // isConnectedClient es true una vez que el cliente está completamente conectado y listo
            if (NetworkClient.active && manager.client.isConnected)
            {
                Debug.Log("Conectado exitosamente como cliente.");
                yield break; // Salir de la corrutina si se conecta
            }
            yield return null; // Espera un frame antes de reintentar
        }

        // Si después del tiempo de espera no se conectó, significa que no hay host
        Debug.Log("No se encontró partida en localhost. Convirtiéndose en Host.");
        manager.StopClient(); // Asegúrate de detener el intento de cliente si no se conectó
        manager.StartHost(); // Inicia la partida como host
    }

    // Opcional: Métodos para manejar eventos del NetworkManager
    // Estos se llaman automáticamente por el NetworkManager si están presentes
    public void OnClientConnect(NetworkClient client)
    {
        Debug.Log("Cliente conectado al servidor.");
    }

    public void OnClientDisconnect(NetworkClient client)
    {
        Debug.Log("Cliente desconectado del servidor.");
        // Si quieres que el cliente intente reconectarse o busque un nuevo host, puedes llamar a TryConnectOrCreateHost() aquí.
    }

    public void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("Servidor: Nuevo cliente conectado desde " + conn.address);
    }

    public void OnServerDisconnect(NetworkConnection conn)
    {
        Debug.Log("Servidor: Cliente desconectado desde " + conn.address);
    }

    public void OnStartHost()
    {
        Debug.Log("Host iniciado.");
    }

    public void OnStartClient(NetworkClient client)
    {
        Debug.Log("Cliente iniciando...");
    }
}