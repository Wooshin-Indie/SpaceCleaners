using MPGame.Controller.StateMachine;
using MPGame.Utils;
using Steamworks;
using Unity.Netcode;
using UnityEditor.Build;
using UnityEngine;


namespace MPGame.Controller
{
	public class PlayerController : NetworkBehaviour
	{

		/** Components **/
		private Rigidbody rigid;
		private CapsuleCollider capsule;
		private Animator animator;


		/** Properties **/
		public Rigidbody Rigidbody { get => rigid; }
		public CapsuleCollider Capsule { get => capsule; }
		public Animator Animator { get => animator; }

		[Header("Player Move Args")]
		[SerializeField] private float walkSpeed;
		[SerializeField] private float horzRotSpeed;
		[SerializeField] private float vertRotSpeed;
		[SerializeField, Range(0f, 90f)] private float maxVertRot;
		[SerializeField, Range(-90f, 0f)] private float minVertRot;
		[SerializeField] private float jumpForce;

		[Header("Slope Args")]
		[SerializeField] private float groundedOffset;
		[SerializeField] private Vector3 groundRectSize;
		[SerializeField] private float slopeRayLength;
		[SerializeField] private float slopeLimit;
		[SerializeField] private PhysicsMaterial playerPM;
		[SerializeField] private PhysicsMaterial slopePM;

		[Header("GameObjects")]
		[SerializeField] private Transform cameraTransform;

		[Header("Raycast Args")]                        // Use to detect Interactables
		[SerializeField] private float rayLength;


		// animation Ids
		public int animIDSpeed;
		public int animIDJump;
		public int animIDMotionSpeed;
		public int animIdGrounded;
		public int animIdFreeFall;

		private PlayerStateMachine stateMachine;
		public PlayerStateMachine StateMachine { get => stateMachine; }
		public IdleState idleState;
		public WalkState walkState;
		public JumpState jumpState;
		public FallState fallState;


		private bool isGrounded = true;
		private bool isDetectInteractable = false;

		public bool IsGrounded { get => isGrounded; }
		public bool IsDetectInteractable { get => isDetectInteractable; }

		private GameObject recentlyDetectedProp = null;
		public GameObject RecentlyDetectedProp { get => recentlyDetectedProp; }


		private void Awake()
		{
			rigid = GetComponent<Rigidbody>();
			animator = GetComponent<Animator>();
			capsule = GetComponent<CapsuleCollider>();

			stateMachine = new PlayerStateMachine();
			idleState = new IdleState(this, stateMachine);
			walkState = new WalkState(this, stateMachine);
			jumpState = new JumpState(this, stateMachine);
			fallState = new FallState(this, stateMachine);

			animIDSpeed = Animator.StringToHash("Speed");
			animIDJump = Animator.StringToHash("Jump");
			animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
			animIdGrounded = Animator.StringToHash("Grounded");
			animIdFreeFall = Animator.StringToHash("FreeFall");

			animator.SetFloat(animIDMotionSpeed, 1f);
		}

		float currentSpeed = 0f;

		private void Start()
		{
			stateMachine.Init(idleState);
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			cameraTransform.gameObject.SetActive(IsOwner);
		}

		private void Update()
		{
			if (!IsOwner) return;
			
			stateMachine.CurState.HandleInput();
			stateMachine.CurState.LogicUpdate();
		}

		private void FixedUpdate()
		{
			if (!IsOwner) return;

			stateMachine.CurState.PhysicsUpdate();
		}


		#region Logic Control Funcs

		private float horzRot = 0f;
		private float vertRot = 0f;
		public void RotateWithMouse(float mouseX, float mouseY)
		{
			horzRot += mouseX * horzRotSpeed;
			vertRot -= mouseY * vertRotSpeed;
			vertRot = Mathf.Clamp(vertRot, minVertRot, maxVertRot);

			transform.rotation = Quaternion.Euler(0f, horzRot, 0f);
			cameraTransform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);
			PlayerRotateServerRPC(transform.rotation, cameraTransform.localRotation);
		}

		[ServerRpc(RequireOwnership = false)]
		private void PlayerRotateServerRPC(Quaternion playerQuat, Quaternion camQuat)
		{
			UpdatePlayerRotateClientRPC(transform.rotation, cameraTransform.rotation);
		}
		[ClientRpc]
		private void UpdatePlayerRotateClientRPC(Quaternion playerQuat, Quaternion camQuat)
		{
			if (IsOwner) return;
			transform.rotation = playerQuat;
			cameraTransform.rotation = camQuat;
		}

		public void GroundedCheck()
		{
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
				transform.position.z);
			isGrounded = Physics.CheckBox(spherePosition, groundRectSize, Quaternion.identity,
				Constants.LAYER_GROUND,
				QueryTriggerInteraction.Ignore);

			animator.SetBool(animIdGrounded, isGrounded);
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawCube(new Vector3(transform.position.x, transform.position.y - groundedOffset,
				transform.position.z), groundRectSize);
		}

		public void DetectIsGround()
		{
			if (isGrounded)
			{
				animator.SetBool(animIdGrounded, true);
				stateMachine.ChangeState(idleState);
			}
		}

		public void DetectIsFalling()
		{
			if (isGrounded) return;
			DetectIsFallingWhileJump();
		}

		public void DetectIsFallingWhileJump()
		{
			if (rigid.linearVelocity.y <= 0f)
			{
				stateMachine.ChangeState(fallState);
			}
		}

		public bool OnSlope()
		{
			Debug.DrawRay(transform.position * slopeRayLength, Vector3.down, Color.red);
			if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeRayLength))
			{
				float angle = Vector3.Angle(hit.normal, Vector3.up);
				Debug.Log("ANGLE : " + angle);
				return angle > slopeLimit;
			}

			return false;
		}


		public void Jump(bool isJumpPressed)
		{
			if (!isJumpPressed) return;

			rigid.AddForce(new Vector3(0f, jumpForce, 0f), ForceMode.Impulse);
			stateMachine.ChangeState(jumpState);
		}

		#endregion


		#region Physics Control Funcs

		public void WalkWithArrow(float vertInputRaw, float horzInputRaw, float diag)
		{
			Vector3 moveDir = (transform.forward * horzInputRaw + transform.right * vertInputRaw);
			currentSpeed = moveDir.magnitude * walkSpeed * diag;

			rigid.MovePosition(transform.position + moveDir * diag * walkSpeed * Time.fixedDeltaTime);
			animator.SetFloat(animIDSpeed, currentSpeed);
			PlayerWalkServerRPC(moveDir, diag);
		}

		[ServerRpc(RequireOwnership = false)]
		private void PlayerWalkServerRPC(Vector3 moveDir, float diag)
		{
			UpdatePlayerWalkClientRPC(moveDir, diag);
		}
		[ClientRpc]
		private void UpdatePlayerWalkClientRPC(Vector3 moveDir, float diag)
		{
			if (IsOwner) return;
			rigid.MovePosition(transform.position + moveDir * diag * walkSpeed * Time.fixedDeltaTime);
		}
		public void RaycastInteractableObject()
		{
			RaycastHit hit;
			int targetLayer = Constants.LAYER_INTERACTABLE;

			if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, rayLength, targetLayer))
			{
				isDetectInteractable = true;
				recentlyDetectedProp = hit.transform.GetComponent<GameObject>();
			}
			else
			{
				isDetectInteractable = false;
				recentlyDetectedProp = null;
			}

			Debug.DrawRay(cameraTransform.position, cameraTransform.forward * rayLength, Color.red);
		}

		public void TurnPlayerPM()
		{
			capsule.material = playerPM;
		}

		public void TurnSlopePM()
		{
			capsule.material = slopePM;
		}

		#endregion

		#region Animation Synchronization

		#endregion

		#region Animation Event
		private void OnFootstep (AnimationEvent animationEvent)
		{

		}
		private void OnLand(AnimationEvent animationEvent)
		{

		}
		#endregion
	}
}