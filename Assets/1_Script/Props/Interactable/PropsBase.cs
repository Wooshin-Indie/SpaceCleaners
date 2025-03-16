using Unity.Netcode;
using UnityEngine;

namespace MPGame.Props
{
    public class PropsBase : NetworkBehaviour
    {

		private NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>(ulong.MaxValue);
		public NetworkVariable<ulong> OwnerClientId { get => ownerClientId; }

        /// <summary>
        /// 외부에서 Interact 할 때 호출하는 함수
        /// </summary>
        public void TryInteract()
		{
			RequestOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
		}

		/// <summary>
		/// 몃 Interact   몄 ⑥
		/// </summary>
		public void EndInteraction()
		{
			RequestRemoveOwnershipServerRPC();
		}

		[ServerRpc(RequireOwnership = false)]
		private void RequestRemoveOwnershipServerRPC()
		{
			ownerClientId.Value = ulong.MaxValue;
        }

        [ServerRpc(RequireOwnership = false)]
		private void RequestOwnershipServerRpc(ulong requestingClientId)
		{
			Debug.Log(ownerClientId.Value + ", " + requestingClientId);
			if (ownerClientId.Value == ulong.MaxValue)
			{
				//NetworkObject.ChangeOwnership(requestingClientId);
				ownerClientId.Value = requestingClientId;
				GrantInteractionClientRpc(requestingClientId);
			}
			else
			{
				// TODO - 대�  寃쎌
			}
		}

		[ClientRpc]
		private void GrantInteractionClientRpc(ulong newOwnerClientId)
		{
			Interaction(newOwnerClientId);
		}

		protected virtual bool Interaction(ulong newOwnerClientId)
		{
			return NetworkManager.Singleton.LocalClientId == newOwnerClientId;
		}
	}
}