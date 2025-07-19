using UnityEngine;
using UnityEngine.Networking; // Necesario para UNET
using UnityEngine.UI; // Opcional, para visualizar el estado
using System.Net;     // Necesario para obtener la IP
using System.Net.Sockets; // Necesario para sockets

public class AutoNetworkDiscovery : NetworkDiscovery
{
    // --- Configuración ---
    public NetworkManager networkManager;
    public float searchTimeout = 5f; // Tiempo en segundos para buscar hosts antes de convertirse en host

    // --- Variables Internas ---
    private float searchTimer;
    private bool isSearching = false;
    private bool hostFound = false;

    // --- UI Elements (Opcional para depuración visual) ---
    public Text statusText;

    void Start()
    {
        // Asegúrate de que el NetworkManager está asignado en el Inspector
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager no encontrado en la escena. Asegúrate de tener uno.");
                return;
            }
        }

        string localIP = GetLocalIPAddress();
        if (string.IsNullOrEmpty(localIP))
        {
            Debug.LogError("No se pudo obtener la dirección IP local. Asegúrate de estar conectado a una red.");
            UpdateStatus("Error: No se pudo obtener la IP local.");
            return;
        }

        networkManager.networkAddress = localIP;
        // Inicializa NetworkDiscovery con los parámetros configurados en el Inspector
        Initialize();

        // Inicia el proceso de búsqueda automática
        StartAutoDiscovery();
    }

    void Update()
    {
        if (isSearching)
        {
            searchTimer -= Time.deltaTime;
            // UpdateStatus($"Buscando servidores... Tiempo restante: {searchTimer:F1}s");

            if (searchTimer <= 0 && !hostFound)
            {
                // El tiempo de búsqueda ha terminado y no encontramos ningún host
                Debug.Log("Tiempo de búsqueda agotado. No se encontraron hosts. Convirtiéndose en Host.");
                UpdateStatus("No se encontraron servidores. Iniciando como Host...");
                StartHosting();
                isSearching = false; // Detiene el bucle de búsqueda
            }
        }
    }

    // --- Métodos de Control ---

    public void StartAutoDiscovery()
    {
        if (running) StopBroadcast(); // Asegúrate de detener cualquier operación anterior
        hostFound = false; // Reinicia el estado de búsqueda
        isSearching = true; // Habilita el temporizador en Update
        searchTimer = searchTimeout; // Resetea el temporizador

        // Configura NetworkDiscovery para escuchar (cliente)
        StartAsClient(); 
        UpdateStatus("Iniciando búsqueda automática...");
        Debug.Log("Iniciando búsqueda automática de hosts...");
    }

    public void StartHosting()
    {
        StopBroadcast(); // Detén cualquier búsqueda pendiente
        isSearching = false; // Asegúrate de que el temporizador no siga corriendo

        // *** CAMBIO CLAVE AQUÍ: Obtener la IP de red local ***
        string localIP = GetLocalIPAddress();
        if (string.IsNullOrEmpty(localIP))
        {
            Debug.LogError("No se pudo obtener la dirección IP local. Asegúrate de estar conectado a una red.");
            UpdateStatus("Error: No se pudo obtener la IP local.");
            return;
        }

        // Asigna los datos a transmitir: IP de red local + puerto del NetworkManager
        // broadcastData = "NetworkManager:" + localIP + ":" + networkManager.networkPort;
        // networkManager.networkAddress = localIP; // Opcional, si quieres que el NetworkManager también use esta IP

        // Inicia NetworkDiscovery como servidor (transmisor)
        StartAsServer(); 
        
        // Inicia el NetworkManager como Host (Servidor + Cliente)
        networkManager.StartHost(); 
        UpdateStatus("Host iniciado y transmitiendo...");
        Debug.Log($"Host iniciado en {localIP}:{networkManager.networkPort} y transmitiendo...");
    }

    // Método llamado automáticamente por NetworkDiscovery cuando se recibe una transmisión
    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        if (hostFound) return; // Ya hemos encontrado un host, ignorar otras transmisiones
        
        Debug.Log($"Servidor encontrado desde: {fromAddress}, Datos: {data}");
        UpdateStatus($"Servidor encontrado: {fromAddress}");
        
        hostFound = true; // Marca que hemos encontrado un host
        isSearching = false; // Detiene el temporador de búsqueda

        // Detén la escucha de transmisiones una vez que encuentres un host
        StopBroadcast(); 

        // Conectar al servidor encontrado
        string[] addressAndPort = data.Split(':');
        if (addressAndPort.Length == 2)
        {
            networkManager.networkAddress = addressAndPort[0];
            networkManager.networkPort = int.Parse(addressAndPort[1]);
            networkManager.StartClient(); // Conéctate como cliente al servidor
            UpdateStatus($"Conectando a {networkManager.networkAddress}:{networkManager.networkPort}...");
            Debug.Log($"Conectando a {networkManager.networkAddress}:{networkManager.networkPort}...");
        }
        else
        {
            Debug.LogError("Formato de datos de transmisión inesperado: " + data);
            // Si el formato es inesperado, quizás deberíamos seguir buscando o volver a intentar
            hostFound = false; 
            StartAsClient(); // Volver a escuchar si el data estaba mal
        }
    }

    // --- Utilidades ---

    // Nuevo método para obtener la IP de red local
    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // Busca una dirección IPv4
            {
                // Filtra direcciones de loopback (127.0.0.1) o aquellas que no son de red local
                if (ip.ToString().StartsWith("192.168.") || ip.ToString().StartsWith("10.") || ip.ToString().StartsWith("172.16."))
                {
                    return ip.ToString();
                }
            }
        }
        // Si no encuentra una IP de red local, puede devolver localhost como fallback (o una cadena vacía)
        // Para este caso, es mejor que devuelva una cadena vacía para que el error sea evidente.
        Debug.LogWarning("No se encontró una dirección IP de red local válida. Usando 127.0.0.1 como fallback.");
        return "127.0.0.1"; // Fallback, aunque preferimos una IP real para LAN
        // return string.Empty; // Podrías devolver string.Empty y manejar el error en StartHosting
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }

    void OnApplicationQuit()
    {
        // Asegúrate de detener todo al cerrar la aplicación
        StopBroadcast();
        if (networkManager != null)
        {
            networkManager.StopHost();
        }
    }
}