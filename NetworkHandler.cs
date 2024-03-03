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

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
        Instance = this;

        base.OnNetworkSpawn();
    }

    [ClientRpc]
    public void ExplodeClientRpc(Vector3 position, float killRange, float damageRange)
    {
        Landmine.SpawnExplosion(position, true, killRange, damageRange);
    }
    
    
}