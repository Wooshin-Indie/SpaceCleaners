using MPGame.Controller.StateMachine;
using MPGame.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Unity.Netcode;
using UnityEditor;
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

		public void Vacuuming()
		{
			if (!StateBase.IsVacuumEnabled)
			{
                return;
			}

            if (!StateBase.IsVacuumPressed)
			{
                if (isVacuumingStarted)
                {
                    isVacuumingStarted = false;

					foreach (var obj in prevDetected)
					{
						obj.VacuumEnd();
                    }
                    prevDetected.Clear();
                }
                return;
			}

            DetectVacuumingObjects();
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
        [SerializeField] private float vacuumDetectRadius;
        [SerializeField] private float vacuumDetectLength;
        [SerializeField] private float vacuumSpeed;
        [SerializeField] private LayerMask vacuumableLayers;
        private HashSet<VacuumableObject> prevDetected = new HashSet<VacuumableObject>(); //이전 프레임에 빨아들이고있던 물체들을	저장하는 HashSet

        [Header("Absorption Settings")]
        private float absorbDistance = 1f;

		private bool isVacuumingStarted = false;

        private Vector3 cameraPos;
        private Vector3 cameraForward;
        private Vector3 detectingVector;
        private void DetectVacuumingObjects()
        {
			if (!isVacuumingStarted)
			{
                isVacuumingStarted = true;
            }

			Debug.Log("Detecting");

            cameraPos = cameraTransform.position;
            cameraForward = cameraTransform.forward;
			detectingVector = cameraPos + cameraForward * vacuumDetectLength;

            Collider[] hitColliders = Physics.OverlapCapsule(cameraTransform.position, detectingVector, vacuumDetectRadius, vacuumableLayers);
			
			HashSet<VacuumableObject> currentDetected = new HashSet<VacuumableObject>();

            foreach (var hitCollider in hitColliders)
            {
				VacuumableObject cur = hitCollider.GetComponent<VacuumableObject>();
                currentDetected.Add(cur);
                cur.Init(cameraPos, cameraForward);
            }

			foreach (var cur in currentDetected)
			{
				if (!prevDetected.Contains(cur))
                {
					// 새로 들어온 오브젝트들
                }
            }

            foreach (VacuumableObject prev in prevDetected)
            {
                if (!currentDetected.Contains(prev))
                {
					prev.VacuumEnd();
                }
            }

            prevDetected = currentDetected;
        }

        // 선택된 상태에서만 Scene 뷰에 그리기
        private void OnDrawGizmosSelected()
        {
            if (cameraTransform == null)
                return;

            // 시작점과 끝점을 정의 (detectingVector가 월드 좌표라면 그대로, 로컬 좌표라면 변환 필요)
            Vector3 startPoint = cameraTransform.position;
            Vector3 endPoint = detectingVector;

            // Gizmos 색상 설정
            Gizmos.color = Color.green;
			#if UNITY_EDITOR
            DrawWireCapsule(startPoint, endPoint, vacuumDetectRadius);
			#endif
        }
        void DrawWireCapsule(Vector3 start, Vector3 end, float radius)
        {
            // 두 점 사이의 방향 및 거리 계산
            Vector3 direction = end - start;
            float height = direction.magnitude;
            Vector3 up = direction.normalized;

            // 시작점과 끝점에 원을 그립니다.
            Handles.DrawWireDisc(start, up, radius);
            Handles.DrawWireDisc(end, up, radius);

            // 원을 연결할 때 사용할 두 축(수평 방향) 결정
            Vector3 forward = Vector3.Cross(up, Vector3.right);
            if (forward == Vector3.zero)
            {
                forward = Vector3.Cross(up, Vector3.forward);
            }
            forward.Normalize();
            Vector3 right = Vector3.Cross(up, forward).normalized;

            // 원 둘레의 네 방향(상, 하, 좌, 우)으로 오프셋 계산
            Vector3 offset1 = forward * radius;
            Vector3 offset2 = -forward * radius;
            Vector3 offset3 = right * radius;
            Vector3 offset4 = -right * radius;

            // 각 원의 네 점을 연결하는 선분을 그립니다.
            Handles.DrawLine(start + offset1, end + offset1);
            Handles.DrawLine(start + offset2, end + offset2);
            Handles.DrawLine(start + offset3, end + offset3);
            Handles.DrawLine(start + offset4, end + offset4);
        }

        #endregion
    }
}