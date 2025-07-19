using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con UI Text
using System.Net;     // Necesario para obtener la IP
using System.Net.Sockets; // Necesario para sockets

public class SetTextIpAddress : MonoBehaviour
{
    public Text ipAddressText; // Asigna tu UI Text a este campo en el Inspector

    void Start()
    {
        // Asegúrate de que el componente Text esté asignado
        if (ipAddressText == null)
        {
            Debug.LogError("No se ha asignado un componente UI Text al script DisplayIPAddress.");
            return;
        }

        string ip = GetLocalIPAddress();
        if (!string.IsNullOrEmpty(ip))
        {
            ipAddressText.text = "IP Local: " + ip;
            Debug.Log("IP Local asignada a la UI: " + ip);
        }
        else
        {
            ipAddressText.text = "No se pudo obtener la IP";
            Debug.LogWarning("No se pudo obtener la dirección IP local del equipo.");
        }
    }

    // Método para obtener la IP de red local (reutilizado de AutoNetworkDiscovery)
    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // Busca una dirección IPv4
            {
                // Filtra direcciones de loopback (127.0.0.1) y otras no deseadas en LAN
                if (ip.ToString().StartsWith("192.168.") || ip.ToString().StartsWith("10.") || (ip.ToString().StartsWith("172.") && IsPrivate172Ip(ip.ToString())))
                {
                    return ip.ToString();
                }
            }
        }
        return string.Empty; // Devuelve una cadena vacía si no se encuentra una IP válida
    }

    // Helper para el rango 172.16.0.0 - 172.31.255.255
    private bool IsPrivate172Ip(string ipAddress)
    {
        string[] parts = ipAddress.Split('.');
        if (parts.Length == 4 && int.TryParse(parts[1], out int octet2))
        {
            return octet2 >= 16 && octet2 <= 31;
        }
        return false;
    }
}
