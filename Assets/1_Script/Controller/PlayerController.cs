using MPGame.Controller.StateMachine;
using MPGame.Utils;
using System.Collections.Generic;
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
		}

		private void Update()
		{
			if (!IsOwner) return;

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
		public void RotateWithMouse(float mouseX, float mouseY)
		{
			horzRot += mouseX * horzRotSpeed;
			vertRot -= mouseY * vertRotSpeed;
			vertRot = Mathf.Clamp(vertRot, minVertRot, maxVertRot);

			transform.rotation = Quaternion.Euler(0f, horzRot, 0f);
			cameraTransform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);
		}

		public void GroundedCheck()
		{
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
				transform.position.z);
			isGrounded = Physics.CheckBox(spherePosition, groundRectSize, Quaternion.identity,
				Constants.LAYER_GROUND,
				QueryTriggerInteraction.Ignore);

			ChangeAnimatorParam(animIdGrounded, isGrounded);
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
				ChangeAnimatorParam(animIdGrounded, true);
				stateMachine.ChangeState(idleState);
			}
		}

		public void DetectIsFalling()
		{
			if (isGrounded) return;
			if (rigid.linearVelocity.y < 0f)
			{
				stateMachine.ChangeState(fallState);
			}
		}

		public bool OnSlope()
		{
			Debug.DrawRay(transform.position, Vector3.down* slopeRayLength, Color.red);
			if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeRayLength))
			{
				float angle = Vector3.Angle(hit.normal, Vector3.up);
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

        #region Vacuum Funcs

        [Header("Vacuum Settings")]
        private float vacuumRadius = 10f;
        private float vacuumSpeed = 5f;
        private LayerMask vacuumableLayers;
        private KeyCode vacuumKey = KeyCode.Space;

        [Header("Absorption Settings")]
        private float absorbDistance = 1f;
        private Transform absorbPoint;

		private bool isVacuumEnabled = false;
		private bool isVacuuming = false;
        private List<VacuumableObject> targetObjects = new List<VacuumableObject>();

        private void OnUpdateVacuumFunc()
        {
            if (Input.GetMouseButton(0))
            {
                StartVacuuming();
            }
            else
            {
                StopVacuuming();
            }

            if (isVacuuming)
            {
                DetectNewObjects();
                MoveObjectsTowardsPlayer();
            }
        }

        private void StartVacuuming()
        {
            isVacuuming = true;

        }

        private void StopVacuuming()
        {
            isVacuuming = false;

        }

        private void DetectNewObjects()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, vacuumRadius, vacuumableLayers);

            foreach (var hitCollider in hitColliders)
            {
                // 이미 처리 중인 오브젝트는 건너뛰기
                if (targetObjects.Exists(obj => obj.ObjectCollider == hitCollider))
                    continue;

                // VacuumableObject 컴포넌트 확인 또는 추가
                VacuumableObject vacObj = hitCollider.GetComponent<VacuumableObject>();
                if (vacObj == null)
                {
                    vacObj = hitCollider.gameObject.AddComponent<VacuumableObject>();
                }

                // 리스트에 추가
                targetObjects.Add(vacObj);

                // 필요하다면 초기 설정
                vacObj.Initialize(absorbPoint);
            }
        }

        private void MoveObjectsTowardsPlayer()
        {
            for (int i = targetObjects.Count - 1; i >= 0; i--)
            {
                VacuumableObject obj = targetObjects[i];

                // 오브젝트가 유효한지 확인
                if (obj == null || obj.gameObject == null)
                {
                    targetObjects.RemoveAt(i);
                    continue;
                }

                // 거리 계산
                float distance = Vector3.Distance(obj.transform.position, absorbPoint.position);

                // 흡수 거리에 도달했다면
                if (distance <= absorbDistance)
                {
                    AbsorbObject(obj);
                    targetObjects.RemoveAt(i);
                    continue;
                }

                // 거리에 따른 속도 조절 (가까울수록 빨라짐)
                float speedMultiplier = 1f + (vacuumRadius - distance) / vacuumRadius;
                float currentSpeed = vacuumSpeed * speedMultiplier * Time.deltaTime;

                // 오브젝트 이동
                obj.MoveTowards(absorbPoint.position, currentSpeed);
            }
        }

        #endregion
    }
}