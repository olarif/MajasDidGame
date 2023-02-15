using System;
using System.Collections;
using UnityEngine;

namespace PlayerController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour{
        
        //reference for scriptable object player stats
        [SerializeField] private ScriptableStats _stats;

        #region Internal

        [HideInInspector] private Rigidbody2D _rb;
        private CapsuleCollider2D _standingCollider;
        private CapsuleCollider2D _col; // current active collider
        private PlayerInputs _input;
        private bool _cachedTriggerSetting;

        private FrameInput _frameInput;
        private Vector2 _currentExternalVelocity;
        private int _fixedFrame;
        private bool _hasControl = true;

        private bool facingRight;
        private bool canMove;

        [SerializeField] private Transform skeleton;
        private Animator anim;
        
        #endregion

        #region External
        
        [HideInInspector] public Vector2 _speed;
        public event Action<bool, float> GroundedChanged;
        public event Action<bool> Jumped;
        public event Action AirJumped;
        public ScriptableStats PlayerStats => _stats;
        public Vector2 Input => _frameInput.Move;
        public Vector2 Speed => _speed;
        public Vector2 GroundNormal => _groundNormal;

        public virtual void ApplyVelocity(Vector2 vel, PlayerForce forceType) {
            if (forceType == PlayerForce.Burst) _speed += vel;
            else _currentExternalVelocity += vel;
        }

        public virtual void TakeAwayControl(bool resetVelocity = true) {
            if (resetVelocity) _rb.velocity = Vector2.zero;
            _hasControl = false;
        }

        public virtual void ReturnControl() {
            _speed = Vector2.zero;
            _hasControl = true;
        }

        #endregion

        protected virtual void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            _rb = GetComponent<Rigidbody2D>();
            _standingCollider = GetComponent<CapsuleCollider2D>();
            _input = GetComponent<PlayerInputs>();
            _cachedTriggerSetting = Physics2D.queriesHitTriggers;
            _col = _standingCollider;
            Physics2D.queriesStartInColliders = false;
        }

        protected virtual void Update() {
            GatherInput();
            UpdateAnimator();
        }

        protected virtual void GatherInput() {
            _frameInput = _input.FrameInput;

            if (_frameInput.JumpDown) {
                _jumpToConsume = true;
                _frameJumpWasPressed = _fixedFrame;
            }
        }

        protected virtual void FixedUpdate() {
            _fixedFrame++;

            CheckCollisions();
            HandleCollisions();
            HandleJump();

            HandleHorizontal();
            HandleVertical();
            ApplyVelocity();
        }

        #region Collisions

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[2];
        private readonly RaycastHit2D[] _ceilingHits = new RaycastHit2D[2];
        private readonly Collider2D[] _ladderHits = new Collider2D[1];
        private Vector2 _groundNormal;
        private int _groundHitCount;
        private int _ceilingHitCount;
        private int _wallHitCount;
        private int _ladderHitCount;
        private int _frameLeftGrounded = int.MinValue;
        private bool _grounded;

        protected virtual void CheckCollisions() {
            Physics2D.queriesHitTriggers = false;
            
            // Ground and Ceiling
            _groundHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _groundHits, _stats.GrounderDistance, _stats._groundLayer);
            _ceilingHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _ceilingHits, _stats.GrounderDistance, ~_stats.PlayerLayer);

            // Walls and Ladders
            var bounds = GetWallDetectionBounds(); // won't be able to detect a wall if we're crouching mid-air

            if (_grounded)
            {
                inAir = false;
            }
        }

        protected virtual bool TryGetGroundNormal(out Vector2 groundNormal) {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.Raycast(_rb.position, Vector2.down, _stats.GrounderDistance * 2, ~_stats.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            groundNormal = hit.normal; // defaults to Vector2.zero if nothing was hit
            return hit.collider != null;
        }

        private Bounds GetWallDetectionBounds() {
            var colliderOrigin = _rb.position + _standingCollider.offset;
            return new Bounds(colliderOrigin, _stats.WallDetectorSize);
        }

        protected virtual void HandleCollisions() {
            // Hit a Ceiling
            if (_ceilingHitCount > 0) _speed.y = Mathf.Min(0, _speed.y);

            // Landed on the Ground
            if (!_grounded && _groundHitCount > 0) {
                _grounded = true;
                ResetJump();
                GroundedChanged?.Invoke(true, Mathf.Abs(_speed.y));
            }
            // Left the Ground
            else if (_grounded && _groundHitCount == 0) {
                _grounded = false;
                _frameLeftGrounded = _fixedFrame;
                GroundedChanged?.Invoke(false, 0);
            }
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private bool _bufferedJumpUsable;
        private int _frameJumpWasPressed = int.MinValue;
        private int _airJumpsRemaining;
        private bool inAir;

        private bool CanUseCoyote => _coyoteUsable && !_grounded && _fixedFrame < _frameLeftGrounded + _stats.CoyoteFrames;
        private bool HasBufferedJump => _bufferedJumpUsable && _fixedFrame < _frameJumpWasPressed + _stats.JumpBufferFrames;
        private bool CanAirJump => _airJumpsRemaining > 0;

        protected virtual void HandleJump() {
            if (_jumpToConsume || HasBufferedJump) {
                if (_grounded || CanUseCoyote) NormalJump();
                else if (_jumpToConsume && CanAirJump) AirJump();
            }
            
            _jumpToConsume = false; // Always consume the flag

            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true; // Early end detection
        }

        protected virtual void NormalJump()
        {
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _speed.y = _stats.JumpPower;
            Jumped?.Invoke(false);
        }

        protected virtual void AirJump()
        {
            _endedJumpEarly = false;
            _airJumpsRemaining--;
            _speed.y = _stats.JumpPower;
            AirJumped?.Invoke();
        }

        protected virtual void ResetJump() {
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            ResetAirJumps();
        }

        protected virtual void ResetAirJumps() => _airJumpsRemaining = _stats.MaxAirJumps;

        #endregion

        #region Horizontal

        protected virtual void HandleHorizontal() {

            if (_frameInput.Move.x > 0 && facingRight)
            {
                Flip();
            }
            
            if (_frameInput.Move.x < 0 && !facingRight)
            {
                Flip();
            }
            
            // Deceleration
            if (Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadzoneThreshold)
                _speed.x = Mathf.MoveTowards(_speed.x, 0, (_grounded ? _stats.GroundDeceleration : _stats.AirDeceleration) * Time.fixedDeltaTime);
            // Regular Horizontal Movement
            else {
                // Prevent useless horizontal speed buildup when against a wall
                if (_wallHitCount > 0 && Mathf.Approximately(_rb.velocity.x, 0) && Mathf.Sign(_frameInput.Move.x) == Mathf.Sign(_speed.x))
                    _speed.x = 0;

                var inputX = _frameInput.Move.x * (1);
                _speed.x = Mathf.MoveTowards(_speed.x, inputX * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
            
        }
        
        private void Flip()
        {
            facingRight = !facingRight;
            skeleton.transform.RotateAround(transform.position, transform.up, 180f);
        }

        #endregion

        #region Vertical

        protected virtual void HandleVertical() {
            // Grounded & Slopes
            if (_grounded && _speed.y <= 0f) { // TODO: double check this velocity condition. If we're going up a slope, y-speed will be >0
                _speed.y = _stats.GroundingForce;

                if (TryGetGroundNormal(out _groundNormal)) {
                    if (!Mathf.Approximately(_groundNormal.y, 1f)) { // on a slope
                        _speed.y = _speed.x * -_groundNormal.x / _groundNormal.y;
                        if (_speed.x != 0) _speed.y += _stats.GroundingForce;
                    }
                }
            }
            // In Air
            else
            {
                inAir = true;
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _speed.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _speed.y = Mathf.MoveTowards(_speed.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Triggers

        public void OnTriggerEnter2D(Collider2D col)
        {
            
        }

        private void OnTriggerExit2D(Collider2D col)
        {
            
        }

        private void OnTriggerStay2D(Collider2D col)
        {
            
        }
        
        #endregion
        
        protected virtual void UpdateAnimator()
        {
            //anim.SetFloat("VerticalSpeed",   Mathf.Abs(_speed.y));
            //anim.SetFloat("HorizontalSpeed", Mathf.Abs(_speed.x));

            //anim.SetBool("IsJumping", inAir ? true : false);
            
        }
        
        protected virtual void ApplyVelocity() {
            if (!_hasControl) return;
            _rb.velocity = _speed + _currentExternalVelocity;

            _currentExternalVelocity = Vector2.MoveTowards(_currentExternalVelocity, Vector2.zero, _stats.ExternalVelocityDecay * Time.fixedDeltaTime);
        }
    }

    public enum PlayerForce {
        /// <summary>
        /// Added directly to the players movement speed, to be controlled by the standard deceleration
        /// </summary>
        Burst,

        /// <summary>
        /// An additive force handled by the decay system
        /// </summary>
        Decay
    }
}
