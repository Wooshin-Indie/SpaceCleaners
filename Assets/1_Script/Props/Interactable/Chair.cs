using MPGame.Controller;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Props
{
	/// <summary>
	/// ���ּ� ���� prop��
	/// local �� ���ּ� ���� ��ġ��
	/// </summary>
    public class Chair : PropsBase
	{
		public Vector3 localEnterPosition;
		public Vector3 localExitPosition;
		[SerializeField] private bool isDriver;

		protected override void Interaction(ulong newOwnerClientId)
		{
			base.Interaction(newOwnerClientId);

			NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()
				.GetComponent<PlayerController>().TurnStateToFlightState(this, isDriver);
		}
	}
}