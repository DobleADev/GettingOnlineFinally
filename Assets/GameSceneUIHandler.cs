// Script: GameSceneUIHandler.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GameSceneUIHandler : MonoBehaviour
{
    public Button exitGameButton;
    public CustomNetworkManager customNetworkManager; // <-- La referencia a tu CustomNetworkManager

    void Start()
    {
        if (exitGameButton == null)
        {
            Debug.LogError("ExitGameButton not assigned in GameSceneUIHandler Inspector!");
            return;
        }

        if (customNetworkManager == null)
        {
            // Como CustomNetworkManager es DontDestroyOnLoad, FindObjectOfType siempre lo encontrará si está en la escena.
            customNetworkManager = FindObjectOfType<CustomNetworkManager>();
            if (customNetworkManager == null)
            {
                Debug.LogError("CustomNetworkManager not found in GameSceneUIHandler!");
                return;
            }
        }

        exitGameButton.onClick.AddListener(OnClickExitGame);
    }

    void OnClickExitGame()
    {
        Debug.Log("Attempting to exit game...");

        if (customNetworkManager == null)
        {
            Debug.LogError("CustomNetworkManager is NULL in OnClickExitGame! Cannot stop network.");
            return;
        }

        // Si somos el host (servidor activo), detenemos el host.
        if (NetworkServer.active)
        {
            customNetworkManager.StopHost();
            Debug.Log("Stopped Host.");
        }
        // Si somos solo un cliente, nos desconectamos.
        else if (customNetworkManager.client != null && customNetworkManager.client.isConnected)
        {
            customNetworkManager.StopClient();
            Debug.Log("Stopped Client.");
        }
        else
        {
            // En caso de que no haya una conexión activa (ej. si este script se activa por error),
            // simplemente cargamos la escena offline.
            Debug.Log("No active network connection, returning to offline scene.");
            // Asegúrate de que customNetworkManager.offlineScene esté configurado correctamente en el Inspector.
            SceneManager.LoadScene(customNetworkManager.offlineScene);
        }

        // El NetworkManager base se encargará de cargar la offlineScene
        // y el OnEnable de LobbyUIHandler se encargará de re-inicializar NetworkDiscovery.
    }
}