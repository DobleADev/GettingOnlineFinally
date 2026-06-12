// Script: LobbyUIHandler.cs (Actualizado para usar CustomNetworkManager)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Sigue siendo necesario para NetworkDiscovery, etc.
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LobbyUIHandler : MonoBehaviour
{
    // Ahora referencia a nuestro CustomNetworkManager
    public CustomNetworkManager customNetworkManager;
    public CustomNetworkDiscovery customNetworkDiscovery;

    [Header("UI References")]
    public InputField gameNameInputField;
    public GameObject gameEntryPrefab;
    public Transform contentPanel;
    public GameObject lobbyPanel; // Panel que contiene los elementos del lobby (InputField, botones, ScrollView)
    public GameObject connectingPanel; // Panel que se muestra mientras se conecta/espera

    [Header("Game Scene Settings")]
    public string gameSceneName = "GameScene"; // Nombre de tu escena de juego (también en CustomNetworkManager Inspector)

    private Dictionary<string, GameObject> listedGameEntries = new Dictionary<string, GameObject>();

    void Start()
    {
        // La lógica de asignación inicial de customNetworkManager y customNetworkDiscovery.
        // Esto es para la primera carga de la escena.
        if (customNetworkManager == null)
        {
            customNetworkManager = FindObjectOfType<CustomNetworkManager>();
        }
        if (customNetworkDiscovery == null)
        {
            customNetworkDiscovery = FindObjectOfType<CustomNetworkDiscovery>();
        }

        // Ya tienes los null checks, lo cual es bueno.
        // Los eventos de red deben ser suscritos aquí O en OnEnable,
        // y desuscritos en OnDisable/OnDestroy para evitar fugas.
        // Lo más seguro es OnEnable/OnDisable si el GameObject se activa/desactiva.
        // Si el GameObject LobbyUIHandler nunca se desactiva, Start/OnDestroy es suficiente.
        // Dado que tu problema es al recargar la escena, OnEnable/OnDisable es la mejor opción.

        // ShowLobbyPanel();
        // Iniciar el refresh al principio de la escena del lobby.
        // Si se llama en Start(), se asegura que solo se haga una vez por carga de escena.
        // OnClickRefreshGames();
    }


    void OnEnable() // Se llama cada vez que el GameObject se activa (incluyendo la carga de escena)
    {
        // Re-obtener las referencias y re-suscribirse a eventos cada vez que el LobbyUIHandler se activa.
        // Esto es vital si el CustomNetworkManager persiste con DontDestroyOnLoad.

        if (customNetworkManager == null)
        {
            customNetworkManager = FindObjectOfType<CustomNetworkManager>();
        }
        if (customNetworkDiscovery == null)
        {
            customNetworkDiscovery = FindObjectOfType<CustomNetworkDiscovery>();
        }

        // Asegúrate de que las referencias no sean null antes de suscribir.
        if (customNetworkManager != null)
        {
            customNetworkManager.OnClientConnectedEvent += OnClientConnected;
            customNetworkManager.OnClientDisconnectedEvent += OnClientDisconnected;
            customNetworkManager.OnHostStartedEvent += OnHostStarted;
            customNetworkManager.OnHostStoppedEvent += OnHostStopped;
            Debug.Log("LobbyUIHandler: Subscribed to CustomNetworkManager events.");
        }
        else
        {
            Debug.LogError("LobbyUIHandler: CustomNetworkManager is NULL in OnEnable. Check scene setup.");
        }

        if (customNetworkDiscovery != null)
        {
            customNetworkDiscovery.OnServerFoundEvent += AddGameEntryToList;
            Debug.Log("LobbyUIHandler: Subscribed to CustomNetworkDiscovery events.");
        }
        else
        {
            Debug.LogError("LobbyUIHandler: CustomNetworkDiscovery is NULL in OnEnable. Check scene setup.");
        }
    }

    void OnDisable() // Se llama cada vez que el GameObject se desactiva (o se destruye)
    {
        // Desuscribirse para evitar fugas de memoria. Esto es MUY importante.
        if (customNetworkManager != null)
        {
            customNetworkManager.OnClientConnectedEvent -= OnClientConnected;
            customNetworkManager.OnClientDisconnectedEvent -= OnClientDisconnected;
            customNetworkManager.OnHostStartedEvent -= OnHostStarted;
            customNetworkManager.OnHostStoppedEvent -= OnHostStopped;
            Debug.Log("LobbyUIHandler: Unsubscribed from CustomNetworkManager events.");
        }

        if (customNetworkDiscovery != null)
        {
            customNetworkDiscovery.OnServerFoundEvent -= AddGameEntryToList;
            Debug.Log("LobbyUIHandler: Unsubscribed from CustomNetworkDiscovery events.");
            // Si el NetworkDiscovery está corriendo al deshabilitarse la UI del lobby,
            // esto podría ser un buen lugar para detenerlo para evitar que siga escuchando.
            // Pero ShowLobbyPanel y OnClientDisconnected/OnHostStopped ya manejan esto.
            // Solo si la escena se destruye por completo o si se desactiva el objeto
            // sin un cambio de escena/desconexión explícito.
            // if (customNetworkDiscovery.running) customNetworkDiscovery.StopBroadcast();
        }
    }

    // --- UI Control ---
    void ShowLobbyPanel()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (connectingPanel != null) connectingPanel.SetActive(false);
    }

    void ShowConnectingPanel()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (connectingPanel != null) connectingPanel.SetActive(true);
    }

    // --- Custom NetworkManager Callbacks (a través de eventos) ---

    void OnClientConnected()
    {
        Debug.Log("LobbyUIHandler: Client Connected to Server (via event)!");
        // Ocultamos la UI del lobby, mostramos "conectando" hasta que la escena cambie.
        ShowConnectingPanel();
    }

    void OnClientDisconnected()
    {
        Debug.Log("LobbyUIHandler: Client Disconnected from Server (via event). Returning to lobby.");
        ShowLobbyPanel();
        ClearGameEntries();
        customNetworkDiscovery.ClearFoundServers();
        customNetworkDiscovery.StopBroadcast();
    }

    void OnHostStarted()
    {
        Debug.Log("LobbyUIHandler: Host started successfully (via event). Loading game scene.");
        // El host siempre va directo a la escena de juego.
        ShowConnectingPanel(); // Mostrar panel de carga/espera
        // customNetworkManager.StopHost();
    }

    void OnHostStopped()
    {
        Debug.Log("LobbyUIHandler: Host stopped (via event). Returning to lobby.");
        ShowLobbyPanel();
        ClearGameEntries();
        customNetworkDiscovery.ClearFoundServers();
        customNetworkDiscovery.StopBroadcast();
    }


    // --- UI Button Actions ---

    public void OnClickCreateGame()
    {
        string gameName = gameNameInputField.text;
        if (string.IsNullOrEmpty(gameName))
        {
            Debug.LogWarning("Please enter a game name!");
            return;
        }

        ClearGameEntries();
        customNetworkDiscovery.ClearFoundServers();

        // Iniciar como host a través de nuestro CustomNetworkManager
        customNetworkManager.StartHost(); // Esto llamará a CustomNetworkManager.OnStartHost()

        // Configurar el broadcastData antes de iniciar el servidor de descubrimiento
        customNetworkDiscovery.broadcastData = gameName;
        customNetworkDiscovery.Initialize();
        customNetworkDiscovery.StartAsServer(); // El host empieza a anunciar su presencia
        Debug.Log("Hosting game: " + gameName + " on " + customNetworkManager.networkAddress);

        ShowConnectingPanel(); // Mostrar que estamos en el proceso de hostear/cargar la partida.
    }

    public void OnClickJoinGame(string ipAddress)
    {
        customNetworkManager.networkAddress = ipAddress;
        customNetworkManager.StartClient(); // Esto llamará a CustomNetworkManager.OnClientConnect()
        Debug.Log("Attempting to join game at: " + ipAddress);

        ShowConnectingPanel(); // Mostrar panel de conexión.
    }

    public void OnClickRefreshListedGames()
    {
        if (!customNetworkDiscovery.running)
        {
            customNetworkDiscovery.Initialize();
            customNetworkDiscovery.StartAsClient();
            Debug.Log("Searching for games...");
        }
    }

    public void OnClickStopRefreshing()
    {
        if (customNetworkDiscovery.running)
        {
            Debug.Log("Searching stopped");
            customNetworkDiscovery.StopBroadcast();
            ClearGameEntries();
            customNetworkDiscovery.ClearFoundServers();
        }
    }

    private void AddGameEntryToList(string fromAddress, string data)
{
    // Usamos el 'data' (nombre del juego) como clave para la deduplicación en la UI.
    // Asume que 'data' es el nombre único de la partida.
    string gameIdentifier = data; // O una combinación de data y algo más si el nombre no es único.

    if (listedGameEntries.ContainsKey(gameIdentifier))
    {
        // Actualizamos la entrada existente.
        // Puedes decidir si quieres mostrar todas las IPs o solo la primera.
        // Para mostrar todas, podrías concatenarlas o listarlas en el texto.
        // Por ahora, solo actualizamos el texto, mostrando la IP más reciente o la primera.
        listedGameEntries[gameIdentifier].GetComponentInChildren<Text>().text = "Game: " + data + " (" + fromAddress + ")";
        Debug.Log("Updated existing game entry for '" + gameIdentifier + "' with IP: " + fromAddress);
        return;
    }

    // Si no existe, creamos una nueva entrada.
    GameObject entry = Instantiate(gameEntryPrefab, contentPanel);
    entry.name = "GameEntry_" + gameIdentifier; // Nombre del GameObject en la jerarquía

    Text gameInfoText = entry.GetComponentInChildren<Text>();
    Button joinButton = entry.GetComponentInChildren<Button>();

    gameInfoText.text = "Game: " + data + " (" + fromAddress + ")";

    // Guardamos la IP asociada al botón de unirse.
    // Aquí hay una decisión: si hay múltiples IPs para el mismo juego, ¿cuál usar?
    // La más sencilla es usar la última recibida, o la que el host prefiera para la conexión.
    // En tu caso, el OnClickJoinGame ya toma la IP.
    joinButton.onClick.RemoveAllListeners(); // Limpiamos para evitar duplicados si se actualiza.
    joinButton.onClick.AddListener(() => OnClickJoinGame(fromAddress));

    listedGameEntries.Add(gameIdentifier, entry);
    Debug.Log("Added new game entry: '" + gameIdentifier + "' from IP: " + fromAddress);
}

    private void ClearGameEntries()
    {
        foreach (var entry in new List<GameObject>(listedGameEntries.Values))
        {
            Destroy(entry);
        }
        listedGameEntries.Clear();
        Debug.Log("Cleared all game entries from UI.");
    }

    public void OnClickRefreshGames()
    {
        Debug.Log("Refreshing game list...");

        // 1. Siempre detén cualquier broadcast anterior si está corriendo.
        if (customNetworkDiscovery.running)
        {
            customNetworkDiscovery.StopBroadcast();
            Debug.Log("Stopped previous NetworkDiscovery broadcast.");
        }

        // 2. Limpia la lista de partidas del UI Y del NetworkDiscovery.
        ClearGameEntries(); // <-- Asegúrate de que esto se llama para destruir GameObjects.
        customNetworkDiscovery.ClearFoundServers(); // <-- Limpia el diccionario interno de NetworkDiscovery.

        // 3. Re-inicializa NetworkDiscovery.
        customNetworkDiscovery.Initialize();
        Debug.Log("NetworkDiscovery Initialized.");

        // 4. Inicia NetworkDiscovery como cliente para buscar partidas.
        customNetworkDiscovery.StartAsClient();
        Debug.Log("Searching for games as client.");
    }
}