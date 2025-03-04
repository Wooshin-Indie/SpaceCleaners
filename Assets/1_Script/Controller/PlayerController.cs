using MPGame.Controller.StateMachine;
using MPGame.Manager;
using MPGame.Props;
using MPGame.Utils;
using System;
using Unity.Netcode;
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
		[SerializeField] private float gravityForce;
		[SerializeField] private bool useGravity;

		[Header("Slope Args")]
		[SerializeField] private float groundedOffset;
		[SerializeField] private Vector3 groundRectSize;
		[SerializeField] private float slopeRayLength;
		[SerializeField] private float slopeLimit;
		[SerializeField] private PhysicsMaterial idlePM;
		[SerializeField] private PhysicsMaterial playerPM;
		[SerializeField] private PhysicsMaterial slopePM;
		[SerializeField] private PhysicsMaterial flyPM;

		[Header("Flying Args")]
		[SerializeField] private float thrustPower;
		[SerializeField] private float rotationPower;
		[SerializeField] private float velocityToLand;
		[SerializeField] private float enableToLandAngle;

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
		public FlyState flyState;


		public bool UseGravity { get => useGravity; set => useGravity = value; }

		private bool isGrounded = true;
		private bool isDetectInteractable = false;

		public bool IsGrounded { get => isGrounded; }
		public bool IsDetectInteractable { get => isDetectInteractable; }

		private GameObject recentlyDetectedProp = null;
		public GameObject RecentlyDetectedProp { get => recentlyDetectedProp; }

		private Vector3 gravityVector = new Vector3(0f, 0f, 0f);
		private GravityType gravityType = GravityType.None;

		Vector3 projectedForward = Vector3.zero;

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
			flyState = new FlyState(this, stateMachine);

			animIDSpeed = Animator.StringToHash("Speed");
			animIDJump = Animator.StringToHash("Jump");
			animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
			animIdGrounded = Animator.StringToHash("Grounded");
			animIdFreeFall = Animator.StringToHash("FreeFall");

			rigid.maxLinearVelocity = 10f;
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
			rigid.isKinematic = !IsOwner;
			ChangeAnimatorParam(animIDMotionSpeed, 1f);

			if (IsHost)
				PlayerSpawner.Instance.SpawnEnvironments();
		}

		private void Update()
		{
			if (!IsOwner) return;
			if (Input.GetKeyDown(KeyCode.R))
			{
				stateMachine.ChangeState(flyState);
			}
			stateMachine.CurState.HandleInput();
			stateMachine.CurState.LogicUpdate();
			UpdatePlayerTransformServerRPC(transform.position, transform.rotation, cameraTransform.localRotation);
		}

		private void FixedUpdate()
		{
			if (!IsOwner) return;

			stateMachine.CurState.PhysicsUpdate();

		}

		#region Transform Synchronization

		[ServerRpc(RequireOwnership = false)]
		private void UpdatePlayerTransformServerRPC(Vector3 playerPosition, Quaternion playerQuat, Quaternion camQuat)
		{
			UpdatePlayerTransformClientRPC(playerPosition, playerQuat, camQuat);
		}

		[ClientRpc]
		private void UpdatePlayerTransformClientRPC(Vector3 playerPosition, Quaternion playerQuat, Quaternion camQuat)
		{
			if (IsOwner) return;
			transform.position = playerPosition;
			transform.rotation = playerQuat;
			cameraTransform.localRotation = camQuat;
		}

		#endregion

		#region Logic Control Funcs

		private float horzRot = 0f;
		private float vertRot = 0f;

		Collider[] hitObjects;
		public void GroundedCheck()
		{
			Vector3 rectPosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
				transform.position.z);
			hitObjects = Physics.OverlapBox(rectPosition, groundRectSize, transform.rotation, Constants.LAYER_GROUND);
			isGrounded = hitObjects.Length > 0;
			ChangeAnimatorParam(animIdGrounded, isGrounded);

			if (!IsGrounded) return;

			transform.parent = hitObjects[0].transform.parent;		// 감지하면 Parent로 설정

			GroundBase gb = hitObjects[0].GetComponentInParent<GroundBase>();
			gravityType = gb.GravityType;
			gravityVector = gb.GetGravityVector();
		}

		public void UnsetParent()
		{
			transform.parent = null;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.blue;
			Gizmos.matrix = Matrix4x4.TRS(new Vector3(transform.position.x, transform.position.y - groundedOffset,
				transform.position.z), transform.rotation, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, groundRectSize * 2);
		}

		public void DetectIsGround()
		{
			if (isGrounded)
			{
				ChangeAnimatorParam(animIdGrounded, true);
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

		private bool isOnSlope = false;
		private bool isOnSlopeWhileFlying = false;
		private float slopeAngle = 0f;

		private Vector3 normalVector = Vector3.zero;

		public void SlopeCheck()
		{
			Debug.DrawRay(transform.position, -transform.up * slopeRayLength, Color.red);
			if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, slopeRayLength))
			{
				normalVector = hit.normal;
				slopeAngle = Vector3.Angle(hit.normal, -GetGravityDirection());

				float landAngle = Vector3.Angle(hit.normal, transform.up);
				isOnSlope = slopeAngle > slopeLimit;
				isOnSlopeWhileFlying = slopeAngle > slopeLimit || landAngle > enableToLandAngle;
			}
		}

		public bool OnSlope(bool isFlying = false)
		{
			return isFlying ? isOnSlopeWhileFlying : isOnSlope;
		}

		public void Jump(bool isJumpPressed)
		{
			if (!isJumpPressed) return;

			rigid.AddForce(transform.up * jumpForce, ForceMode.Impulse);
			stateMachine.ChangeState(jumpState);
		}

		public bool IsEnoughVelocityToLand()
		{
			return rigid.linearVelocity.magnitude < velocityToLand;
		}

		#endregion

		private Vector3 GetGravityDirection()
		{
			Vector3 downDir = Vector3.zero;
			switch (gravityType)
			{
				case GravityType.None:
					break;
				case GravityType.Point:
					downDir = Vector3.Normalize(gravityVector - transform.position);
					break;
				case GravityType.Direction:
					downDir = gravityVector;
					break;
			}
			return downDir;
		}

		#region Physics Control Funcs

		public void ApplyGravity()
		{
			if (!useGravity) return;

			Vector3 downDir = GetGravityDirection(); 
			
			projectedForward = Vector3.ProjectOnPlane(transform.forward, downDir).normalized;
			Quaternion targetRotation = Quaternion.LookRotation(projectedForward, -downDir);

			rigid.MoveRotation(targetRotation);
			rigid.AddForce(downDir * gravityForce, ForceMode.Acceleration);
		}

		public void WalkWithArrow(float vertInputRaw, float horzInputRaw, float diag)
		{
			Vector3 moveDir = (transform.forward * horzInputRaw + transform.right * vertInputRaw) * diag * walkSpeed;
			
			currentSpeed = moveDir.magnitude * walkSpeed * diag;
			rigid.AddForce(Vector3.ProjectOnPlane(moveDir, normalVector) * Time.fixedDeltaTime, ForceMode.VelocityChange);
			ChangeAnimatorParam(animIDSpeed, currentSpeed);
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

		public void TurnIdlePM()
		{
			capsule.material = idlePM;
		}
		public void TurnPlayerPM()
		{
			capsule.material = playerPM;
		}
		public void TurnSlopePM()
		{
			capsule.material = slopePM;
		}
		public void TurnFlyPM()
		{
			capsule.material = flyPM;
		}

		// 앞뒤/양옆/위아래 입력
		float keyWeight = 0.2f;
		public void Fly(float vert, float horz, float depth)
		{
			rigid.AddForce(transform.forward * vert * thrustPower, ForceMode.Force);
			rigid.AddForce(transform.right * horz * thrustPower, ForceMode.Force);
			rigid.AddForce(transform.up * depth * thrustPower, ForceMode.Force);
		}
		public void RotateBodyWithMouse(float mouseX, float mouseY, float roll)
		{
			cameraTransform.localRotation = Quaternion.Lerp(cameraTransform.localRotation, Quaternion.identity, 0.5f);

			rigid.AddTorque(transform.up * mouseX * rotationPower, ForceMode.Force);
			rigid.AddTorque(-transform.right * mouseY * rotationPower, ForceMode.Force);
			rigid.AddTorque(-transform.forward * roll * rotationPower * keyWeight, ForceMode.Force);
		}

		public void RotateWithMouse(float mouseX, float mouseY)
		{
			vertRot -= mouseY * vertRotSpeed;
			vertRot = Mathf.Clamp(vertRot, minVertRot, maxVertRot);
			rigid.AddTorque(transform.up * mouseX * rotationPower, ForceMode.Force);
			cameraTransform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);
		}
		#endregion

		#region Animation Synchronization

		public void ChangeAnimatorParam(int id, bool param)
		{
			animator.SetBool(id, param);
			ChangeAnimatorParamServerRPC(id, param);
		}
		public void ChangeAnimatorParam(int id, float param)
		{
			animator.SetFloat(id, param);
			ChangeAnimatorParamServerRPC(id, param);
		}

		[ServerRpc(RequireOwnership = false)]
		public void ChangeAnimatorParamServerRPC(int id, bool param)
		{
			ChangeAnimatorParamClientRPC(id, param);
		}
		[ServerRpc(RequireOwnership = false)]
		public void ChangeAnimatorParamServerRPC(int id, float param)
		{
			ChangeAnimatorParamClientRPC(id, param);
		}

		[ClientRpc]
		public void ChangeAnimatorParamClientRPC(int id, bool param)
		{
			if (IsOwner) return;
			animator.SetBool(id, param);
		}
		[ClientRpc]
		public void ChangeAnimatorParamClientRPC(int id, float param)
		{
			if (IsOwner) return;
			animator.SetFloat(id, param);
		}
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