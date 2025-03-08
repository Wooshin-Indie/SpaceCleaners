using MPGame.Controller.StateMachine;
using MPGame.Manager;
using MPGame.Props;
using MPGame.Utils;
using System;
using System.Runtime.CompilerServices;
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
		[SerializeField] private float walkForce;
		[SerializeField] private float maxWalkSpeed;
		[SerializeField] private float jumpForce;
		[SerializeField] private float gravityForce;
		[SerializeField] private bool useGravity;
		[SerializeField, Range(0f, 90f)] private float maxVertRot;
		[SerializeField, Range(-90f, 0f)] private float minVertRot;

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
		[SerializeField] private float maxFlySpeed;
		[SerializeField] private float thrustPower;
		[SerializeField] private float rotationPower;
		[SerializeField] private float velocityToLand;
		[SerializeField] private float enableToLandAngle;

		[Header("GameObjects")]
		[SerializeField] public Transform cameraTransform;

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
		public FlightState flightState;


		public bool UseGravity { get => useGravity; set => useGravity = value; }

		private bool isGrounded = true;
		private bool isDetectInteractable = false;

		public bool IsGrounded { get => isGrounded; }
		public bool IsDetectInteractable { get => isDetectInteractable; }

		private PropsBase recentlyDetectedProp = null;
		public PropsBase RecentlyDetectedProp { get => recentlyDetectedProp; }

		private Vector3 gravityVector = new Vector3(0f, 0f, 0f);
		private GravityType gravityType = GravityType.None;

		private Vector3 projectedForward = Vector3.zero;
		private float currentSpeed = 0f;

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
			flightState = new FlightState(this, stateMachine);

			animIDSpeed = Animator.StringToHash("Speed");
			animIDJump = Animator.StringToHash("Jump");
			animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
			animIdGrounded = Animator.StringToHash("Grounded");
			animIdFreeFall = Animator.StringToHash("FreeFall");
		}


		private void Start()
		{
			stateMachine.Init(idleState);
			SetMaxWalkSpeed();
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			cameraTransform.gameObject.SetActive(IsOwner);
			rigid.isKinematic = false;
			rigid.useGravity = false;
			ChangeAnimatorParam(animIDMotionSpeed, 1f);

			if (IsOwner && IsHost)
			{
				PlayerSpawner.Instance.SpawnEnvironments();
			}
		}

		private void Update()
		{
			if (!IsOwner) return;
			stateMachine.CurState.HandleInput();
			stateMachine.CurState.LogicUpdate();

			if (!rigid.isKinematic)
				UpdatePlayerPositionServerRPC(transform.position);
			UpdatePlayerRotateServerRPC(transform.rotation, cameraTransform.localRotation);
		}

		private void FixedUpdate()
		{
			if (!IsOwner) return;

			stateMachine.CurState.PhysicsUpdate();

		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.blue;
			Gizmos.matrix = Matrix4x4.TRS(new Vector3(transform.position.x, transform.position.y - groundedOffset,
				transform.position.z), transform.rotation, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, groundRectSize * 2);
		}

		#region Transform Synchronization

		[ServerRpc(RequireOwnership = false)]
		public void UpdatePlayerPositionServerRPC(Vector3 playerPosition)
		{
			UpdatePlayerPositionClientRPC(playerPosition);
		}

		[ClientRpc]
		private void UpdatePlayerPositionClientRPC(Vector3 playerPosition, bool fromServer = false)
		{
			if (!fromServer && IsOwner) return;
			transform.position = playerPosition;
		}

		[ServerRpc(RequireOwnership = false)]
		private void UpdatePlayerRotateServerRPC(Quaternion playerQuat, Quaternion camQuat)
		{
			UpdatePlayerRotateClientRPC(playerQuat, camQuat);
		}

		[ClientRpc]
		private void UpdatePlayerRotateClientRPC(Quaternion playerQuat, Quaternion camQuat, bool fromServer = false)
		{
			if (!fromServer && IsOwner) return;
			transform.rotation = playerQuat;
			cameraTransform.localRotation = camQuat;
		}

		[ServerRpc(RequireOwnership = false)]
		public void SetParentServerRPC(ulong parentId)
		{
			if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentId, out NetworkObject parentObject)){
				transform.parent = parentObject.transform;
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void SetParentServerRPC(ulong parentId, Vector3 localPos, Quaternion localRot)
		{
			if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentId, out NetworkObject parentObject))
			{
				transform.parent = parentObject.transform;
				transform.localPosition = localPos;
				transform.localRotation = localRot;

				UpdatePlayerPositionClientRPC(transform.position, true);
				UpdatePlayerRotateClientRPC(transform.rotation, Quaternion.identity, true);
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void UnsetParentServerRPC()
		{
			transform.parent = null;
		}

		public void SetKinematic(bool isKinematic)
		{
			rigid.isKinematic = isKinematic;
			capsule.isTrigger = isKinematic;
			SetKinematicServerRPC(isKinematic);
		}

		[ServerRpc (RequireOwnership = false)]
		private void SetKinematicServerRPC(bool isKinematic)
		{
			SetKinematicClientRPC(isKinematic);
		}

		[ClientRpc]
		private void SetKinematicClientRPC(bool isKinematic)
		{
			if (IsOwner) return;
			rigid.isKinematic = isKinematic;
			capsule.isTrigger = isKinematic;
		}


		#endregion

		#region Logic Control Funcs

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
			Vector3 localVelocity = transform.InverseTransformDirection(rigid.linearVelocity);

			if (localVelocity.y <= 0f) // 로컬 기준 y축(=위쪽) 속도가 0 이하이면 하강 중
			{
				stateMachine.ChangeState(fallState);
			}
		}

		private bool isOnSlope = false;					// Check if ground is disable to walk
		private bool isOnSlopeWhileFlying = false;		// Check if ground is disable to land
		private float slopeAngle = 0f;					
		private Vector3 normalVector = Vector3.zero;	// Slope angle (hit.normal)
		public void SlopeCheck()
		{
			Debug.DrawRay(transform.position, -transform.up * slopeRayLength, Color.red);
			if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, slopeRayLength))
			{
				normalVector = hit.normal;
				slopeAngle = Vector3.Angle(hit.normal, -gravityDirection);

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

		#region Physics Control Funcs

		Vector3 gravityDirection = Vector3.zero;
		Collider[] hitObjects;
		SpaceshipContoller spaceship = null;

		public SpaceshipContoller Spaceship { get => spaceship; }
		public void GroundedCheck()
		{
			Vector3 rectPosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
				transform.position.z);
			hitObjects = Physics.OverlapBox(rectPosition, groundRectSize, transform.rotation, Constants.LAYER_GROUND);
			isGrounded = hitObjects.Length > 0;

			if (!IsGrounded) return;

			spaceship = hitObjects[0].transform.parent.GetComponent<SpaceshipContoller>();
			SetParentServerRPC(hitObjects[0].transform.parent.GetComponent<NetworkObject>().NetworkObjectId);

			GroundBase gb = hitObjects[0].GetComponentInParent<GroundBase>();
			gravityType = gb.GravityType;
			gravityVector = gb.GetGravityVector();

		}

		public void CalculateGravity()
		{
			switch (gravityType)
			{
				case GravityType.Point:
					gravityDirection = Vector3.Normalize(gravityVector - transform.position);
					break;
				case GravityType.Direction:
					gravityDirection = gravityVector;
					break;
			}
		}

		public void ApplyGravity()
		{
			if (!useGravity) return;
			
			projectedForward = Vector3.ProjectOnPlane(transform.forward, gravityDirection).normalized;
			Quaternion targetRotation = Quaternion.LookRotation(projectedForward, -gravityDirection);

			rigid.MoveRotation(targetRotation);		// HACK - AddTorque로 바꾸면 좋음
			rigid.AddForce(gravityDirection * gravityForce, ForceMode.Acceleration);
		}

		public void WalkWithArrow(float vertInputRaw, float horzInputRaw, float diag)
		{
			Vector3 moveDir = (transform.forward * horzInputRaw + transform.right * vertInputRaw) * diag * walkForce;
			
			currentSpeed = moveDir.magnitude;
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
				Debug.Log(hit.transform.name);
				recentlyDetectedProp = hit.transform.GetComponent<PropsBase>();
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
		public void SetMaxWalkSpeed()
		{
			rigid.maxLinearVelocity = maxWalkSpeed;
		}
		public void SetMaxFlySpeed()
		{
			rigid.maxLinearVelocity = maxFlySpeed;
		}

		public void TurnStateToIdleState()
		{
			stateMachine.ChangeState(idleState);
		}
		public void TurnStateToFlyState()
		{
			stateMachine.ChangeState(flyState);
		}
		public void TurnStateToFlightState(Chair chair, bool isDriver)
		{
			flightState.SetParams(chair, isDriver);
			stateMachine.ChangeState(flightState);
		}

		// 앞뒤/양옆/위아래 입력
		private float keyWeight = 0.2f;
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

		private float vertRot = 0f;
		public void RotateWithMouse(float mouseX, float mouseY)
		{
			vertRot -= mouseY * rotationPower;
			vertRot = Mathf.Clamp(vertRot, minVertRot, maxVertRot);
			rigid.AddTorque(transform.up * mouseX * rotationPower, ForceMode.Force);
			cameraTransform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);
		}

		public void RotateWithoutRigidbody(float mouseX, float mouseY)
		{
			vertRot -= mouseY * rotationPower;
			vertRot = Mathf.Clamp(vertRot, minVertRot, maxVertRot);
			cameraTransform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);

			Vector3 quat = transform.localRotation.eulerAngles;
			transform.localRotation = Quaternion.Euler(quat.x, quat.y + mouseX * rotationPower, quat.z);
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