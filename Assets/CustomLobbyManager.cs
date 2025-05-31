// Script: CustomLobbyManager.cs (Intento de reevaluación del flujo)
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CustomLobbyManager : NetworkLobbyManager
{
    // Bandera para indicar si el juego ya "empezó" para el host.
    private bool gameStartedForHost = false;

    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("Host started. Lobby scene will load first for host.");
        gameStartedForHost = false; // Reiniciar la bandera
        // NO llamamos a ServerChangeScene(playScene) aquí.
        // Queremos que el host pase por el lobby como si fuera un cliente normal.
    }

    public override void OnLobbyServerConnect(NetworkConnection conn)
    {
        base.OnLobbyServerConnect(conn);
        Debug.Log("OnLobbyServerConnect called for connection ID: " + conn.connectionId);
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        // En este punto, NetworkLobbyManager ya ha decidido que un jugador debe ser añadido.
        // Si el host ya inició la partida y está en la escena de juego,
        // necesitamos añadir GamePlayer directamente para clientes remotos.

        // Identifica la conexión local del host (la más probable que cause el ready duplicado)
        bool isLocalHostClient = (NetworkManager.singleton.client != null && conn == NetworkManager.singleton.client.connection);

        if (isLocalHostClient)
        {
            // Este es el cliente local (el host mismo).
            // Le permitimos pasar por el flujo normal del LobbyManager para su lobbyPlayer
            // y luego lo moveremos a la escena de juego.
            Debug.Log("OnServerAddPlayer: Handling local host client.");
            base.OnServerAddPlayer(conn, playerControllerId); // Crea el LobbyPlayer para el host local
        }
        else // Es un cliente remoto
        {
            // Si el juego ya ha comenzado para el host (está en la playScene),
            // entonces este es un cliente que se une tarde.
            if (gameStartedForHost && SceneManager.GetActiveScene().name == playScene)
            {
                Debug.Log("OnServerAddPlayer: Handling remote late joiner. Spawning GamePlayer directly.");

                // Solo llamar SetClientReady para clientes remotos.
                NetworkServer.SetClientReady(conn);

                GameObject gamePlayer = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                NetworkServer.AddPlayerForConnection(conn, gamePlayer, playerControllerId);
            }
            else // Es un cliente remoto que se une mientras el host AÚN está en la escena de lobby
            {
                Debug.Log("OnServerAddPlayer: Handling remote client in lobby. Spawning LobbyPlayer.");
                base.OnServerAddPlayer(conn, playerControllerId); // Crea el LobbyPlayer para el cliente remoto
            }
        }
    }

    // Este método se llama en el servidor DESPUÉS de que un cliente se conecta y el lobbyPlayer es creado/gestionado.
    public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
    {
        Debug.Log("OnLobbyServerSceneLoadedForPlayer called for connection ID: " + lobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient.connectionId);

        // Identifica si es el cliente local (host) o un cliente remoto.
        bool isLocalHostClient = (NetworkManager.singleton.client != null && lobbyPlayer.GetComponent<NetworkIdentity>().connectionToClient == NetworkManager.singleton.client.connection);

        if (isLocalHostClient)
        {
            // Si es el cliente local (host), y no hemos iniciado el juego todavía para él.
            if (!gameStartedForHost)
            {
                Debug.Log("OnLobbyServerSceneLoadedForPlayer: Host's lobby player ready. Starting game for host.");
                gameStartedForHost = true; // Marca que el juego ha comenzado para el host.

                // Aquí es donde el host salta a la escena de juego.
                // Esta llamada debería ser la que le dice al NetworkLobbyManager que el "juego ha comenzado".
                ServerChangeScene(playScene);

                // No necesitamos llamar base.OnLobbyServerSceneLoadedForPlayer aquí para el host,
                // ya que ServerChangeScene lo maneja. Devolvemos true.
                return true;
            }
        }
        else // Es un cliente remoto
        {
            // Si el juego ya ha comenzado para el host (es decir, el host está en la playScene),
            // entonces este cliente remoto también debe ir a la playScene.
            if (gameStartedForHost && SceneManager.GetActiveScene().name == playScene)
            {
                Debug.Log("OnLobbyServerSceneLoadedForPlayer: Remote client joining existing game. Forcing Game Scene.");

                // Destruimos el lobbyPlayer, ya que el GamePlayer ya fue (o será) añadido en OnServerAddPlayer.
                NetworkServer.Destroy(lobbyPlayer);

                // Aquí podemos asegurarnos de que el cliente remoto cambie de escena.
                // Ya que estamos en la playScene, y el cliente es SetClientReady,
                // la base.OnLobbyServerSceneLoadedForPlayer() debería orquestar esto.
                return base.OnLobbyServerSceneLoadedForPlayer(lobbyPlayer, gamePlayer);
            }
        }

        // Para cualquier otro caso, o si el juego no ha empezado, usamos la lógica base del lobby.
        Debug.Log("OnLobbyServerSceneLoadedForPlayer: Default lobby logic. Calling base.");
        return base.OnLobbyServerSceneLoadedForPlayer(lobbyPlayer, gamePlayer);
    }


    public override void OnLobbyClientSceneChanged(NetworkConnection conn)
    {
        base.OnLobbyClientSceneChanged(conn);
        Debug.Log("Client: Scene changed to " + SceneManager.GetActiveScene().name);

        if (SceneManager.GetActiveScene().name == playScene)
        {
            Debug.Log("Client is now in the Game Scene.");
        }
    }
}