// Script: CustomNetworkDiscovery.cs (Sin cambios, sigue siendo el mismo)
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class CustomNetworkDiscovery : NetworkDiscovery
{
    public delegate void OnServerFound(string fromAddress, string data);
    public event OnServerFound OnServerFoundEvent;

    private Dictionary<string, string> foundServers = new Dictionary<string, string>();

    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        if (!foundServers.ContainsKey(fromAddress) || foundServers[fromAddress] != data)
        {
            foundServers[fromAddress] = data;
            if (OnServerFoundEvent != null)
            {
                Debug.Log(fromAddress + data);
                OnServerFoundEvent.Invoke(fromAddress, data);
            }
        }
    }

    public void ClearFoundServers()
    {
        foundServers.Clear();
    }
}