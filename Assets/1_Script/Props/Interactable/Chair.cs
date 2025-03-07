using MPGame.Controller;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Props
{
	/// <summary>
	/// 우주선 의자 prop임
	/// local 은 우주선 기준 위치임
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