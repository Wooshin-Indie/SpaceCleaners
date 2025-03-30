using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using MPGame.Manager;

namespace MPGame.Controller
{
    public class SpaceshipContoller : NetworkBehaviour
    {
		private Rigidbody rigid;

		public Rigidbody Rigidbody { get { return rigid; } }

		[Header("Movement Args")]
		[SerializeField] private float thrustPower;
		[SerializeField] private float rotationPower;
		[SerializeField] private float maxSpeed;

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			Debug.Log("SPAWNED");
		}

		private void Start()
		{
			rigid = GetComponent<Rigidbody>();
			rigid.maxLinearVelocity = maxSpeed;
		}

		private void Update()
		{
			if (!IsHost) return;
			// UpdateShipTransformClientRPC(transform.position, transform.rotation);
		}

		private List<PlayerController> insidePlayers = new List<PlayerController>();

		[SerializeField] private float playerGravity = 100f;

		private void FixedUpdate()
		{

        }


        void OnTriggerEnter(Collider other)
		{
			if (other.GetComponent<PlayerController>() == null) return;
			if (!insidePlayers.Contains(other.GetComponent<PlayerController>()))
			{
				insidePlayers.Add(other.GetComponent<PlayerController>());
				Debug.Log("insidePlayer added: " + other.GetComponent<NetworkBehaviour>().NetworkObjectId);
				if (other.GetComponent<PlayerController>().StateMachine.CurState
					== other.GetComponent<PlayerController>().flightState) return;
				other.GetComponent<PlayerController>().SetInShipState();
            }
		}

		void OnTriggerExit(Collider other)
		{
			if (other.GetComponent<PlayerController>() == null) return;
			if (insidePlayers.Contains(other.GetComponent<PlayerController>()))
			{
				insidePlayers.Remove(other.GetComponent<PlayerController>());
                if (other.GetComponent<PlayerController>().StateMachine.CurState
                    == other.GetComponent<PlayerController>().flightState) return;
                other.GetComponent<PlayerController>().SetFlyState();
            }
		}



		// 앞뒤/양옆/위아래 입력
		private float keyWeight = 0.2f;

		[ServerRpc (RequireOwnership = false)]
		public void FlyServerRPC(float vert, float horz, float depth)
		{
			if (Managers.Scene.CurrentScene?.SceneEnum == Utils.SceneEnum.Lobby) return;
			rigid.AddForce(transform.forward * vert * thrustPower, ForceMode.Acceleration);
			rigid.AddForce(transform.right * horz * thrustPower, ForceMode.Acceleration);
			rigid.AddForce(transform.up * depth * thrustPower, ForceMode.Acceleration);
		}

		[ServerRpc(RequireOwnership = false)]
		public void RotateBodyWithMouseServerRPC(float mouseX, float mouseY, float roll)
		{
			if (Managers.Scene.CurrentScene?.SceneEnum == Utils.SceneEnum.Lobby) return;
			rigid.AddTorque(transform.up * mouseX * rotationPower, ForceMode.Acceleration);
			rigid.AddTorque(-transform.right * mouseY * rotationPower, ForceMode.Acceleration);
			rigid.AddTorque(-transform.forward * roll * rotationPower * keyWeight, ForceMode.Acceleration);
		}

		#region Transform Synchronization

		[ClientRpc]
		private void UpdateShipTransformClientRPC(Vector3 playerPosition, Quaternion playerQuat)
		{
			if (IsHost) return;
			if (rigid == null) return;

			rigid.MovePosition(playerPosition);
			rigid.MoveRotation(playerQuat);
		}

		#endregion

	}
}