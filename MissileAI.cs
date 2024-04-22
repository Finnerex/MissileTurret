using System;
using HarmonyLib;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MissileTurret;

public class MissileAI : NetworkBehaviour
{
    public Transform player; // this jawng is null on client? but this shouldn't exist on client so idk man

    private float _speed = 0f;

    private float _currentLaunchTime;
    private readonly float launchTimeSeconds = 0.4f;

    private float _aliveTimeSeconds;
    
    private Rigidbody _rigidbody;

    public static float MaxTurnSpeed = 1f;
    public static float MaxSpeed = 0.7f;
    public static float Acceleration = 0.001f;

    public static float KillRange = 1f;
    public static float DamageRange = 5f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _currentLaunchTime = launchTimeSeconds;
    }

    private void Update()
    {

        Transform t = transform;
        Vector3 forward = t.forward;

        _aliveTimeSeconds += Time.deltaTime;

        if (_currentLaunchTime > 0)
        {
            _currentLaunchTime -= Time.deltaTime;
            t.position += t.up * (_speed * 1.5f);
            _speed += 0.0004f * Time.deltaTime;
        }
        else if (_speed < MaxSpeed)
            _speed += Acceleration * Time.deltaTime;

        _rigidbody.MovePosition(t.position + forward * (_speed * Time.deltaTime));

        
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Vector3 between = player.position + Vector3.up - t.position;

            Quaternion rotationToTarget = Quaternion.LookRotation(between);
            t.rotation = Quaternion.Lerp(t.rotation, rotationToTarget, between.magnitude * (MaxTurnSpeed * Time.deltaTime));
        }

        if (_aliveTimeSeconds > 10)
            EndIt();
        
    }

    private void OnCollisionEnter(Collision other)
    {
        EndIt();
    }

    private void EndIt()
    {
        // only exists on the server anyway so this means nothing?
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) return;

        ExplodeClientRpc(transform.position, KillRange, DamageRange);

        var net = GetComponent<NetworkObject>();
        
        if (net is not null && net.IsSpawned)
            net.Despawn();
        
        Destroy(gameObject);
    }
    
    [ClientRpc]
    public void ExplodeClientRpc(Vector3 position, float killRange, float damageRange)
    {
        Landmine.SpawnExplosion(position, true, killRange, damageRange);
    }

}