using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MissileTurret;

public class MissileAI : NetworkBehaviour
{
    public Transform player;

    private float _speed = 0f;

    private float _currentLaunchTime;
    private readonly float launchTimeSeconds = 0.4f;

    private float _aliveTimeSeconds;
    
    private Rigidbody _rigidbody;

    public static float MaxTurnSpeed = 1f;
    public static float MaxSpeed = 0.7f;

    public static float KillRange = 1f;
    public static float DamageRange = 5f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _currentLaunchTime = launchTimeSeconds;
        // NetworkHandler.LevelEvent += 
        
        MissileTurret.TheLogger.LogInfo($"New Missile - rb: {_rigidbody}, ...");
    }

    private void Update()
    {

        Transform t = transform;
        Vector3 forward = t.forward;

        _aliveTimeSeconds += Time.deltaTime;

        if (_currentLaunchTime > 0)
        {
            MissileTurret.TheLogger.LogInfo($"target: {player.name}");
            _currentLaunchTime -= Time.deltaTime;
            t.position += t.up * (_speed * 1.5f);
            _speed += 0.0004f;
        }
        else if (_speed < MaxSpeed)
            _speed += 0.001f;
        
        
        _rigidbody.MovePosition(t.position + forward * _speed);

        
        Vector3 between = player.position + Vector3.up - t.position;

        Quaternion rotationToTarget = Quaternion.LookRotation(between);
        t.rotation = Quaternion.Lerp(t.rotation, rotationToTarget, between.magnitude * (MaxTurnSpeed / 100));


        if (_aliveTimeSeconds > 10)
            EndIt();
        
    }

    private void OnCollisionEnter(Collision other)
    {
        EndIt();
    }

    private void EndIt()
    {
        Landmine.SpawnExplosion(transform.position, true, KillRange, DamageRange);
        
        if (!NetworkManager.IsHost && !NetworkManager.IsServer) return;

        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }
}