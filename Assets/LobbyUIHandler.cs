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
        // Asegúrate de obtener la referencia a tu CustomNetworkManager
        if (customNetworkManager == null)
        {
            customNetworkManager = FindObjectOfType<CustomNetworkManager>();
            if (customNetworkManager == null)
            {
                Debug.LogError("LobbyUIHandler: CustomNetworkManager not found in scene!");
                return;
            }
        }
        if (customNetworkDiscovery == null)
        {
            customNetworkDiscovery = FindObjectOfType<CustomNetworkDiscovery>();
            if (customNetworkDiscovery == null)
            {
                Debug.LogError("LobbyUIHandler: CustomNetworkDiscovery not found in scene!");
                return;
            }
        }

        // Suscribirse a los EVENTOS PERSONALIZADOS de CustomNetworkManager
        customNetworkManager.OnClientConnectedEvent += OnClientConnected;
        customNetworkManager.OnClientDisconnectedEvent += OnClientDisconnected;
        customNetworkManager.OnHostStartedEvent += OnHostStarted;
        customNetworkManager.OnHostStoppedEvent += OnHostStopped;

        // Suscribirse al evento de descubrimiento de servidores
        customNetworkDiscovery.OnServerFoundEvent += AddGameEntryToList;

    }

    void OnDestroy()
    {
        // Desuscribirse para evitar errores y fugas de memoria
        if (customNetworkDiscovery != null)
        {
            customNetworkDiscovery.OnServerFoundEvent -= AddGameEntryToList;
        }

        if (customNetworkManager != null)
        {
            customNetworkManager.OnClientConnectedEvent -= OnClientConnected;
            customNetworkManager.OnClientDisconnectedEvent -= OnClientDisconnected;
            customNetworkManager.OnHostStartedEvent -= OnHostStarted;
            customNetworkManager.OnHostStoppedEvent -= OnHostStopped;
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

    // --- Game Entry UI Management (Sin cambios aquí) ---

	private void AddGameEntryToList(string fromAddress, string data)
	{
		if (listedGameEntries.ContainsKey(fromAddress))
		{
			listedGameEntries[fromAddress].GetComponentInChildren<Text>().text = "Game: " + data + " (" + fromAddress + ")";
			return;
		}

		GameObject entry = Instantiate(gameEntryPrefab, contentPanel);
		entry.name = "GameEntry_" + fromAddress;

		Text gameInfoText = entry.GetComponentInChildren<Text>();
		Button joinButton = entry.GetComponentInChildren<Button>();

		gameInfoText.text = "Game: " + data + " (" + fromAddress + ")";

		joinButton.onClick.AddListener(() => OnClickJoinGame(fromAddress));

		listedGameEntries.Add(fromAddress, entry);
		Debug.Log("Added game entry: " + data + " (" + fromAddress + ")");
	}

    private void ClearGameEntries()
    {
        foreach (var entry in new List<GameObject>(listedGameEntries.Values))
        {
            Destroy(entry);
        }
        listedGameEntries.Clear();
    }
}