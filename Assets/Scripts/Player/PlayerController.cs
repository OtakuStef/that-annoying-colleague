using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTutorial.Manager;

namespace UnityTutorial.PlayerControl
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float AnimBlendSpeed = 8.9f;
        [SerializeField] private Transform CameraRoot;
        [SerializeField] private Transform Camera;
        [SerializeField] private float UpperLimit = -40f;
        [SerializeField] private float BottomLimit = 70f;
        [SerializeField] private float MouseSensitivity = 21.9f;
        [SerializeField, Range(10, 500)] private float JumpFactor = 260f;
        [SerializeField] private float Dis2Ground = 0.8f;
        [SerializeField] private LayerMask GroundCheck;
        [SerializeField] private float AirResistance = 0.8f;
        private Rigidbody _playerRigidbody;
        private InputManager _inputManager;
        private Animator _animator;
        private bool _grounded = false;
        private bool _hasAnimator;
        private int _xVelHash;
        private int _yVelHash;
        private int _jumpHash;
        private int _groundHash;
        private int _fallingHash;
        private int _zVelHash;
        private int _crouchHash;
        private float _xRotation;

        private const float _walkSpeed = 2f;
        private const float _runSpeed = 6f;
        private Vector2 _currentVelocity;

        // adding audio manager to add sound effects for player
        AudioManager audioManager;
        private bool isFootstepPlaying = false;
        private bool wasRunning = false;

        //adding powerup boost variables
        private float speedMultiplier = 1f;
        private float damageMutliplier = 1f;
        private  bool isShieldActive = false;

        private void Start() {
            _hasAnimator = TryGetComponent<Animator>(out _animator);
            _playerRigidbody = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();


            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _zVelHash = Animator.StringToHash("Z_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");
            _crouchHash = Animator.StringToHash("Crouch");
        }

        private void Awake()
        {
            audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        }

        private void FixedUpdate() {
            SampleGround();
            Move();
            HandleJump();
            HandleCrouch();
        }
        private void LateUpdate() {
            CamMovements();
        }

        private void Move()
        {
            if(!_hasAnimator) return;

            float targetSpeed = _inputManager.Run ? _runSpeed : _walkSpeed;
            targetSpeed *= speedMultiplier;

            AudioClip stepsSound = _inputManager.Run ? audioManager.steps_running_01 : audioManager.steps_01;
            bool isRunning = _inputManager.Run;
            bool isMoving = _inputManager.Move != Vector2.zero;

            if (_inputManager.Crouch)
            {
                targetSpeed = 1.5f;
                targetSpeed *= speedMultiplier;
                Debug.Log("Move pressed");
            }
            if (_inputManager.Move == Vector2.zero)  targetSpeed = 0;
            

            if (_grounded)
            {
                
                _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, _inputManager.Move.x * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
                _currentVelocity.y =  Mathf.Lerp(_currentVelocity.y, _inputManager.Move.y * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);

                var xVelDifference = _currentVelocity.x - _playerRigidbody.velocity.x;
                var zVelDifference = _currentVelocity.y - _playerRigidbody.velocity.z;

                _playerRigidbody.AddForce(transform.TransformVector(new Vector3(xVelDifference, 0 , zVelDifference)), ForceMode.VelocityChange);

                if (isMoving)
                {
                    if (!isFootstepPlaying || wasRunning != isRunning)
                    {
                        audioManager.StopSFX();
                        audioManager.PlaySFX(stepsSound, true);
                        isFootstepPlaying = true;
                        wasRunning = isRunning;
                    }
                }
                else if (isFootstepPlaying)
                {
                    audioManager.StopSFX();
                    isFootstepPlaying = false;
                }

            }
            else
            {
                _playerRigidbody.AddForce(transform.TransformVector(new Vector3(_currentVelocity.x * AirResistance,0,_currentVelocity.y * AirResistance)), ForceMode.VelocityChange);
            }


            _animator.SetFloat(_xVelHash , _currentVelocity.x);
            _animator.SetFloat(_yVelHash, _currentVelocity.y);
        }

        private void CamMovements()
        {
            if(!_hasAnimator) return;

            var Mouse_X = _inputManager.Look.x;
            var Mouse_Y = _inputManager.Look.y;
            Camera.position = CameraRoot.position;
            
            
            _xRotation -= Mouse_Y * MouseSensitivity * Time.smoothDeltaTime;
            _xRotation = Mathf.Clamp(_xRotation, UpperLimit, BottomLimit);

            Camera.localRotation = Quaternion.Euler(_xRotation, 0 , 0);
            _playerRigidbody.MoveRotation(_playerRigidbody.rotation * Quaternion.Euler(0, Mouse_X * MouseSensitivity * Time.smoothDeltaTime, 0));
        }

        private void HandleCrouch() => _animator.SetBool(_crouchHash , _inputManager.Crouch);


        private void HandleJump()
        {
            if(!_hasAnimator) return;
            if(!_inputManager.Jump) return;
            if(!_grounded) return;
            _animator.SetTrigger(_jumpHash);

            //Enable this if you want B-Hop
            _playerRigidbody.AddForce(-_playerRigidbody.velocity.y * Vector3.up, ForceMode.VelocityChange);
            _playerRigidbody.AddForce(Vector3.up * JumpFactor, ForceMode.Impulse);
            _animator.ResetTrigger(_jumpHash);
        }

        public void JumpAddForce()
        {
            //Comment this out if you want B-Hop, otherwise the player will jump twice in the air
            //_playerRigidbody.AddForce(-_playerRigidbody.velocity.y * Vector3.up, ForceMode.VelocityChange);
            //_playerRigidbody.AddForce(Vector3.up * JumpFactor, ForceMode.Impulse);
            //_animator.ResetTrigger(_jumpHash);
        }

        private void SampleGround()
        {
            if(!_hasAnimator) return;
            
            RaycastHit hitInfo;
            if(Physics.Raycast(_playerRigidbody.worldCenterOfMass, Vector3.down, out hitInfo, Dis2Ground + 0.1f, GroundCheck))
            {
                //Grounded
                _grounded = true;
                SetAnimationGrounding();
                return;
            }
            //Falling
            _grounded = false;
            _animator.SetFloat(_zVelHash, _playerRigidbody.velocity.y);
            SetAnimationGrounding();
            return;
        }

        private void SetAnimationGrounding()
        {
            _animator.SetBool(_fallingHash, !_grounded);
            _animator.SetBool(_groundHash, _grounded);
        }

        //powerup functions
        public void setSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        public void setDamageMultiplier(float multiplier)
        {
            damageMutliplier = multiplier;
        }

        public float getDamageMultiplier()
        {
            return damageMutliplier;
        }

        public void setShield(bool shieldActive)
        {
            isShieldActive = shieldActive;
        }

        public bool getShieldStatus()
        {
            return isShieldActive;
        }

    }
}
