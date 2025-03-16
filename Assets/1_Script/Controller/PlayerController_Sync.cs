using UnityEngine;
using Unity.Netcode;

namespace MPGame.Controller
{
	/// <summary>
	/// Class for player's synchronization with RPCs.
	/// This class also include Client-Side Prediction (CSP) codes.
	/// </summary>
	public partial class PlayerController
	{
		private NetworkVariable<Vector3> networkPosition = new(writePerm: NetworkVariableWritePermission.Server);
		private NetworkVariable<Vector3> networkVelocity = new(writePerm: NetworkVariableWritePermission.Server);
		private NetworkVariable<Vector3> networkRotation = new(writePerm: NetworkVariableWritePermission.Server);

		private Vector3 predictedPosition;

		#region Transform RPC
		[ServerRpc(RequireOwnership = false)]
		public void UpdatePlayerLocalPositionServerRPC(Vector3 playerPosition)
		{
			UpdatePlayerLocalPositionClientRPC(playerPosition);
		}

		[ClientRpc]
		private void UpdatePlayerLocalPositionClientRPC(Vector3 playerPosition)
		{
			if (IsOwner) return;

			transform.localPosition = playerPosition;
		}

		[ServerRpc(RequireOwnership = false)]
		public void UpdatePlayerPositionServerRPC(Vector3 playerPosition)
		{
			UpdatePlayerPositionClientRPC(playerPosition);
		}

		[ClientRpc]
		private void UpdatePlayerPositionClientRPC(Vector3 playerPosition, bool fromServer = false)
		{
			if (!fromServer && IsOwner) return;

			rigid.linearVelocity = Vector3.zero;
			rigid.MovePosition(playerPosition);
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
			if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentId, out NetworkObject parentObject))
			{
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

		#endregion

		#region Rigidbody RPC
		[ServerRpc(RequireOwnership = false)]
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

		#region Animator RPC
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
	}
}
