// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Modified for NPC behavior with behavior tree system and advanced vision + Special Points System + Leader Functionality

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.AnimationBaseLocomotion.NPC
{
    public class NPCAnimationController : MonoBehaviour
    {
        #region Enums

        private enum AnimationState
        {
            Base,
            Locomotion,
            Jump,
            Fall,
            Crouch
        }

        private enum GaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        public enum NPCGaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        #endregion

        #region Animation Variable Hashes

        private readonly int _movementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private readonly int _movementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private readonly int _movementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private readonly int _shuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private readonly int _shuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");
        private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");
        private readonly int _isJumpingAnimHash = Animator.StringToHash("IsJumping");
        private readonly int _fallingDurationHash = Animator.StringToHash("FallingDuration");
        private readonly int _inclineAngleHash = Animator.StringToHash("InclineAngle");
        private readonly int _strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int _strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");
        private readonly int _forwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private readonly int _cameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int _isTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");
        private readonly int _isCrouchingHash = Animator.StringToHash("IsCrouching");
        private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int _isStoppedHash = Animator.StringToHash("IsStopped");
        private readonly int _isStartingHash = Animator.StringToHash("IsStarting");
        private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int _leanValueHash = Animator.StringToHash("LeanValue");
        private readonly int _headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int _headLookYHash = Animator.StringToHash("HeadLookY");
        private readonly int _bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int _bodyLookYHash = Animator.StringToHash("BodyLookY");
        private readonly int _locomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");

        #endregion

        #region Serialized Fields

        [Header("Core Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _controller;

        [Header("Movement Settings")]
        [SerializeField] private bool _alwaysStrafe = false;
        [SerializeField] private float _walkSpeed = 1.4f;
        [SerializeField] private float _runSpeed = 2.5f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _speedChangeDamping = 10f;
        [SerializeField] private float _rotationSmoothing = 10f;

        [Header("Path Following")]
        [SerializeField] private bool _enablePathFollowing = true;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private bool _loopWaypoints = true;
        [SerializeField] private float _waypointReachDistance = 1f;
        [SerializeField] private float _waypointWaitTime = 2f;

        [Header("Special Points System")]
        [SerializeField] private Transform[] _specialPoints;
        [SerializeField] private float _specialPointDetectionRange = 5f;
        [SerializeField] private float _specialPointReachDistance = 1f;
        [SerializeField] private float _specialPointStayTime = 10f;
        [SerializeField] private float _specialPointActivationChance = 0.3f;
        [SerializeField] private float _specialPointCooldownTime = 10f;
        [SerializeField] private bool _enableSpecialPoints = true;

        [Header("Detection Settings")]
        [SerializeField] private LayerMask _obstacleLayerMask = -1;
        [SerializeField] private float _obstacleDetectionDistance = 1.0f;
        [SerializeField] private float _obstacleDetectionAngle = 20f;
        [SerializeField] private int _obstacleRayCount = 2;
        [SerializeField] private float _playerDetectionAngle = 60f;
        [SerializeField] private float _playerDetectionDistance = 10f;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private LayerMask _blockingLayerMask = -1;

        [Header("Leader Settings")]
        [SerializeField] private bool _isLeader = false;
        [SerializeField] private float _penaltyCooldown = 3f;
        [SerializeField] private bool _enablePenaltyDebug = true;
        [SerializeField] private ParticleSystem _penaltyParticleSystem; // Added: used to play penalty particle effect

        [Header("Vision System")]
        [SerializeField] private bool _enableHeadTurn = true;
        [SerializeField] private float _maxHeadRotationAngle = 60f;
        [SerializeField] private float _headRotationSpeed = 45f;
        [SerializeField] private float _scanningSpeed = 30f;
        [SerializeField] private float _scanPauseTime = 1f;
        [SerializeField] private float _walkScanRange = 25f;
        [SerializeField] private float _walkScanSpeed = 20f;
        [SerializeField] private float _playerLockBreakAngle = 80f;
        [SerializeField] private float _stationaryScanInterval = 8f;
        [SerializeField] private float _stationaryScanDuration = 4f;

        [Header("Physics Settings")]
        [SerializeField] private Transform _rearRayPos;
        [SerializeField] private Transform _frontRayPos;
        [SerializeField] private LayerMask _groundLayerMask;
        [SerializeField] private float _groundedOffset = -0.14f;
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private float _gravityMultiplier = 2f;

        #endregion

        #region Private Variables

        // Animation state
        private AnimationState _currentState = AnimationState.Base;
        private GaitState _currentGait;
        private bool _isGrounded = true;
        private bool _isWalking;
        private bool _isSprinting;
        private bool _isCrouching;
        private bool _isStarting;
        private bool _isStopped = true;
        private bool _isStrafing;
        private bool _isTurningInPlace;

        // Movement
        private Vector3 _moveDirection;
        private Vector3 _velocity;
        private Vector3 _targetVelocity;
        private float _speed2D;
        private float _currentMaxSpeed;
        private float _targetMaxSpeed;

        // Animation values
        private float _headLookX;
        private float _headLookY;
        private float _bodyLookX;
        private float _bodyLookY;
        private float _leanValue;
        private float _inclineAngle;
        private float _locomotionStartDirection;
        private float _locomotionStartTimer;
        private float _fallingDuration;
        private float _fallStartTime;

        // Input simulation
        private bool _movementInputHeld;
        private bool _movementInputPressed;
        private bool _movementInputTapped;

        // Vision system
        private float _currentHeadAngle = 0f;
        private float _targetHeadAngle = 0f;
        private bool _isPlayerLocked = false;
        private bool _isScanning = false;
        private bool _isScanningStationary = false;
        private float _scanTimer = 0f;
        private float _scanDirection = 1f;
        private float _lastStationaryScanTime = 0f;
        private float _stationaryScanTimer = 0f;
        private float _walkScanTimer = 0f;

        // Detection
        [Header("NPC Status (Debug)")]
        [Tooltip("Is player currently detected?")]
        [SerializeField] private bool _hasPlayerInSight = false;
        [Tooltip("Is obstacle detected ahead?")]
        [SerializeField] private bool _hasObstacleAhead = false;
        [Tooltip("Currently detected player transform")]
        [SerializeField] private Transform _detectedPlayer;
        [Tooltip("Is player blocked by obstacles?")]
        [SerializeField] private bool _isPlayerBlocked = false;
        [Tooltip("Is player currently slacking at work?")]
        [SerializeField] private bool _isPlayerSlacking = false;

        // Leader penalty system
        private float _lastPenaltyTime = -999f;

        // Path following
        private int _currentWaypointIndex = 0;
        private float _waypointWaitTimer = 0f;
        private bool _isWaitingAtWaypoint = false;
        private bool _isFollowingPath = false;

        // Special Points System
        [Header("Special Points Status (Debug)")]
        [Tooltip("Is currently going to a special point?")]
        [SerializeField] private bool _isGoingToSpecialPoint = false;
        [Tooltip("Is currently at a special point?")]
        [SerializeField] private bool _isAtSpecialPoint = false;
        [Tooltip("Current special point being targeted")]
        [SerializeField] private Transform _currentSpecialPoint;
        [Tooltip("Position to return to after visiting special point")]
        [SerializeField] private Vector3 _returnPosition;
        [Tooltip("Time remaining for special point cooldown")]
        [SerializeField] private float _specialPointCooldownRemaining = 0f;

        private int _returnWaypointIndex;
        private float _specialPointTimer = 0f;
        private float _lastSpecialPointCheckTime = 0f;
        private HashSet<Transform> _visitedSpecialPoints = new HashSet<Transform>();

        // Animation helpers
        private float _headLookDelay;
        private float _bodyLookDelay;
        private float _leanDelay;
        private Vector3 _currentRotation;
        private Vector3 _previousRotation;
        private float _rotationRate;
        private float _newDirectionDifferenceAngle;
        private float _strafeDirectionX;
        private float _strafeDirectionZ;
        private float _forwardStrafe = 1f;

        // Behavior tree
        private NPCBehaviorTree _behaviorTree;

        // Constants
        private const float _ANIMATION_DAMP_TIME = 5f;

        #endregion

        #region Public Properties

        public bool HasPlayerInSight => _hasPlayerInSight;
        public bool HasObstacleAhead => _hasObstacleAhead;
        public Transform DetectedPlayer => _detectedPlayer;
        public bool IsPlayerBlocked => _isPlayerBlocked;
        public bool IsPlayerSlacking => _isPlayerSlacking;
        public bool IsLeader => _isLeader;
        public bool IsMoving => _speed2D > 0.1f;
        public bool IsGrounded => _isGrounded;
        public float ObstacleDetectionDistance => _obstacleDetectionDistance;
        public LayerMask ObstacleLayerMask => _obstacleLayerMask;
        public bool IsFollowingPath => _isFollowingPath;
        public bool EnablePathFollowing => _enablePathFollowing;
        public Transform[] Waypoints => _waypoints;
        public bool IsScanning => _isScanning;
        public bool IsScanningStationary => _isScanningStationary;
        public bool IsPlayerLocked => _isPlayerLocked;

        // Special Points Properties
        public bool IsGoingToSpecialPoint => _isGoingToSpecialPoint;
        public bool IsAtSpecialPoint => _isAtSpecialPoint;
        public Transform CurrentSpecialPoint => _currentSpecialPoint;
        public bool EnableSpecialPoints => _enableSpecialPoints;
        public float SpecialPointCooldownRemaining => _specialPointCooldownRemaining;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _isStrafing = _alwaysStrafe;
            _behaviorTree = new NPCBehaviorTree(this);
            SwitchState(AnimationState.Locomotion);
        }

        private void Update()
        {
            UpdateVisionSystem();
            ScanForObstacles();
            ScanForPlayer();
            UpdatePathFollowing();
            UpdateSpecialPoints();

            _behaviorTree?.Update();

            switch (_currentState)
            {
                case AnimationState.Locomotion:
                    UpdateLocomotionState();
                    break;
                case AnimationState.Jump:
                    UpdateJumpState();
                    break;
                case AnimationState.Fall:
                    UpdateFallState();
                    break;
                case AnimationState.Crouch:
                    UpdateCrouchState();
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Obstacle detection visualization
            Gizmos.color = _hasObstacleAhead ? Color.red : Color.green;
            Vector3 leftBoundary = Quaternion.AngleAxis(-_obstacleDetectionAngle / 2f, Vector3.up) * transform.forward * _obstacleDetectionDistance;
            Vector3 rightBoundary = Quaternion.AngleAxis(_obstacleDetectionAngle / 2f, Vector3.up) * transform.forward * _obstacleDetectionDistance;
            Gizmos.DrawRay(transform.position, leftBoundary);
            Gizmos.DrawRay(transform.position, rightBoundary);

            // Player detection visualization
            if (_hasPlayerInSight)
            {
                if (_isPlayerSlacking && _isLeader)
                {
                    Gizmos.color = Color.red; // Leader spotted player slacking
                }
                else if (_isPlayerBlocked)
                {
                    Gizmos.color = Color.yellow; // Player is blocked
                }
                else
                {
                    Gizmos.color = Color.blue; // Player detected normally
                }
            }
            else
            {
                Gizmos.color = Color.gray;
            }

            Vector3 headDirection = GetHeadLookDirection();
            Vector3 leftPlayerBoundary = Quaternion.AngleAxis(-_playerDetectionAngle / 2f, Vector3.up) * headDirection * _playerDetectionDistance;
            Vector3 rightPlayerBoundary = Quaternion.AngleAxis(_playerDetectionAngle / 2f, Vector3.up) * headDirection * _playerDetectionDistance;
            Gizmos.DrawRay(transform.position, leftPlayerBoundary);
            Gizmos.DrawRay(transform.position, rightPlayerBoundary);

            // Detection range sphere
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _playerDetectionDistance);

            // If player is detected, draw a line to the player
            if (_detectedPlayer != null)
            {
                if (_isPlayerSlacking && _isLeader)
                {
                    Gizmos.color = Color.red;
                }
                else if (_isPlayerBlocked)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.blue;
                }
                Gizmos.DrawLine(transform.position + Vector3.up * 1.7f, _detectedPlayer.position + Vector3.up * 1f);
            }

            // Waypoints visualization
            if (_waypoints != null && _waypoints.Length > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    if (_waypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(_waypoints[i].position, 0.5f);
                        int nextIndex = (_loopWaypoints && i == _waypoints.Length - 1) ? 0 : i + 1;
                        if (nextIndex < _waypoints.Length && _waypoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(_waypoints[i].position, _waypoints[nextIndex].position);
                        }
                    }
                }
            }

            // Special points visualization
            if (_specialPoints != null && _specialPoints.Length > 0)
            {
                for (int i = 0; i < _specialPoints.Length; i++)
                {
                    if (_specialPoints[i] != null)
                    {
                        // Special point itself - magenta
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawWireSphere(_specialPoints[i].position, 0.8f);
                        Gizmos.DrawCube(_specialPoints[i].position + Vector3.up * 0.5f, Vector3.one * 0.3f);

                        // Detection range - semi-transparent orange
                        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                        Gizmos.DrawSphere(_specialPoints[i].position, _specialPointDetectionRange);

                        // Detection range boundary - red wireframe
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(_specialPoints[i].position, _specialPointDetectionRange);

                        // Reach range - red
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(_specialPoints[i].position, _specialPointReachDistance);
                    }
                }
            }

            // Line to current special point
            if (_currentSpecialPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, _currentSpecialPoint.position);
            }

            // Return position marker
            if (_isGoingToSpecialPoint || _isAtSpecialPoint)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_returnPosition, Vector3.one);
                Gizmos.DrawLine(transform.position, _returnPosition);
            }

            // Leader marker
            if (_isLeader)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.3f);
            }
        }

        #endregion

        #region Vision System

        private void UpdateVisionSystem()
        {
            // If waiting at a special point, do not scan or turn head; remain idle
            if (_isAtSpecialPoint)
            {
                _targetHeadAngle = 0f;
                _isScanning = false;
                _isScanningStationary = false;
                _isPlayerLocked = false;
                UpdateHeadRotation();
                return;
            }

            UpdateHeadRotation();

            if (_isPlayerLocked && _detectedPlayer != null)
            {
                UpdatePlayerTracking();
            }
            else if (_isScanningStationary)
            {
                UpdateStationaryScanning();
            }
            else if (IsMoving && !_hasPlayerInSight)
            {
                UpdateWalkingScanning();
            }
            else if (!IsMoving && !_hasPlayerInSight)
            {
                CheckForStationaryScanning();
            }
            else
            {
                _targetHeadAngle = 0f;
            }
        }

        private void UpdateHeadRotation()
        {
            if (!_enableHeadTurn) return;

            _targetHeadAngle = Mathf.Clamp(_targetHeadAngle, -_maxHeadRotationAngle, _maxHeadRotationAngle);
            float rotationDelta = _headRotationSpeed * Time.deltaTime;
            _currentHeadAngle = Mathf.MoveTowards(_currentHeadAngle, _targetHeadAngle, rotationDelta);
            _headLookX = _currentHeadAngle / _maxHeadRotationAngle;
        }

        private void UpdatePlayerTracking()
        {
            if (_detectedPlayer == null)
            {
                _isPlayerLocked = false;
                return;
            }

            Vector3 directionToPlayer = (_detectedPlayer.position - transform.position).normalized;
            float angleToPlayer = Vector3.SignedAngle(transform.forward, directionToPlayer, Vector3.up);

            if (Mathf.Abs(angleToPlayer) <= _maxHeadRotationAngle)
            {
                _targetHeadAngle = angleToPlayer;
            }
            else if (Mathf.Abs(angleToPlayer) > _playerLockBreakAngle)
            {
                _isPlayerLocked = false;
                _targetHeadAngle = 0f;
            }
        }

        private void UpdateWalkingScanning()
        {
            _walkScanTimer += Time.deltaTime;
            float scanAngle = Mathf.Sin(_walkScanTimer * _walkScanSpeed * Mathf.Deg2Rad) * _walkScanRange;
            _targetHeadAngle = scanAngle;
        }

        private void CheckForStationaryScanning()
        {
            if (Time.time - _lastStationaryScanTime > _stationaryScanInterval)
            {
                StartStationaryScanning();
            }
        }

        public void StartStationaryScanning()
        {
            _isScanningStationary = true;
            _isScanning = true;
            _stationaryScanTimer = 0f;
            _scanTimer = 0f;
            _scanDirection = 1f;
            _targetHeadAngle = -_maxHeadRotationAngle;
            _lastStationaryScanTime = Time.time;
        }

        public void StopStationaryScanning()
        {
            _isScanningStationary = false;
            _isScanning = false;
            _targetHeadAngle = 0f;
        }

        private void UpdateStationaryScanning()
        {
            _stationaryScanTimer += Time.deltaTime;

            if (_stationaryScanTimer >= _stationaryScanDuration)
            {
                StopStationaryScanning();
                return;
            }

            _scanTimer += Time.deltaTime;

            if (_scanTimer >= _scanPauseTime)
            {
                _scanTimer = 0f;

                if (_targetHeadAngle >= _maxHeadRotationAngle)
                {
                    _scanDirection = -1f;
                }
                else if (_targetHeadAngle <= -_maxHeadRotationAngle)
                {
                    _scanDirection = 1f;
                }

                _targetHeadAngle += _scanDirection * (_maxHeadRotationAngle * 0.5f);
                _targetHeadAngle = Mathf.Clamp(_targetHeadAngle, -_maxHeadRotationAngle, _maxHeadRotationAngle);
            }
        }

        public void LockOntoPlayer(Transform player)
        {
            _detectedPlayer = player;
            _isPlayerLocked = true;
            _isScanning = false;
            _isScanningStationary = false;
        }

        public void ReleasePlayerLock()
        {
            _isPlayerLocked = false;
            _detectedPlayer = null;
        }

        public Vector3 GetHeadLookDirection()
        {
            return Quaternion.AngleAxis(_currentHeadAngle, Vector3.up) * transform.forward;
        }

        #endregion

        #region Detection Systems

        private void ScanForObstacles()
        {
            _hasObstacleAhead = false;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            float angleStep = _obstacleDetectionAngle / (_obstacleRayCount - 1);
            float startAngle = -_obstacleDetectionAngle / 2f;

            for (int i = 0; i < _obstacleRayCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector3 rayDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * transform.forward;

                if (Physics.Raycast(rayOrigin, rayDirection, _obstacleDetectionDistance, _obstacleLayerMask))
                {
                    _hasObstacleAhead = true;
                    Debug.DrawRay(rayOrigin, rayDirection * _obstacleDetectionDistance, Color.red);
                }
                else
                {
                    Debug.DrawRay(rayOrigin, rayDirection * _obstacleDetectionDistance, Color.green);
                }
            }
        }

        private void ScanForPlayer()
        {
            _hasPlayerInSight = false;
            _isPlayerBlocked = false;
            _isPlayerSlacking = false;
            Transform previousPlayer = _detectedPlayer;
            _detectedPlayer = null;

            Vector3 detectionDirection = _enableHeadTurn ? GetHeadLookDirection() : transform.forward;
            Collider[] colliders = Physics.OverlapSphere(transform.position, _playerDetectionDistance);

            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag(_playerTag))
                {
                    Vector3 directionToPlayer = (collider.transform.position - transform.position).normalized;
                    float angleToPlayer = Vector3.Angle(detectionDirection, directionToPlayer);

                    if (angleToPlayer <= _playerDetectionAngle / 2f)
                    {
                        Vector3 eyePosition = transform.position + Vector3.up * 1.7f;
                        Vector3 playerCenter = collider.bounds.center;
                        Vector3 rayDirection = (playerCenter - eyePosition).normalized;
                        float distanceToPlayer = Vector3.Distance(eyePosition, playerCenter);

                        Debug.DrawRay(eyePosition, rayDirection * distanceToPlayer, Color.blue, 0.1f);

                        RaycastHit hit;
                        if (Physics.Raycast(eyePosition, rayDirection, out hit, distanceToPlayer, ~0))
                        {
                            if (hit.collider.CompareTag(_playerTag))
                            {
                                // Player is not blocked
                                _hasPlayerInSight = true;
                                _detectedPlayer = collider.transform;
                                _isPlayerBlocked = false;

                                // Check if player is slacking and apply penalty (leaders only)
                                CheckPlayerSlackingAndApplyPenalty(collider);

                                if (!_isPlayerLocked)
                                {
                                    LockOntoPlayer(collider.transform);
                                }

                                Debug.DrawLine(transform.position, collider.transform.position, Color.green, 0.1f);
                                break;
                            }
                            else
                            {
                                if (IsBlockingObject(hit.collider))
                                {
                                    // Player is blocked, cannot apply penalty
                                    _hasPlayerInSight = true;
                                    _detectedPlayer = collider.transform;
                                    _isPlayerBlocked = true;

                                    if (!_isPlayerLocked)
                                    {
                                        LockOntoPlayer(collider.transform);
                                    }

                                    Debug.DrawLine(transform.position, hit.point, Color.yellow, 0.1f);
                                    Debug.DrawLine(hit.point, collider.transform.position, Color.red, 0.1f);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Player is not blocked (no hit)
                            _hasPlayerInSight = true;
                            _detectedPlayer = collider.transform;
                            _isPlayerBlocked = false;

                            // Check if player is slacking and apply penalty (leaders only)
                            CheckPlayerSlackingAndApplyPenalty(collider);

                            if (!_isPlayerLocked)
                            {
                                LockOntoPlayer(collider.transform);
                            }

                            Debug.DrawLine(transform.position, collider.transform.position, Color.green, 0.1f);
                            break;
                        }
                    }
                }
            }

            if (previousPlayer != null && _detectedPlayer == null && _isPlayerLocked)
            {
                ReleasePlayerLock();
            }
        }

        /// <summary>
        /// Check if the player is slacking and apply a penalty (leaders only)
        /// </summary>
        /// <param name="playerCollider">Player's collider</param>
        private void CheckPlayerSlackingAndApplyPenalty(Collider playerCollider)
        {
            if (!_isLeader)
            {
                if (_enablePenaltyDebug) Debug.Log($"[NPC Leader {gameObject.name}] ❌ Not a leader, skipping penalty check.");
                return;
            }
            // Note: ScanForPlayer will not call this when the player is blocked, but the check is retained here for debugging purposes
            if (_isPlayerBlocked)
            {
                if (_enablePenaltyDebug) Debug.Log($"[NPC Leader {gameObject.name}] 🚧 Player is blocked, skipping penalty check.");
                return;
            }

            // Get the player's CharacterStatus component
            // Assume CharacterStatus contains isSlackingAtWork and ApplyPenalty()
            CharacterStatus characterStatus = playerCollider.GetComponent<CharacterStatus>();
            if (characterStatus != null && characterStatus.isSlackingAtWork)
            {
                _isPlayerSlacking = true;
                if (_enablePenaltyDebug) Debug.Log($"[NPC Leader {gameObject.name}] ✅ Player is slacking. Preparing cooldown check.");

                // Check penalty cooldown
                if (Time.time - _lastPenaltyTime >= _penaltyCooldown)
                {
                    if (_enablePenaltyDebug) Debug.Log($"[NPC Leader {gameObject.name}] ⏱️ Cooldown expired. Attempting to apply penalty to player...");

                    // Try to apply penalty to the player (assume ApplyPenalty() returns true on success)
                    bool penaltyApplied = characterStatus.ApplyPenalty();

                    if (penaltyApplied)
                    {
                        _lastPenaltyTime = Time.time;

                        // Play particle effect
                        if (_penaltyParticleSystem != null)
                        {
                            _penaltyParticleSystem.Play();
                            if (_enablePenaltyDebug) Debug.Log($"[NPC Leader {gameObject.name}] 🎉 Penalty applied! Particle effect Play() called.");
                        }
                        else if (_enablePenaltyDebug)
                        {
                            Debug.Log($"[NPC Leader {gameObject.name}] ⚠️ ParticleSystem reference missing! Cannot play effect. Please assign it in the Inspector.");
                        }

                        if (_enablePenaltyDebug)
                        {
                            Debug.Log($"[NPC Leader {gameObject.name}] 🚨 Player slacking detected! Penalty applied.");
                        }
                    }
                    else if (_enablePenaltyDebug)
                    {
                        Debug.Log($"[NPC Leader {gameObject.name}] ⚠️ Penalty failed (ApplyPenalty() returned false). Possibly insufficient funds or other reason.");
                    }
                }
                else if (_enablePenaltyDebug)
                {
                    float remainingCooldown = _penaltyCooldown - (Time.time - _lastPenaltyTime);
                    Debug.Log($"[NPC Leader {gameObject.name}] ⏳ Penalty on cooldown, remaining time: {remainingCooldown:F2}s.");
                }
            }
            else
            {
                // If NPC is leader and debug enabled, but player is not slacking
                if (_isLeader && _enablePenaltyDebug)
                {
                    // Note: the condition here checks that playerCollider.GetComponent<CharacterStatus>() != null
                    if (characterStatus != null)
                    {
                        Debug.Log($"[NPC Leader {gameObject.name}] 🧐 Player working normally, isSlackingAtWork: {characterStatus.isSlackingAtWork}");
                    }
                }
            }
        }

        /// <summary>
        /// Check whether a penalty can be applied
        /// </summary>
        /// <returns>Whether penalty can be applied</returns>
        public bool CanApplyPenalty()
        {
            return _isLeader && Time.time - _lastPenaltyTime >= _penaltyCooldown;
        }

        /// <summary>
        /// Get remaining penalty cooldown time
        /// </summary>
        /// <returns>Remaining cooldown time (seconds)</returns>
        public float GetPenaltyRemainingCooldown()
        {
            if (!_isLeader) return 0f;
            float remainingTime = _penaltyCooldown - (Time.time - _lastPenaltyTime);
            return Mathf.Max(0f, remainingTime);
        }

        /// <summary>
        /// Reset penalty cooldown (for debugging)
        /// </summary>
        public void ResetPenaltyCooldown()
        {
            _lastPenaltyTime = -999f;
            if (_enablePenaltyDebug)
            {
                Debug.Log($"[NPC Leader {gameObject.name}] Penalty cooldown reset");
            }
        }

        private bool IsBlockingObject(Collider collider)
        {
            int objectLayer = collider.gameObject.layer;
            return (_blockingLayerMask.value & (1 << objectLayer)) != 0;
        }

        #endregion

        #region Special Points System

        private void UpdateSpecialPoints()
        {
            if (!_enableSpecialPoints || _specialPoints == null || _specialPoints.Length == 0)
                return;

            // Update cooldown
            if (_specialPointCooldownRemaining > 0f)
            {
                _specialPointCooldownRemaining -= Time.deltaTime;
                _specialPointCooldownRemaining = Mathf.Max(0f, _specialPointCooldownRemaining);
            }

            // If currently at a special point, update stay timer
            if (_isAtSpecialPoint && _currentSpecialPoint != null)
            {
                _specialPointTimer += Time.deltaTime;
                if (_specialPointTimer >= _specialPointStayTime)
                {
                    StartReturnFromSpecialPoint();
                }
            }
        }

        // Check for special points within detection range
        public Transform GetNearbySpecialPoint()
        {
            if (!_enableSpecialPoints || _specialPoints == null || _specialPoints.Length == 0)
                return null;

            // If already handling a special point, don't check new ones
            if (_isGoingToSpecialPoint || _isAtSpecialPoint)
                return null;

            // Check cooldown
            if (_specialPointCooldownRemaining > 0f)
            {
                return null;
            }

            foreach (Transform specialPoint in _specialPoints)
            {
                if (specialPoint == null) continue;

                float distance = Vector3.Distance(transform.position, specialPoint.position);
                if (distance <= _specialPointDetectionRange)
                {
                    // Start cooldown (regardless of chance result)
                    _specialPointCooldownRemaining = _specialPointCooldownTime;
                    _lastSpecialPointCheckTime = Time.time;

                    // Chance roll
                    float randomValue = Random.Range(0f, 1f);
                    Debug.Log($"NPC {gameObject.name}: Special point {specialPoint.name} chance roll: {randomValue:F2} <= {_specialPointActivationChance:F2}?");

                    if (randomValue <= _specialPointActivationChance)
                    {
                        Debug.Log($"NPC {gameObject.name}: Chance succeeded! Heading to special point {specialPoint.name}");
                        return specialPoint;
                    }
                    else
                    {
                        Debug.Log($"NPC {gameObject.name}: Chance failed, starting cooldown {_specialPointCooldownTime} seconds");
                        // Even if chance fails, start cooldown
                        break; // Break to avoid checking other special points immediately
                    }
                }
            }

            return null;
        }

        // Start going to a special point
        public void StartGoingToSpecialPoint(Transform specialPoint)
        {
            if (specialPoint == null) return;

            _isGoingToSpecialPoint = true;
            _isAtSpecialPoint = false;
            _currentSpecialPoint = specialPoint;

            // Record return position
            if (_isFollowingPath && _waypoints != null && _currentWaypointIndex < _waypoints.Length)
            {
                _returnPosition = _waypoints[_currentWaypointIndex].position;
                _returnWaypointIndex = _currentWaypointIndex;
            }
            else
            {
                _returnPosition = transform.position;
                _returnWaypointIndex = _currentWaypointIndex;
            }

            Debug.Log($"NPC {gameObject.name}: Starting to go to special point {specialPoint.name}");
        }

        // Reach special point
        public void ReachSpecialPoint()
        {
            if (!_isGoingToSpecialPoint || _currentSpecialPoint == null) return;

            _isGoingToSpecialPoint = false;
            _isAtSpecialPoint = true;
            _specialPointTimer = 0f;

            // Mark visited
            _visitedSpecialPoints.Add(_currentSpecialPoint);

            Debug.Log($"NPC {gameObject.name}: Reached special point {_currentSpecialPoint.name}, starting to stay for {_specialPointStayTime} seconds");
        }

        // Start returning from special point
        public void StartReturnFromSpecialPoint()
        {
            if (!_isAtSpecialPoint) return;

            _isAtSpecialPoint = false;
            _isGoingToSpecialPoint = false;

            Debug.Log($"NPC {gameObject.name}: Returning from special point {_currentSpecialPoint.name} to path");

            _currentSpecialPoint = null;
            _specialPointTimer = 0f;

            // Resume path following
            if (_enablePathFollowing && _waypoints != null && _waypoints.Length > 0)
            {
                _currentWaypointIndex = _returnWaypointIndex;
                _isFollowingPath = true;
            }
        }

        // Check if near special point
        public bool IsNearSpecialPoint(Transform specialPoint)
        {
            if (specialPoint == null) return false;
            return Vector3.Distance(transform.position, specialPoint.position) <= _specialPointReachDistance;
        }

        // Check if near return position
        public bool IsNearReturnPosition()
        {
            return Vector3.Distance(transform.position, _returnPosition) <= _waypointReachDistance;
        }

        #endregion

        #region Path Following

        public void StartPathFollowing()
        {
            if (_waypoints != null && _waypoints.Length > 0)
            {
                _isFollowingPath = true;
                _currentWaypointIndex = 0;
                SetNextWaypoint();
            }
        }

        public void StopPathFollowing()
        {
            _isFollowingPath = false;
        }

        private void SetNextWaypoint()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;
        }

        private void UpdatePathFollowing()
        {
            if (!_isFollowingPath || _waypoints == null || _waypoints.Length == 0) return;

            if (_isWaitingAtWaypoint)
            {
                _waypointWaitTimer -= Time.deltaTime;
                if (_waypointWaitTimer <= 0f)
                {
                    _isWaitingAtWaypoint = false;
                    MoveToNextWaypoint();
                }
                return;
            }

            Vector3 currentTarget = _waypoints[_currentWaypointIndex].position;
            float distanceToWaypoint = Vector3.Distance(transform.position, currentTarget);

            if (distanceToWaypoint <= _waypointReachDistance)
            {
                _isWaitingAtWaypoint = true;
                _waypointWaitTimer = _waypointWaitTime;
                StopMovement();
            }
            else
            {
                MoveTowards(currentTarget);
            }
        }

        private void MoveToNextWaypoint()
        {
            _currentWaypointIndex++;

            if (_currentWaypointIndex >= _waypoints.Length)
            {
                if (_loopWaypoints)
                {
                    _currentWaypointIndex = 0;
                }
                else
                {
                    _isFollowingPath = false;
                    return;
                }
            }

            SetNextWaypoint();
        }

        public Vector3 GetCurrentWaypointTarget()
        {
            if (_waypoints != null && _waypoints.Length > 0 && _currentWaypointIndex < _waypoints.Length)
            {
                return _waypoints[_currentWaypointIndex].position;
            }
            return transform.position;
        }

        public void InterruptPathFollowing()
        {
            // Simple interruption without NavMesh
        }

        public void ResumePathFollowing()
        {
            if (_isFollowingPath)
            {
                SetNextWaypoint();
            }
        }

        #endregion

        #region Movement Control

        public void SetMoveDirection(Vector3 direction)
        {
            _moveDirection = direction.normalized;
            _movementInputHeld = direction.magnitude > 0.1f;
            _movementInputPressed = _movementInputHeld;
            _movementInputTapped = false;
        }

        public void SetGaitState(NPCGaitState gait)
        {
            switch (gait)
            {
                case NPCGaitState.Walk:
                    _isWalking = true;
                    _isSprinting = false;
                    break;
                case NPCGaitState.Run:
                    _isWalking = false;
                    _isSprinting = false;
                    break;
                case NPCGaitState.Sprint:
                    _isWalking = false;
                    _isSprinting = true;
                    break;
                default:
                    _isWalking = false;
                    _isSprinting = false;
                    break;
            }
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 lookDirection = (targetPosition - transform.position).normalized;
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSmoothing * Time.deltaTime);
            }
        }

        public void StopMovement()
        {
            SetMoveDirection(Vector3.zero);
        }

        public void MoveTowards(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;
            SetMoveDirection(direction);
        }

        #endregion

        #region Animation State Machine

        private void SwitchState(AnimationState newState)
        {
            ExitCurrentState();
            EnterState(newState);
        }

        private void EnterState(AnimationState stateToEnter)
        {
            _currentState = stateToEnter;
            switch (_currentState)
            {
                case AnimationState.Locomotion:
                    EnterLocomotionState();
                    break;
                case AnimationState.Jump:
                    EnterJumpState();
                    break;
                case AnimationState.Fall:
                    EnterFallState();
                    break;
                case AnimationState.Crouch:
                    EnterCrouchState();
                    break;
            }
        }

        private void ExitCurrentState()
        {
            // State cleanup if needed
        }

        private void EnterLocomotionState()
        {
            _previousRotation = transform.forward;
        }

        private void UpdateLocomotionState()
        {
            GroundedCheck();

            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
            }

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        private void EnterJumpState()
        {
            _velocity = new Vector3(_velocity.x, _jumpForce, _velocity.z);
        }

        private void UpdateJumpState()
        {
            ApplyGravity();
            if (_velocity.y <= 0f)
            {
                SwitchState(AnimationState.Fall);
            }
            GroundedCheck();
            CalculateMoveDirection();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        private void EnterFallState()
        {
            _fallStartTime = Time.time;
            _fallingDuration = 0f;
            _velocity.y = 0f;
        }

        private void UpdateFallState()
        {
            GroundedCheck();
            CalculateMoveDirection();
            FaceMoveDirection();
            ApplyGravity();
            Move();
            UpdateAnimatorController();

            if (_controller.isGrounded)
            {
                SwitchState(AnimationState.Locomotion);
            }

            _fallingDuration = Time.time - _fallStartTime;
        }

        private void EnterCrouchState()
        {
            // Crouch setup
        }

        private void UpdateCrouchState()
        {
            GroundedCheck();
            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
            }

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        #endregion

        #region Core Movement

        private void CalculateMoveDirection()
        {
            if (!_isGrounded)
            {
                _targetMaxSpeed = _currentMaxSpeed;
            }
            else if (_isCrouching)
            {
                _targetMaxSpeed = _walkSpeed;
            }
            else if (_isSprinting)
            {
                _targetMaxSpeed = _sprintSpeed;
            }
            else if (_isWalking)
            {
                _targetMaxSpeed = _walkSpeed;
            }
            else
            {
                _targetMaxSpeed = _runSpeed;
            }

            _currentMaxSpeed = Mathf.Lerp(_currentMaxSpeed, _targetMaxSpeed, _ANIMATION_DAMP_TIME * Time.deltaTime);

            _targetVelocity.x = _moveDirection.x * _currentMaxSpeed;
            _targetVelocity.z = _moveDirection.z * _currentMaxSpeed;

            _velocity.z = Mathf.Lerp(_velocity.z, _targetVelocity.z, _speedChangeDamping * Time.deltaTime);
            _velocity.x = Mathf.Lerp(_velocity.x, _targetVelocity.x, _speedChangeDamping * Time.deltaTime);

            _speed2D = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
            _speed2D = Mathf.Round(_speed2D * 1000f) / 1000f;

            Vector3 playerForwardVector = transform.forward;
            _newDirectionDifferenceAngle = playerForwardVector != _moveDirection
                ? Vector3.SignedAngle(playerForwardVector, _moveDirection, Vector3.up)
                : 0f;

            CalculateGait();
        }

        private void CalculateGait()
        {
            float runThreshold = (_walkSpeed + _runSpeed) / 2;
            float sprintThreshold = (_runSpeed + _sprintSpeed) / 2;

            if (_speed2D < 0.01)
            {
                _currentGait = GaitState.Idle;
            }
            else if (_speed2D < runThreshold)
            {
                _currentGait = GaitState.Walk;
            }
            else if (_speed2D < sprintThreshold)
            {
                _currentGait = GaitState.Run;
            }
            else
            {
                _currentGait = GaitState.Sprint;
            }
        }

        private void FaceMoveDirection()
        {
            Vector3 faceDirection = new Vector3(_velocity.x, 0f, _velocity.z);
            if (faceDirection == Vector3.zero) return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(faceDirection),
                _rotationSmoothing * Time.deltaTime
            );
        }

        private void CheckIfStopped()
        {
            _isStopped = _moveDirection.magnitude == 0 && _speed2D < .5;
        }

        private void CheckIfStarting()
        {
            _locomotionStartTimer = VariableOverrideDelayTimer(_locomotionStartTimer);
            bool isStartingCheck = false;

            if (_locomotionStartTimer <= 0.0f)
            {
                if (_moveDirection.magnitude > 0.01 && _speed2D < 1 && !_isStrafing)
                {
                    isStartingCheck = true;
                }

                if (isStartingCheck)
                {
                    if (!_isStarting)
                    {
                        _locomotionStartDirection = _newDirectionDifferenceAngle;
                    }

                    float delayTime = 0.2f;
                    _leanDelay = delayTime;
                    _headLookDelay = delayTime;
                    _bodyLookDelay = delayTime;
                    _locomotionStartTimer = delayTime;
                }
            }
            else
            {
                isStartingCheck = true;
            }

            _isStarting = isStartingCheck;
        }

        private void Move()
        {
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (_velocity.y > Physics.gravity.y)
            {
                _velocity.y += Physics.gravity.y * _gravityMultiplier * Time.deltaTime;
            }
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                _controller.transform.position.x,
                _controller.transform.position.y - _groundedOffset,
                _controller.transform.position.z
            );
            _isGrounded = Physics.CheckSphere(spherePosition, _controller.radius, _groundLayerMask, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                GroundInclineCheck();
            }
        }

        private void GroundInclineCheck()
        {
            if (_rearRayPos == null || _frontRayPos == null) return;

            float rayDistance = Mathf.Infinity;
            _rearRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);
            _frontRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);

            Physics.Raycast(_rearRayPos.position, _rearRayPos.TransformDirection(-Vector3.up), out RaycastHit rearHit, rayDistance, _groundLayerMask);
            Physics.Raycast(_frontRayPos.position, _frontRayPos.TransformDirection(-Vector3.up), out RaycastHit frontHit, rayDistance, _groundLayerMask);

            Vector3 hitDifference = frontHit.point - rearHit.point;
            float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;

            _inclineAngle = Mathf.Lerp(_inclineAngle, Mathf.Atan2(hitDifference.y, xPlaneLength) * Mathf.Rad2Deg, 20f * Time.deltaTime);
        }

        private float VariableOverrideDelayTimer(float timeVariable)
        {
            if (timeVariable > 0.0f)
            {
                timeVariable -= Time.deltaTime;
                timeVariable = Mathf.Clamp(timeVariable, 0.0f, 1.0f);
            }
            else
            {
                timeVariable = 0.0f;
            }
            return timeVariable;
        }

        #endregion

        #region Animation Updates

        private void UpdateAnimatorController()
        {
            _animator.SetFloat(_leanValueHash, _leanValue);
            _animator.SetFloat(_headLookXHash, _headLookX);
            _animator.SetFloat(_headLookYHash, _headLookY);
            _animator.SetFloat(_bodyLookXHash, _bodyLookX);
            _animator.SetFloat(_bodyLookYHash, _bodyLookY);
            _animator.SetFloat(_isStrafingHash, _isStrafing ? 1.0f : 0.0f);
            _animator.SetFloat(_inclineAngleHash, _inclineAngle);
            _animator.SetFloat(_moveSpeedHash, _speed2D);
            _animator.SetInteger(_currentGaitHash, (int)_currentGait);
            _animator.SetFloat(_strafeDirectionXHash, _strafeDirectionX);
            _animator.SetFloat(_strafeDirectionZHash, _strafeDirectionZ);
            _animator.SetFloat(_forwardStrafeHash, _forwardStrafe);
            _animator.SetBool(_movementInputHeldHash, _movementInputHeld);
            _animator.SetBool(_movementInputPressedHash, _movementInputPressed);
            _animator.SetBool(_movementInputTappedHash, _movementInputTapped);
            _animator.SetBool(_isTurningInPlaceHash, _isTurningInPlace);
            _animator.SetBool(_isCrouchingHash, _isCrouching);
            _animator.SetFloat(_fallingDurationHash, _fallingDuration);
            _animator.SetBool(_isGroundedHash, _isGrounded);
            _animator.SetBool(_isWalkingHash, _isWalking);
            _animator.SetBool(_isStoppedHash, _isStopped);
            _animator.SetFloat(_locomotionStartDirectionHash, _locomotionStartDirection);
        }

        #endregion
    }

    #region Behavior Tree System

    public abstract class BehaviorNode
    {
        public enum NodeState
        {
            Running,
            Success,
            Failure
        }

        public abstract NodeState Evaluate();
    }

    public class SelectorNode : BehaviorNode
    {
        private List<BehaviorNode> children = new List<BehaviorNode>();

        public SelectorNode(params BehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override NodeState Evaluate()
        {
            foreach (BehaviorNode child in children)
            {
                NodeState result = child.Evaluate();
                if (result == NodeState.Success || result == NodeState.Running)
                {
                    return result;
                }
            }
            return NodeState.Failure;
        }
    }

    public class SequenceNode : BehaviorNode
    {
        private List<BehaviorNode> children = new List<BehaviorNode>();

        public SequenceNode(params BehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override NodeState Evaluate()
        {
            foreach (BehaviorNode child in children)
            {
                NodeState result = child.Evaluate();
                if (result == NodeState.Failure || result == NodeState.Running)
                {
                    return result;
                }
            }
            return NodeState.Success;
        }
    }

    public class ConditionNode : BehaviorNode
    {
        private System.Func<bool> condition;

        public ConditionNode(System.Func<bool> condition)
        {
            this.condition = condition;
        }

        public override NodeState Evaluate()
        {
            return condition() ? NodeState.Success : NodeState.Failure;
        }
    }

    public class ActionNode : BehaviorNode
    {
        private System.Func<NodeState> action;

        public ActionNode(System.Func<NodeState> action)
        {
            this.action = action;
        }

        public override NodeState Evaluate()
        {
            return action();
        }
    }

    public class NPCBehaviorTree
    {
        private NPCAnimationController npc;
        private BehaviorNode rootNode;
        private Vector3 patrolTarget;
        private float lastPatrolTime;
        private float patrolInterval = 3f;

        public NPCBehaviorTree(NPCAnimationController npcController)
        {
            npc = npcController;
            BuildBehaviorTree();
        }

        private void BuildBehaviorTree()
        {
            rootNode = new SelectorNode(
                // Highest priority: avoid obstacles
                new SequenceNode(
                    new ConditionNode(() => npc.HasObstacleAhead),
                    new ActionNode(AvoidObstacle)
                ),

                // Second priority: go to special points
                new SequenceNode(
                    new ConditionNode(() => npc.EnableSpecialPoints && !npc.IsGoingToSpecialPoint && !npc.IsAtSpecialPoint),
                    new ActionNode(CheckForSpecialPoints)
                ),

                // Third priority: special point navigation
                new SequenceNode(
                    new ConditionNode(() => npc.IsGoingToSpecialPoint),
                    new ActionNode(GoToSpecialPoint)
                ),

                // Fourth priority: stay at special point
                new SequenceNode(
                    new ConditionNode(() => npc.IsAtSpecialPoint),
                    new ActionNode(StayAtSpecialPoint)
                ),

                // Fifth priority: follow path
                new SequenceNode(
                    new ConditionNode(() => npc.EnablePathFollowing && npc.Waypoints != null && npc.Waypoints.Length > 0),
                    new ActionNode(FollowPath)
                ),

                // Sixth priority: stationary scanning
                new SequenceNode(
                    new ConditionNode(() => !npc.IsMoving &&
                                           (!npc.EnablePathFollowing || npc.Waypoints == null || npc.Waypoints.Length == 0)),
                    new ActionNode(StationaryScanning)
                ),

                // Lowest priority: patrol behavior
                new ActionNode(PatrolBehavior)
            );
        }

        public void Update()
        {
            rootNode?.Evaluate();
        }

        private BehaviorNode.NodeState CheckForSpecialPoints()
        {
            Transform nearbySpecialPoint = npc.GetNearbySpecialPoint();
            if (nearbySpecialPoint != null)
            {
                npc.StartGoingToSpecialPoint(nearbySpecialPoint);
                return BehaviorNode.NodeState.Success;
            }
            return BehaviorNode.NodeState.Failure;
        }

        private BehaviorNode.NodeState GoToSpecialPoint()
        {
            if (npc.CurrentSpecialPoint == null)
            {
                return BehaviorNode.NodeState.Failure;
            }

            // Check if reached the special point
            if (npc.IsNearSpecialPoint(npc.CurrentSpecialPoint))
            {
                npc.ReachSpecialPoint();
                return BehaviorNode.NodeState.Success;
            }

            // Continue moving toward special point
            npc.MoveTowards(npc.CurrentSpecialPoint.position);
            npc.SetGaitState(NPCAnimationController.NPCGaitState.Walk);
            return BehaviorNode.NodeState.Running;
        }

        private BehaviorNode.NodeState StayAtSpecialPoint()
        {
            // Stay at the special point, do nothing
            npc.StopMovement();
            return BehaviorNode.NodeState.Running;
        }

        private BehaviorNode.NodeState StationaryScanning()
        {
            if (!npc.IsScanningStationary)
            {
                npc.StartStationaryScanning();
            }

            npc.StopMovement();

            if (npc.IsScanningStationary)
            {
                return BehaviorNode.NodeState.Running;
            }

            return BehaviorNode.NodeState.Success;
        }

        private BehaviorNode.NodeState AvoidObstacle()
        {
            npc.InterruptPathFollowing();
            npc.StopStationaryScanning();

            Vector3 rayOrigin = npc.transform.position + Vector3.up * 0.5f;

            Vector3 leftDirection = Quaternion.AngleAxis(-45f, Vector3.up) * npc.transform.forward;
            bool leftBlocked = Physics.Raycast(rayOrigin, leftDirection, npc.ObstacleDetectionDistance, npc.ObstacleLayerMask);

            Vector3 rightDirection = Quaternion.AngleAxis(45f, Vector3.up) * npc.transform.forward;
            bool rightBlocked = Physics.Raycast(rayOrigin, rightDirection, npc.ObstacleDetectionDistance, npc.ObstacleLayerMask);

            Vector3 avoidDirection;

            if (!rightBlocked && leftBlocked)
            {
                avoidDirection = npc.transform.right;
            }
            else if (!leftBlocked && rightBlocked)
            {
                avoidDirection = -npc.transform.right;
            }
            else if (leftBlocked && rightBlocked)
            {
                avoidDirection = -npc.transform.forward;
            }
            else
            {
                avoidDirection = npc.transform.right;
            }

            npc.SetMoveDirection(avoidDirection);
            npc.SetGaitState(NPCAnimationController.NPCGaitState.Walk);

            return BehaviorNode.NodeState.Success;
        }

        private BehaviorNode.NodeState FollowPath()
        {
            if (!npc.IsFollowingPath)
            {
                npc.StartPathFollowing();
            }

            Vector3 target = npc.GetCurrentWaypointTarget();
            float distanceToTarget = Vector3.Distance(npc.transform.position, target);

            if (distanceToTarget > 0.5f)
            {
                npc.SetGaitState(NPCAnimationController.NPCGaitState.Walk);
                return BehaviorNode.NodeState.Running;
            }

            return BehaviorNode.NodeState.Success;
        }

        private BehaviorNode.NodeState PatrolBehavior()
        {
            if (Time.time - lastPatrolTime > patrolInterval || Vector3.Distance(npc.transform.position, patrolTarget) < 1f)
            {
                patrolTarget = npc.transform.position + new Vector3(
                    Random.Range(-10f, 10f),
                    0f,
                    Random.Range(-10f, 10f)
                );
                lastPatrolTime = Time.time;
                patrolInterval = Random.Range(2f, 5f);
            }

            if (Vector3.Distance(npc.transform.position, patrolTarget) > 0.5f)
            {
                npc.MoveTowards(patrolTarget);
                npc.SetGaitState(NPCAnimationController.NPCGaitState.Walk);
            }
            else
            {
                npc.StopMovement();
            }

            return BehaviorNode.NodeState.Running;
        }
    }

    #endregion
}