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
    public Transform rail;
    public GameObject missile;
    public GameObject laser;
    
    public static float RotationRange;
    public static float RotationSpeed;
    private float _currentRotationSpeed;

    private enum MissileTurretState
    {
        SEARCHING,
        FIRING,
        CHARGING,
        DISABLED
    }

    private MissileTurretState _state;
    private MissileTurretState _lastState;

    [CanBeNull] private PlayerControllerB _targetPlayer;
    private RaycastHit _lastHit;
    
    private float _currentReloadTime;
    public static float ReloadTimeSeconds;

    private float _currentChargeTime;
    public static float ChargeTimeSeconds;

    public AudioSource acquireTargetAudio;
    public AudioSource disableAudio;
    public AudioSource enableAudio;

    private float _currentDisableTime = 0f;
    public static float DisableTimeSeconds = 8f;

    private void Awake()
    {
        _currentReloadTime = ReloadTimeSeconds;
        _currentChargeTime = ChargeTimeSeconds;
        _currentRotationSpeed = RotationSpeed;
        _currentDisableTime = DisableTimeSeconds;

        TerminalAccessibleObject tao = GetComponent<TerminalAccessibleObject>();
        tao.terminalCodeEvent.AddListener(this, typeof(MissileTurretAI).GetMethod(nameof(DisableTurret)));

        laser.SetActive(false);
        ToggleLaserClientRpc(false);
        
    }

    private void Update()
    {
        MissileTurretState? stateToChangeTo = null; // nullable is dumb here but i dont feel like changing it
        
        
        // if (_targetPlayer is null && _state != MissileTurretState.DISABLED)
        // {
        //     stateToChangeTo = MissileTurretState.SEARCHING;
        //     laser.SetActive(false);
        //     ToggleLaserClientRpc(false);
        //     MissileTurret.TheLogger.LogInfo("The laser was toggled OFF IN THE BENINGING");
        // }
        
        switch (_state)
        {
            case MissileTurretState.SEARCHING:

                if (Physics.Raycast(rod.position + rod.up, rod.up, out RaycastHit hit, 30, 1051400,
                        QueryTriggerInteraction.Ignore) && hit.transform.CompareTag("Player"))
                {
                    if (hit.collider != _lastHit.collider)
                    {
                        _targetPlayer = hit.transform.GetComponent<PlayerControllerB>();
                        _lastHit = hit;
                    }

                    stateToChangeTo = MissileTurretState.CHARGING;
                }
                else
                {
                    _targetPlayer = null;
                    stateToChangeTo = MissileTurretState.SEARCHING;
                }
                
                if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                {
                    rod.Rotate(Vector3.forward, _currentRotationSpeed * Time.deltaTime);
                    
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
                    if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                    {
                        laser.SetActive(true);
                        ToggleLaserClientRpc(true);
                    }
                }

                if (_currentChargeTime <= 0)
                {
                    _currentChargeTime = ChargeTimeSeconds;
                    stateToChangeTo = MissileTurretState.FIRING;
                    
                    if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                    {
                        laser.SetActive(false);
                        ToggleLaserClientRpc(false);
                    }
                }
                else
                    _currentChargeTime -= Time.deltaTime;

                break;
            
            case MissileTurretState.FIRING:

                if (_targetPlayer is null)
                {
                    stateToChangeTo = MissileTurretState.SEARCHING;
                    break;
                }

                if (_lastState != MissileTurretState.FIRING)
                {

                    // check if can still have line of sight
                    if (!Physics.Raycast(rod.position + rod.forward, _targetPlayer.transform.position - rod.position,
                            out hit, 30, 1051400, QueryTriggerInteraction.Ignore) || !hit.transform.CompareTag("Player"))
                    {
                        stateToChangeTo = MissileTurretState.SEARCHING;
                        break;
                    }
                    
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
            
            case MissileTurretState.DISABLED:

                if (_lastState != MissileTurretState.DISABLED)
                {
                    disableAudio.Play();

                    if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                    {
                        Vector3 angles = rail.localEulerAngles;
                        angles.x = -30;
                        rail.localEulerAngles = angles;

                        Vector3 pos = rail.localPosition;
                        pos.y = -0.67f;
                        rail.localPosition = pos;

                        SetRailClientRpc(angles, pos);
                    }
                    
                }

                if (_currentDisableTime <= 0)
                {
                    _currentDisableTime = DisableTimeSeconds;
                    enableAudio.Play();

                    if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                    {
                        Vector3 angles = rail.localEulerAngles;
                        angles.x = 0;
                        rail.localEulerAngles = angles;
                        
                        Vector3 pos = rail.localPosition;
                        pos.y = 0;
                        rail.localPosition = pos;
                        
                        SetRailClientRpc(angles, pos);
                    }

                    stateToChangeTo = MissileTurretState.SEARCHING;
                }
                else
                    _currentDisableTime -= Time.deltaTime;
                

                break;

        }
        
        _lastState = _state;

        if (stateToChangeTo != null && stateToChangeTo != _state &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
        {
            _state = stateToChangeTo.Value;
            SetStateClientRpc(_state, _lastState);
        }

    }

    public void DisableTurret(PlayerControllerB pc)
    {
        if (pc is null)
            return;

        _lastState = _state;
        _state = MissileTurretState.DISABLED;
        SetStateClientRpc(_state, _lastState);
    }

    [ClientRpc]
    private void SetStateClientRpc(MissileTurretState state, MissileTurretState lastState)
    {
        _state = state;
        _lastState = lastState;
    }

    [ClientRpc]
    private void SetYawClientRpc(float yaw)
    {
        Vector3 angles = rod.localEulerAngles;
        angles.z = yaw;
        rod.localEulerAngles = angles;
    }
    
    [ClientRpc]
    private void SetRailClientRpc(Vector3 localEulers, Vector3 localPos)
    {
        rail.localPosition = localPos;
        rail.localEulerAngles = localEulers;
    }

    [ClientRpc]
    private void ToggleMissileClientRpc(bool active)
    {
        missile.SetActive(active);
    }

    [ClientRpc]
    private void ToggleLaserClientRpc(bool active) // this might not be necessary at all
    {
        laser.SetActive(active);
    }
    
}

