using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace MPGame.Controller
{
	/// <summary>
	/// Class for player's synchronization with RPCs.
	/// This class also include Client-Side Prediction (CSP) codes.
	/// </summary>
	public partial class PlayerController
	{
		[System.Serializable]
		public struct ClientInput : INetworkSerializable
		{
			public int sequence;
			public Vector3 moveDir;
			public Vector3 rotateDir;
			public float timestamp;

			void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
			{
				serializer.SerializeValue(ref sequence);
				serializer.SerializeValue(ref timestamp);
				serializer.SerializeValue(ref moveDir);
				serializer.SerializeValue(ref rotateDir);
			}
		}

		// HACK - ALL
		// 임시로 일단 Client 는 호스트에 입력 넘기고 그 결과를 받아서 움직이도록 구현함
		// Delay 가 있긴할텐데 그건 Prediction으로 처리해서 부드럽게 만들예정

		private NetworkVariable<Vector3> networkPosition = new(writePerm: NetworkVariableWritePermission.Server);
		
		private int currentSequence = 0;
		private float lastProcessedTime = 0f;
		private Queue<ClientInput> inputQueue = new Queue<ClientInput>();

		private void OnFixedUpdateSync()
		{
			/*
			if (IsHost)
			{
				networkPosition.Value = rigid.position;
				networkRotation.Value = rigid.rotation;
			}
			else
			{
				rigid.position = networkPosition.Value;
				rigid.rotation = networkRotation.Value.normalized;
			}
			*/
			// if (!IsHost) return;

		}

		#region Input RPC

		/// <summary>
		/// Func to receive input for client-side prediction.
		/// It's for use in external classes.
		/// </summary>
		public void InputForPredictionFly(ClientInput input)
		{
			if (IsHost) return;

			// inputQueue.Enqueue(input);
			SendInputFlyServerRPC(input);
		}

        public void InputForPredictionFlight(ClientInput input)
        {
            if (IsHost) return;

            // inputQueue.Enqueue(input);
            SendInputFlightServerRPC(input);
        }

        public void InputForPredictionInShip(ClientInput input)
        {
            if (IsHost) return;

            // inputQueue.Enqueue(input);
            SendInputInShipServerRPC(input);
        }

        /// <summary>
        /// Send Input to Server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
		private void SendInputFlyServerRPC(ClientInput input)
		{
			PhysicsForFly(input.moveDir.x, input.moveDir.y, input.moveDir.z, input.rotateDir.x, input.rotateDir.y, input.rotateDir.z);

            // SendCorrectionClientRPC(rigid.position, input.sequence);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendInputFlightServerRPC(ClientInput input)
        {
			PhysicsForNoneDriverFlight(input.rotateDir.x, input.rotateDir.y);

            // SendCorrectionClientRPC(rigid.position, input.sequence);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendInputInShipServerRPC(ClientInput input)
        {
			PhysicsForInShip(input.moveDir.x, input.moveDir.y, input.moveDir.z, input.rotateDir.x, input.rotateDir.y);

            // SendCorrectionClientRPC(rigid.position, input.sequence);
        }

        /// <summary>
        /// Send "REAL" game state to client.
        /// Client need additional correction logics. (interpolate, reconciliation)
        /// </summary>
        [ClientRpc]
		private void SendCorrectionClientRPC(Vector3 correctedPos, int processedSequence)
		{


			/*
			if (!IsOwner) return;

			while (inputQueue.Count > 0 && inputQueue.Peek().sequence <= processedSequence)
				inputQueue.Dequeue();

			rigid.position = correctedPos;

			// Reconciliation
			foreach (var input in inputQueue)
			{
				Move(input.moveDir.x, input.moveDir.y, input.moveDir.z);
			}
			*/
		}


		#endregion

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
			if (IsHost) return;
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
			if (IsHost) return;

			rigid.MoveRotation(playerQuat);
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

				rigid.position = transform.position;
				rigid.rotation = transform.rotation;
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void UnsetParentServerRPC()
		{
			Debug.Log("UNSET");
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
