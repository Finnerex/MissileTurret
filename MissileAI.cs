﻿using System;
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

        if (player is null)
        {
            MissileTurret.TheLogger.LogInfo("it was the player that is null");
            _rigidbody.MovePosition(t.position + forward * 0.0005f);
            return;
        }
        
        _aliveTimeSeconds += Time.deltaTime;

        if (_currentLaunchTime > 0)
        {
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
        // only exists on the server anyway so this means nothing?
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer) return;

        NetworkHandler.Instance.ExplodeClientRpc(transform.position, KillRange, DamageRange);
        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
        
    }

    
    // idk man
    // [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
    // static void SubscribeToHandler()
    // {
    //     MissileTurret.TheLogger.LogInfo("This happende (subscribe) !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    //     NetworkHandler.ExplodeEvent += ExplodeOnClients;
    // }
    //
    // [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
    // static void UnsubscribeFromHandler()
    // {
    //     MissileTurret.TheLogger.LogInfo("This happende (unload) !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    //     NetworkHandler.ExplodeEvent -= ExplodeOnClients;
    // }
    
}