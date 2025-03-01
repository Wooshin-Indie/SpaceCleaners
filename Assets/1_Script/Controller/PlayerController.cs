using MPGame.Controller.StateMachine;
using MPGame.Utils;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


namespace MPGame.Controller
{
	public class PlayerController : NetworkBehaviour
	{

		/** Components **/
		private Rigidbody rigid;
		private CapsuleCollider capsuleColl;
		private Animator animator;


		/** Properties **/
		public Rigidbody Rigidbody { get => rigid; }
		public CapsuleCollider CapsuleColl { get { return capsuleColl; } }
		public Animator Animator { get => animator; }

		[Header("Player Move Args")]
		[SerializeField] private float walkSpeed;
		[SerializeField] private float horzRotSpeed;
		[SerializeField] private float vertRotSpeed;
		[SerializeField, Range(0f, 90f)] private float maxVertRot;
		[SerializeField, Range(-90f, 0f)] private float minVertRot;

		[Header("GameObjects")]
		[SerializeField] private Transform cameraTransform;

		[Header("Raycast Args")]                        // Use to detect Interactables
		[SerializeField] private float rayLength;


		private PlayerStateMachine stateMachine;
		public PlayerStateMachine StateMachine { get => stateMachine; }
		public IdleState idleState;
		public WalkState walkState;

		private void Awake()
		{
			rigid = GetComponent<Rigidbody>();
			capsuleColl = GetComponent<CapsuleCollider>();

			stateMachine = new PlayerStateMachine();
			idleState = new IdleState(this, stateMachine);
			walkState = new WalkState(this, stateMachine);
		}

		private void Start()
		{
			stateMachine.Init(idleState);
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

		[SerializeField] private float horzRot = 0f;
		[SerializeField] private float vertRot = 0f;
		public void RotateWithMouse(float mouseX, float mouseY)
		{
			horzRot += mouseX * horzRotSpeed;
			vertRot -= mouseY * vertRotSpeed;
			vertRot = Mathf.Clamp(vertRot, minVertRot, maxVertRot);

			transform.rotation = Quaternion.Euler(0f, horzRot, 0f);
			cameraTransform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);
			RotateCameraServerRPC(vertRot, horzRot);
		}

		[ServerRpc(RequireOwnership = false)]
		private void RotateCameraServerRPC(float vertRot, float horzRot)
		{
			UpdateMovementClientRPC(transform.rotation, cameraTransform.rotation);
		}

		[ClientRpc]
		private void UpdateMovementClientRPC(Quaternion playerQuat, Quaternion camQuat)
		{
			if (IsOwner) return;
			transform.rotation = playerQuat;
			cameraTransform.rotation = camQuat;
		}
		#endregion


		#region Physics Control Funcs

		public void WalkWithArrow(float vertInputRaw, float horzInputRaw, float diag)
		{
			Vector3 moveDir = (transform.forward * horzInputRaw + transform.right * vertInputRaw);

			rigid.MovePosition(transform.position + moveDir * diag * walkSpeed * Time.fixedDeltaTime);
			PlayerWalkServerRPC(moveDir, diag);
		}

		[ServerRpc(RequireOwnership = false)]
		private void PlayerWalkServerRPC(Vector3 moveDir, float diag)
		{
			FixedUpdateMovementClientRPC(moveDir, diag);
		}

		[ClientRpc]
		private void FixedUpdateMovementClientRPC(Vector3 moveDir, float diag)
		{
			if (IsOwner) return;
			rigid.MovePosition(transform.position + moveDir * diag * walkSpeed * Time.fixedDeltaTime);
		}

		private bool isDetectInteractable = false; 
		public bool IsDetectInteractable { get => isDetectInteractable; }
		private GameObject recentlyDetectedProp = null;
		public GameObject RecentlyDetectedProp { get => recentlyDetectedProp; }

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

		#endregion

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			cameraTransform.gameObject.SetActive(IsOwner);
		}
	}
}