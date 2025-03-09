using Unity.Netcode;
using UnityEngine;

namespace MPGame.Controller
{
    public class SpaceshipContoller : NetworkBehaviour
    {
		private Rigidbody rigid;

		public Rigidbody Rigidbody { get { return rigid; } }

		[Header("Movement Args")]
		[SerializeField] private float thrustPower;
		[SerializeField] private float rotationPower;

		private void Start()
		{
			rigid = GetComponent<Rigidbody>();
			rigid.isKinematic = !IsHost;
		}

		private void Update()
		{
			if (!IsHost) return;
			UpdateShipTransformClientRPC(transform.position, transform.rotation);
		}

		// 앞뒤/양옆/위아래 입력
		private float keyWeight = 0.2f;

		[ServerRpc (RequireOwnership = false)]
		public void FlyServerRPC(float vert, float horz, float depth)
		{
			rigid.AddForce(transform.forward * vert * thrustPower, ForceMode.Acceleration);
			rigid.AddForce(transform.right * horz * thrustPower, ForceMode.Acceleration);
			rigid.AddForce(transform.up * depth * thrustPower, ForceMode.Acceleration);
		}

		[ServerRpc(RequireOwnership = false)]
		public void RotateBodyWithMouseServerRPC(float mouseX, float mouseY, float roll)
		{
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