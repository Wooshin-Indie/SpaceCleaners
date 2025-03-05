using Unity.Netcode;
using UnityEngine;

namespace MPGame.Controller
{
    public class SpaceshipContoller : NetworkBehaviour
    {
		private Rigidbody rigid;

		[Header("Movement Args")]
		[SerializeField] private float thrustPower;
		[SerializeField] private float rotationPower;
		public Vector3 enterPosition;
		public Vector3 exitPosition;

		private NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>(ulong.MaxValue);

		private void Start()
		{
			rigid = GetComponent<Rigidbody>();
			rigid.isKinematic = !IsHost;
		}

		private void Update()
		{
			UpdatePlayerTransformServerRPC(transform.position, transform.rotation);
		}

		public void TryInteract()
		{
			RequestOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
		}

		[ServerRpc(RequireOwnership = false)]
		private void RequestOwnershipServerRpc(ulong requestingClientId)
		{
			if (ownerClientId.Value == ulong.MaxValue)
			{
				NetworkObject.ChangeOwnership(requestingClientId);
				ownerClientId.Value = requestingClientId;
				GrantInteractionClientRpc(requestingClientId);
			}
			else
			{
				// TODO - 이미 있는 경우
			}
		}

		[ClientRpc]
		private void GrantInteractionClientRpc(ulong newOwnerClientId)
		{
			if (NetworkManager.Singleton.LocalClientId == newOwnerClientId)
			{
				NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()
					.GetComponent<PlayerController>().TurnStateToFlightState();
			}
		}

		// 앞뒤/양옆/위아래 입력
		private float keyWeight = 0.2f;

		[ServerRpc (RequireOwnership = false)]
		public void FlyServerRPC(float vert, float horz, float depth)
		{
			rigid.AddForce(transform.forward * vert * thrustPower, ForceMode.Force);
			rigid.AddForce(transform.right * horz * thrustPower, ForceMode.Force);
			rigid.AddForce(transform.up * depth * thrustPower, ForceMode.Force);
		}

		[ServerRpc(RequireOwnership = false)]
		public void RotateBodyWithMouseServerRPC(float mouseX, float mouseY, float roll)
		{
			rigid.AddTorque(transform.up * mouseX * rotationPower, ForceMode.Force);
			rigid.AddTorque(-transform.right * mouseY * rotationPower, ForceMode.Force);
			rigid.AddTorque(-transform.forward * roll * rotationPower * keyWeight, ForceMode.Force);
		}

		#region Transform Synchronization

		[ServerRpc(RequireOwnership = false)]
		private void UpdatePlayerTransformServerRPC(Vector3 playerPosition, Quaternion playerQuat)
		{
			UpdatePlayerTransformClientRPC(playerPosition, playerQuat);
		}

		[ClientRpc]
		private void UpdatePlayerTransformClientRPC(Vector3 playerPosition, Quaternion playerQuat)
		{
			if (IsOwner) return;
			transform.position = playerPosition;
			transform.rotation = playerQuat;
		}

		#endregion

	}
}