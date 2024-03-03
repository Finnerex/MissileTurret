using System;
using Unity.Netcode;
using UnityEngine;

namespace MissileTurret;

public class NetworkHandler : NetworkBehaviour
{
    
    public static NetworkHandler Instance { get; private set; }
    
    // public static event Action<Vector3> ExplodeEvent; 
    
    
    public override void OnNetworkSpawn()
    {
        // ExplodeEvent = null;
        
        MissileTurret.TheLogger.LogInfo("THIS ALSO SHOULD HAVE HAPPENED");

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
        Instance = this;

        base.OnNetworkSpawn();
    }

    [ClientRpc]
    public void ExplodeClientRpc(Vector3 position, float killRange, float damageRange)
    {
        MissileTurret.TheLogger.LogInfo("This happende (explode) !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Landmine.SpawnExplosion(position, true, killRange, damageRange); // If the event has subscribers (does not equal null), invoke the event
    }

    
}