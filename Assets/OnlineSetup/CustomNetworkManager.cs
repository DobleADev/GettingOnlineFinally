// Script: CustomNetworkManager.cs
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement; // Para la gestión de escenas

public class CustomNetworkManager : NetworkManager
{
    // Eventos personalizados para que LobbyUIHandler se suscriba.
    // Esto es lo que el NetworkManager no provee directamente con += en 2017.2.
    public delegate void NetworkEvent();
    public event NetworkEvent OnClientConnectedEvent;
    public event NetworkEvent OnClientDisconnectedEvent;
    public event NetworkEvent OnHostStartedEvent;
    public event NetworkEvent OnHostStoppedEvent;

    // Métodos virtuales que sobrescribimos del NetworkManager base
    // Estos se ejecutan cuando ocurren los eventos de red.

    // Llamado en el servidor cuando el host se inicia.
    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("CustomNetworkManager: Host started successfully.");
        // Disparar nuestro evento personalizado para la UI.
        if (OnHostStartedEvent != null)
        {
            OnHostStartedEvent.Invoke();
        }
        // Nota: NetworkManager.StartHost() ya carga automáticamente la "Online Scene"
        // (la escena de juego) una vez que el host se inicia.
    }

    // Llamado en el servidor cuando el host se detiene.
    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("CustomNetworkManager: Host stopped.");
        if (OnHostStoppedEvent != null)
        {
            OnHostStoppedEvent.Invoke();
        }
    }

    // Llamado en el cliente cuando se conecta al servidor.
    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        Debug.Log("CustomNetworkManager: Client connected to server. Connection ID: " + conn.connectionId);
        if (OnClientConnectedEvent != null)
        {
            OnClientConnectedEvent.Invoke();
        }
        // Nota: NetworkManager base se encargará de cargar la "Online Scene"
        // para el cliente una vez que la conexión sea exitosa y el servidor
        // esté en esa escena.
    }

    // Llamado en el cliente cuando se desconecta del servidor.
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        Debug.Log("CustomNetworkManager: Client disconnected from server.");
        if (OnClientDisconnectedEvent != null)
        {
            OnClientDisconnectedEvent.Invoke();
        }
    }

    // Llamado en el servidor cuando un cliente (incluido el host local) se conecta
    // y necesita un Player GameObject.
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        // En este punto, la escena de juego ya debería estar cargada si somos host
        // o si un cliente remoto se une a una partida en curso.
        Debug.Log("CustomNetworkManager: OnServerAddPlayer for connection ID: " + conn.connectionId);

        // Identificamos si es el cliente local (el host mismo).
        // Usamos NetworkManager.singleton.client.connection como identificador fiable en 2017.2.
        bool isLocalHostClient = (NetworkManager.singleton.client != null && conn == NetworkManager.singleton.client.connection);

        // Solo llamar SetClientReady si la conexión no es la conexión local (el host mismo).
        // La conexión local ya es manejada como "lista" por StartHost().
        if (!isLocalHostClient)
        {
            NetworkServer.SetClientReady(conn);
            Debug.Log("CustomNetworkManager: Called SetClientReady for remote client ID: " + conn.connectionId);
        }
        else
        {
            Debug.Log("CustomNetworkManager: Skipping SetClientReady for local host client (already ready).");
        }

        // Instancia el prefab del GamePlayer.
        // Asegúrate de que 'playerPrefab' (Online Player Prefab en el Inspector) esté asignado.
        GameObject gamePlayer = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, gamePlayer, playerControllerId);
        Debug.Log("CustomNetworkManager: GamePlayer spawned and added for client ID: " + conn.connectionId);
    }
}