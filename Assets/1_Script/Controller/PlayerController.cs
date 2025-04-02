using MPGame.Controller.StateMachine;
using MPGame.Manager;
using MPGame.Physics;
using MPGame.Props;
using MPGame.UI.GameScene;
using MPGame.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;


namespace MPGame.Controller
{
	public partial class PlayerController : NetworkBehaviour
	{

		[Header("Player Movement")]

		[SerializeField, Tooltip("Maximum speed of player.")]
		private float maxFlySpeed;

		[SerializeField, Tooltip("Thrust power using fuel.")] 
		private float thrustPower;

		[SerializeField, Tooltip("Additional thrust power to escape gravity area of planets.")]
		private float thrustWeight;

		[SerializeField, Tooltip("Rotation power using fuel.")] 
		private float rotationPower;

		[SerializeField, Tooltip("Additional multiplier to reduce the gap between key and mouse input.")]
		private float rotationKeyWeight;

		[SerializeField, Tooltip("Additional multiplier to make camera rotation more responsive.")]
		private float rotationCameraWeight;

		[SerializeField, Range(0f, 90f), Tooltip("Maximum vertical rotation angle (looking up, in gravity).")]
		private float maxVertRot;

		[SerializeField, Range(-90f, 0f), Tooltip("Minimum vertical rotation angle (looking down, in gravity).")]
		private float minVertRot;


		[Header("Raycast Arguments")]
		
		[SerializeField, Tooltip("Raycast length for interacting with objects (looking forward, from camera).")] 
		private float interactRayLength;
		
		[SerializeField, Tooltip("Raycast length for detecting grounds (looking down, from (0,0,0)).")]
		private float groundRayLength;


		[Header("Transforms")]

		[SerializeField, Tooltip("Player's camera for control view dir (in gravity).")]
		public Transform cameraTransform;

		[SerializeField]
		private GameObject mapCameraPrefab;

		private GameObject mapCamera;

		/** Components **/
		private Rigidbody rigid;
		private CapsuleCollider capsule;
		private Animator animator;

		/** Component Properties **/
		public Rigidbody Rigidbody { get => rigid; }
		public CapsuleCollider Capsule { get => capsule; }
		public Animator Animator { get => animator; }

		/** Animation IDs **/
		[HideInInspector] public int animIDSpeed;
		[HideInInspector] public int animIDJump;
		[HideInInspector] public int animIDMotionSpeed;
		[HideInInspector] public int animIdGrounded;
		[HideInInspector] public int animIdFreeFall;

		/** Player State Machine **/
		private PlayerStateMachine stateMachine;

		public FlyState flyState;
		public FlightState flightState;
		public InShipState inShipState;

		public PlayerStateMachine StateMachine { get => stateMachine; }

		/** Radar HUD **/
		private PlayerHUD playerHUD;

        /** Player State Values **/
        private bool isGrounded = true;
		private bool isInGravity = false;
		private bool isDetectInteractable = false;
		
		public bool IsGrounded { get => isGrounded; }
		public bool IsDetectInteractable { get => isDetectInteractable; }
		
		private PropBase recentlyDetectedProp = null;
		public PropBase RecentlyDetectedProp { get => recentlyDetectedProp; }

		[SerializeField]
		private PlanetBody[] planets = null;
		private PlanetBody playerPlanet = null;

        [SerializeField]
        private Radarable[] radarables = null;

        private float camRotateSpeed = 10f;
		private bool previousGravityState = false;
		private float vertRot = 0;

		#region Lifecycle Funcs

		private void Awake()
		{
			rigid = GetComponent<Rigidbody>();
			animator = GetComponent<Animator>();
			capsule = GetComponent<CapsuleCollider>();

            stateMachine = new PlayerStateMachine();
			stateMachine.SetPlayerController(this);
            flyState = new FlyState(this, stateMachine);
			flightState = new FlightState(this, stateMachine);
			inShipState = new InShipState(this, stateMachine);

			playerHUD = GetComponent<PlayerHUD>();
			playerHUD.SetPlayerCam(cameraTransform.GetComponent<Camera>());

            animIDSpeed = Animator.StringToHash("Speed");
			animIDJump = Animator.StringToHash("Jump");
			animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
			animIdGrounded = Animator.StringToHash("Grounded");
			animIdFreeFall = Animator.StringToHash("FreeFall");
		}

		public void Start()
		{
			stateMachine.Init(flyState);
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			// TODO - Anim : basic anim
			cameraTransform.gameObject.SetActive(IsOwner);


			if (IsHost)
			{
				if (IsOwner)
				{
					EnvironmentSpawner.Instance.SpawnEnvironments();
					//EnvironmentSpawner.Instance.SpawnGalaxy();
					ObjectSpawner.Instance.SpawnTrashArea();
				}

				FindPlanets();
				FindRadarables();
      }
            
			if (IsOwner)
			{
				mapCamera = GameObject.Instantiate(mapCameraPrefab);
				mapCamera.gameObject.SetActive(false);
			}
		}

		private void Update()
		{
			if (IsOwner)
			{
				stateMachine.CurState.HandleInput();
				stateMachine.CurState.LogicUpdate();
				Debug.Log("CurState: " + stateMachine.CurState);

				OnUpdateRadar(); // LogicUpdate로 빼야함

                //HACK
                if (Input.GetKeyDown(KeyCode.Alpha3))
				{
					NetworkTransmission.instance.StartGameServerRPC();
				}
				else if (Input.GetKeyDown(KeyCode.Alpha4))
				{
					NetworkTransmission.instance.EndGameServerRPC();
				}
			}

			if (IsHost)
			{
				UpdatePlayerCamRotateServerRPC(cameraTransform.localRotation);
				// UpdatePlayerPositionClientRPC(transform.position);
				// UpdatePlayerRotateClientRPC(transform.rotation, cameraTransform.localRotation);
			}
		}

		private void FixedUpdate()
		{
			if (IsHost)
			{
				RaycastToGround();
				if (stateMachine.CurState != inShipState)
					ApplyGravity();
			}

			if (!IsOwner) return;

			stateMachine.CurState.PhysicsUpdate();
		}

		#endregion

		#region Player State Setters
		public void SetKinematic(bool isKinematic)
		{
			rigid.isKinematic = isKinematic;
			capsule.isTrigger = isKinematic;
			SetKinematicServerRPC(isKinematic);
		}

		public void SetFlyState()
		{
			stateMachine.ChangeState(flyState);
		}
		public void SetFlightState(Chair chair, bool isDriver)
		{
			flightState.SetParams(chair, isDriver);
			stateMachine.ChangeState(flightState);
		}
        public void SetInShipState()
        {
            stateMachine.ChangeState(inShipState);
        }
        #endregion

        /// <summary>
        /// Find Planets in current loaded scene.
        /// This MUST BE called at the start of the game.
        /// </summary>
        public void FindPlanets()
		{
			planets = FindObjectsByType<PlanetBody>(FindObjectsSortMode.None);
			if (planets == null)
			{
				Debug.LogError("Planets cannot be null.");
			}
		}

        /// <summary>
        /// Find Radarables in current loaded scene.
        /// This MUST BE called at the start of the game.
        /// </summary>
        public void FindRadarables()
        {
            radarables = FindObjectsByType<Radarable>(FindObjectsSortMode.None);
            if (radarables == null)
            {
                Debug.LogError("Planets cannot be null.");
            }
        }

        /// <summary>
        /// Apply gravity to player from planets. (FindPlanets Func must be called.)
        /// </summary>
        public void ApplyGravity()
		{
			if (planets == null) return;

			float maxMag = 0f;
			PlanetBody maxPlanet = null;

			bool nullExist = false;
			foreach (PlanetBody planet in planets)
			{
				if (planet == null)
				{
					nullExist = true;
					continue;
				}

				Vector3 positionDiff = planet.Rigid.position - rigid.position;
				float distanceSqr = positionDiff.sqrMagnitude;

				Vector3 newtonForce = positionDiff.normalized * Constants.CONST_GRAV * planet.Rigid.mass * rigid.mass / distanceSqr;
				newtonForce *= planet.GravityScale;
				
				newtonForce *= Time.fixedDeltaTime;

				rigid.AddForce(newtonForce);

				float mag = newtonForce.magnitude;
				if (maxMag < mag)
				{
					maxMag = mag;
					maxPlanet = planet;
				}
			}

			if (nullExist)
			{
				EraseNullInPlanets();
			}

			// 가장 강한 중력을 가진 행성 처리
			if (maxPlanet == null || maxPlanet.IsSun) return;

			if ((maxPlanet.Rigid.position - rigid.position).magnitude > maxPlanet.GravityRadius)
			{
				isInGravity = false;
				playerPlanet = null;
				AlignToCamera();
			}
			else
			{
				isInGravity = true;
				RotateTowardsPlanet(maxPlanet);
				playerPlanet = maxPlanet;
			}
		}

		private void EraseNullInPlanets()
		{
			if (planets != null)
			{
				List<PlanetBody> filteredPlanets = new List<PlanetBody>();

				foreach (var planet in planets)
				{
					if (planet != null)
					{
						filteredPlanets.Add(planet);
					}
				}

				planets = filteredPlanets.ToArray();
			}
		}

		/// <summary>
		/// Rotate Body toward planet when player enters planet's gravity field.
		/// </summary>
		private void RotateTowardsPlanet(PlanetBody planet)
		{
			Transform cachedTransform = transform;
			Quaternion cachedTransformRotation = cachedTransform.rotation;

			Vector3 cameraLookDirection = cameraTransform.forward;

			Vector3 gravityForceDirection = (cachedTransform.position - planet.Rigid.position).normalized;
			Vector3 playerUp = cachedTransform.up;
			Quaternion neededRotation = Quaternion.FromToRotation(playerUp, gravityForceDirection) * cachedTransformRotation;

			cachedTransformRotation = Quaternion.Slerp(cachedTransformRotation, neededRotation, Time.fixedDeltaTime);
			rigid.MoveRotation(cachedTransformRotation);

			cameraTransform.rotation = Quaternion.LookRotation(cameraLookDirection, cachedTransform.up);
		}

		/// <summary>
		/// Rotate Body to align with the camera when player exits planet's gravity field.
		/// </summary>
		private void AlignToCamera()
		{
			Transform cachedTransform = transform;
			Quaternion targetRotation = cameraTransform.rotation;

			rigid.MoveRotation(Quaternion.Slerp(cachedTransform.rotation, targetRotation, Time.fixedDeltaTime));

			Quaternion inverseRotation = Quaternion.Inverse(cachedTransform.rotation) * targetRotation;
			cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.identity, Time.fixedDeltaTime * 5f);
		}


		/// <summary>
		/// Shoot raycast from camera forward by interactRayLength
		/// </summary>
		public void RaycastToInteractableObject()
		{
			RaycastHit hit;
			int targetLayer = Constants.LAYER_INTERACTABLE;

			if (UnityEngine.Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactRayLength, targetLayer))
			{
				isDetectInteractable = true;
				recentlyDetectedProp = hit.transform.GetComponent<PropBase>();
			}
			else
			{
				isDetectInteractable = false;
				recentlyDetectedProp = null;
			}

			Debug.DrawRay(cameraTransform.position, cameraTransform.forward * interactRayLength, Color.red);
		}

		/// <summary>
		/// Shoot raycast from zero to downward by groundRayLength
		/// </summary>
		public void RaycastToGround()
		{
			RaycastHit hit;
			int targetLayer = Constants.LAYER_GROUND;
			float center = capsule.center.y;
			float height = capsule.height;
			Debug.DrawRay(transform.position - transform.up * (height * (0.5f) - center), -transform.up * groundRayLength, Color.red);

			if (UnityEngine.Physics.Raycast(transform.position - transform.up * (height*(0.5f) - center), -transform.up, out hit, groundRayLength, targetLayer))
			{
				isGrounded = true;
			}
			else
			{
				isGrounded = false;
			}
		}

        /// <summary>
        /// FlyState에서의 Move, Rotate 관리
        /// </summary>
        public void PhysicsForFly(float vert, float horz, float depth, float mouseX, float mouseY, float roll = 0)
		{
			Move(vert, horz, depth);
			RotateBodyInShipState(mouseX, mouseY, roll);
        }

        /// <summary>
        /// Input controll when isDriver is False.
        /// When isDriver is True, it's controlled in SpaceshipController.
		/// driver가 아닌 FlightState에서의 Move, Rotate 관리
        /// </summary>
        public void PhysicsForNoneDriverFlight(float mouseX, float mouseY)
        {
			RotateWithoutRigidbody(mouseX, mouseY);
        }

        /// <summary>
        /// InShipState에서의 Move, Rotate 관리
        /// </summary>
        public void PhysicsForInShip(float vert, float horz, float depth, float mouseX, float mouseY)
		{
            MoveInShip(vert, horz, depth);
            RotateBodyInShipState(mouseX, mouseY);
        }

		/// <summary>
		/// Movement Func in general state.
		/// Seperate fly and walk in this func.
		/// </summary>
		public void Move(float vert, float horz, float depth)
		{
			if (isInGravity && playerPlanet != null)
			{
				Vector3 moveDirection = (transform.forward * vert) + (transform.right * horz);
				moveDirection = moveDirection.normalized;

				Vector3 planetToPlayer = transform.position - playerPlanet.Rigid.position;
				Vector3 projectedMove = Vector3.ProjectOnPlane(moveDirection, planetToPlayer.normalized);

				Vector3 targetVelocity = projectedMove * thrustPower + playerPlanet.Rigid.linearVelocity;
				rigid.linearVelocity = Vector3.Lerp(rigid.linearVelocity, targetVelocity, Time.deltaTime);

				rigid.AddForce(transform.up * depth * thrustPower * thrustWeight, ForceMode.Force);
			}
			else
			{
				rigid.AddForce(transform.forward * vert * thrustPower, ForceMode.Force);
				rigid.AddForce(transform.right * horz * thrustPower, ForceMode.Force);
				rigid.AddForce(transform.up * depth * thrustPower, ForceMode.Force);
			}
		}

		private Vector3 shipVelocity;
		private Quaternion targetRotation;
		private Vector3 shipEulerAngle;
		private Vector3 inputVelocity;
		private Vector3 defaultDownVelocity;
		[SerializeField] private float thrustPowerInShip;
		[SerializeField] private float SlerpWeight;
        public void MoveInShip(float vert, float horz, float depth) // Movement controll in spaceship
		{
            shipVelocity = EnvironmentSpawner.Instance.CurrentSpaceship.GetComponent<Rigidbody>().linearVelocity;
            inputVelocity = ((transform.forward * vert) + (transform.right * horz)
				+ (2 * transform.up * depth)) * thrustPowerInShip;

            if (isGrounded)
                defaultDownVelocity = Vector3.zero;
			else
				defaultDownVelocity = -EnvironmentSpawner.Instance.CurrentSpaceship.GetComponent<Transform>().up * thrustPowerInShip;

            rigid.linearVelocity = shipVelocity + inputVelocity + defaultDownVelocity;

            shipEulerAngle = EnvironmentSpawner.Instance.CurrentSpaceship.GetComponent<Rigidbody>().
				rotation.eulerAngles;
			targetRotation = Quaternion.Euler(shipEulerAngle.x, rigid.rotation.eulerAngles.y, 
				shipEulerAngle.z);
            rigid.MoveRotation(Quaternion.Slerp(rigid.rotation, targetRotation, SlerpWeight * Time.fixedDeltaTime));
            // y축 방향으로만 플레이어가 회전하도록
        }

		/// <summary>
		/// Rotation Func in general state.
		/// </summary>
		public void RotateBodyWithMouse(float mouseX, float mouseY, float roll = 0)
		{

			if (isInGravity)		// TODO - erase local vars
			{
				Vector3 camRot = cameraTransform.localRotation.eulerAngles;
				float tmpV = camRot.x - mouseY * rotationPower;
				if (tmpV >= 180) tmpV -= 360;
				float rotValue = Mathf.Clamp(tmpV, minVertRot, maxVertRot);
				Quaternion targetRotation = Quaternion.Euler(rotValue, 0f, 0f);
				cameraTransform.localRotation = Quaternion.Lerp(cameraTransform.localRotation, targetRotation, Time.deltaTime * 10f);
			}
			else
			{
				rigid.AddTorque(-transform.right * mouseY * rotationPower * camRotateSpeed, ForceMode.Acceleration);
			}

			float rotationInput = mouseX * rotationPower * camRotateSpeed * Time.fixedDeltaTime;
			Quaternion deltaRotation = Quaternion.AngleAxis(rotationInput, Vector3.up);
			rigid.MoveRotation(rigid.rotation * deltaRotation);
			
			rigid.AddTorque(-transform.forward * roll * rotationPower * rotationKeyWeight, ForceMode.Acceleration);
		}

		/// <summary>
		/// Rotation Func while flight state (because flight state must be kinematic).
		/// </summary>
		public void RotateWithoutRigidbody(float mouseX, float mouseY)
		{
			vertRot -= mouseY * rotationPower;
			vertRot = Mathf.Clamp(vertRot, minVertRot, maxVertRot);
			cameraTransform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);

			Vector3 quat = transform.localRotation.eulerAngles;
			transform.localRotation = Quaternion.Euler(quat.x, quat.y + mouseX * rotationPower, quat.z);
		}

        /// <summary>
        /// Rotation Func while inShip state
        /// </summary>
        public void RotateBodyInShipState(float mouseX, float mouseY, float roll = 0)
        {
            Vector3 camRot = cameraTransform.localRotation.eulerAngles;
            float tmpV = camRot.x - mouseY * rotationPower;
            if (tmpV >= 180) tmpV -= 360;
            float rotValue = Mathf.Clamp(tmpV, minVertRot, maxVertRot);
            Quaternion targetRotation = Quaternion.Euler(rotValue, 0f, 0f);
            cameraTransform.localRotation = Quaternion.Lerp(cameraTransform.localRotation, targetRotation, Time.deltaTime * 10f);

            float rotationInput = mouseX * rotationPower * camRotateSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationInput, Vector3.up);
            rigid.MoveRotation(rigid.rotation * deltaRotation);

            rigid.AddTorque(-transform.forward * roll * rotationPower * rotationKeyWeight, ForceMode.Acceleration);
        }

		private bool isMapping = false;
		public bool IsMapping { get => isMapping; set => isMapping = value; }

		public void ToggleMapCamera()
		{
			if (isMapping) ChangeRenderCameraToPlayer();
			else ChangeRenderCameraToMap();
		}

		public void ChangeRenderCameraToPlayer()
		{
			cameraTransform.tag = "MainCamera";
			mapCamera.tag = "Untagged";

			cameraTransform.gameObject.SetActive(true);
			mapCamera.gameObject.SetActive(false);
			isMapping = false;
		}

		public void ChangeRenderCameraToMap()
		{
			cameraTransform.tag = "Untagged";
			mapCamera.tag = "MainCamera";
			
			mapCamera.GetComponent<MapCameraController>().SetStartTransform(transform.position, transform.rotation);
			cameraTransform.gameObject.SetActive(false);
			mapCamera.gameObject.SetActive(true);
			isMapping = true;
		}

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
        [SerializeField, Tooltip("Overlap Capsule Radius")]
        private float vacuumDetectRadius;
        [SerializeField, Tooltip("Overlap Capsule Length")]
        private float vacuumDetectLength;
		[SerializeField, Tooltip("Force to Vaccumable Object")]
		private float vacuumingForce;
		public float VacuumingForce { get => vacuumingForce; }
        [SerializeField, Tooltip("Force Vaccumable Object to Center of OverlapCapsule")]
        private float vacuumingForceToCenter;
        public float VacuumingForceToCenter { get => vacuumingForceToCenter; }
        [SerializeField, Tooltip("Go Destroy Process when Distance to Object gets Closer than This Value")]
		private float removeDistance;
        public float RemoveDistance { get => removeDistance; }
        [SerializeField]
        private LayerMask vacuumableLayers;
        private HashSet<VacuumableObject> prevDetected = new HashSet<VacuumableObject>(); //이전 프레임에 빨아들이고있던 물체들을	저장하는 HashSet
        private HashSet<VacuumableObject> currentDetected = new HashSet<VacuumableObject>();

        [Header("Absorption Settings")]
        private float absorbDistance = 1f;

        private bool isFirstVacuumingStarted = false;

        private Vector3 cameraPos;
        private Vector3 cameraForward;
		public Vector3 CameraForward { get => cameraForward; }
        private Vector3 detectingVector;

        public void Vacuuming()
        {
            if (!StateBase.IsVacuumEnabled)
            {
                return;
            }

            if (!StateBase.IsVacuumPressed)
            {
                if (isFirstVacuumingStarted)
                {
                    isFirstVacuumingStarted = false;

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

        private void DetectVacuumingObjects()
        {
            Debug.Log("Detecting");

            cameraPos = cameraTransform.position;
            cameraForward = cameraTransform.forward;
            detectingVector = cameraPos + cameraForward * vacuumDetectLength;

            Collider[] hitColliders = UnityEngine.Physics.OverlapCapsule(cameraTransform.position, detectingVector, vacuumDetectRadius, vacuumableLayers);

            foreach (var hitCollider in hitColliders)
            {
                if (!NetworkManager.SpawnManager.SpawnedObjectsList.Contains(hitCollider.GetComponent<NetworkObject>()))
                    continue;
                VacuumableObject cur = hitCollider.GetComponent<VacuumableObject>();
                currentDetected.Add(cur);
                cur.Init(GameManagerEx.Instance.MyClientId, cameraPos, cameraForward);
            }

            if (!isFirstVacuumingStarted)
            {
                isFirstVacuumingStarted = true;
                prevDetected = currentDetected;
                currentDetected.Clear();
                return;
            }

            foreach (VacuumableObject prev in prevDetected)
            {
                if (!currentDetected.Contains(prev)) // hitCollider에 있었다가 밖으로 나간 오브젝트들
                {
                    prev.VacuumEnd();
                }
            }

            foreach (var cur in currentDetected) // hitCollider에 없었다가 새로 들어온 오브젝트들
            {
                if (!prevDetected.Contains(cur))
                {
                }
            }

            prevDetected = new HashSet<VacuumableObject>(currentDetected);
            currentDetected.Clear();
        }

        public void RemoveVacuumingObjectsFromHashsets(VacuumableObject ob)
        {
            prevDetected.Remove(ob);
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

        #region Radar Funcs
        [SerializeField] private float RadaringTime;
        private bool isRadarActive = false;

		public void OnUpdateRadar()
		{
			if (!isRadarActive) return;

			playerHUD.OnUpdateRadarablesToScreen();
        }

        [ContextMenu("RadarSetUp")]
        private void RadarSetUp() // 레이더 버튼 처음 눌렀을 때 실행
        {
			if(!IsHost)
			{
                FindRadarables();
            }

            isRadarActive = true;

			playerHUD.ClearRadarablesOfHUD();
            playerHUD.AddRadarablesToHUD(radarables); // PlayerHUD에 전달
            // StartCoroutine(SetRadarTimer());
        }

        IEnumerator SetRadarTimer()
        {
            yield return new WaitForSeconds(RadaringTime);

            isRadarActive = false;
        }
        #endregion
    }
}