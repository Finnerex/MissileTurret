using System;
using GameNetcodeStuff;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace MissileTurret;


public class MissileTurretAI : NetworkBehaviour
{
    public Transform rod;
    public GameObject missile;
    // public Light laser;
    
    public static float RotationRange = 40f;
    public static float RotationSpeed = 0.15f;
    private float _currentRotationSpeed = 0.15f;

    private enum MissileTurretState
    {
        SEARCHING,
        FIRING,
        CHARGING
    }

    private MissileTurretState _state;
    private MissileTurretState _lastState;

    [CanBeNull] private PlayerControllerB _targetPlayer;
    private RaycastHit _lastHit;
    
    private float _currentReloadTime = 0f;
    public static float ReloadTimeSeconds = 5f;

    private float _currentChargeTime = 0f;
    public static float ChargeTimeSeconds = 0.8f;

    public AudioSource acquireTargetAudio;

    private void Awake()
    {
        _currentReloadTime = ReloadTimeSeconds;
        _currentChargeTime = ChargeTimeSeconds;
        _currentRotationSpeed = RotationSpeed;
    }

    private void Update()
    {
        MissileTurretState? stateToChangeTo = null;
        
        if (_targetPlayer is null)
        {
            stateToChangeTo = MissileTurretState.SEARCHING;
        }
        
        switch (_state)
        {
            case MissileTurretState.SEARCHING:

                // if (_lastState != MissileTurretState.SEARCHING)
                //     MissileTurret.TheLogger.LogInfo("searching");


                if (Physics.Raycast(rod.position + rod.up, rod.up, out RaycastHit hit, 30, 1051400,
                        QueryTriggerInteraction.Ignore) && hit.transform.CompareTag("Player"))
                {
                    if (hit.collider != _lastHit.collider)
                    {
                        _targetPlayer = hit.transform.GetComponent<PlayerControllerB>();
                        _lastHit = hit;
                    }

                    stateToChangeTo = MissileTurretState.CHARGING;
            
                    // MissileTurret.TheLogger.LogInfo("Player line of sight");
                }
                else
                {
                    _targetPlayer = null;
                    stateToChangeTo = MissileTurretState.SEARCHING;
                }
                
                if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                {
                    rod.Rotate(Vector3.forward, _currentRotationSpeed);
                    
                    float yaw = rod.localEulerAngles.z;

                    if ((yaw > RotationRange && yaw <= 180 && _currentRotationSpeed > 0) ||
                        (yaw < 360 - RotationRange && yaw > 180 && _currentRotationSpeed < 0))
                        _currentRotationSpeed = -_currentRotationSpeed;
                    
                    SetYawClientRpc(yaw);
                }
                
                break;
            
            case MissileTurretState.CHARGING:

                if (_lastState != MissileTurretState.CHARGING)
                {
                    acquireTargetAudio.Play();
                    // laser.enabled = true;
                }

                if (_currentChargeTime <= 0)
                {
                    _currentChargeTime = ChargeTimeSeconds;
                    stateToChangeTo = MissileTurretState.FIRING;
                }
                else
                    _currentChargeTime -= Time.deltaTime;

                break;
            
            case MissileTurretState.FIRING:
                
                if (_targetPlayer is null)
                    return;

                if (_lastState != MissileTurretState.FIRING)
                {
                    // laser.enabled = false;
                    
                    // check if can still have line of sight
                    if (!Physics.Raycast(rod.position + rod.forward, _targetPlayer.transform.position - rod.position,
                            out hit, 30, 1051400, QueryTriggerInteraction.Ignore) || !hit.transform.CompareTag("Player"))
                    {
                        stateToChangeTo = MissileTurretState.SEARCHING;
                        break;
                    }
                    
                    // MissileTurret.TheLogger.LogInfo($"firing at player {_targetPlayer.name}");
                    if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                    {
                        MissileAI ai = Instantiate(MissileTurret.MissilePrefab, rod.position + Vector3.up, Quaternion.LookRotation(rod.up)).GetComponent<MissileAI>();
                        ai.player = _targetPlayer.transform;
                        ai.GetComponent<NetworkObject>().Spawn(true); // fo sho fo real
                        
                        missile.SetActive(false);
                        ToggleMissileClientRpc(false);
                    }
                    
                }
                
                if (_currentReloadTime <= 0)
                {
                    if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                    {
                        missile.SetActive(true);
                        ToggleMissileClientRpc(true);
                    }

                    _currentReloadTime = ReloadTimeSeconds;
                    stateToChangeTo = MissileTurretState.SEARCHING;
                }
                else
                    _currentReloadTime -= Time.deltaTime;

                break;

        }
        
        _lastState = _state;
        if (stateToChangeTo != null && stateToChangeTo != _state &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
        {
            _state = stateToChangeTo.Value;
            SetStateClientRpc(_state);
        }

    }

    [ClientRpc]
    private void SetStateClientRpc(MissileTurretState state)
    {
        _state = state;
    }

    [ClientRpc]
    private void SetYawClientRpc(float yaw)
    {
        Vector3 angles = rod.localEulerAngles;
        angles.z = yaw;
        rod.localEulerAngles = angles;
    }

    [ClientRpc]
    private void ToggleMissileClientRpc(bool active)
    {
        missile.SetActive(active);
    }
    
}

