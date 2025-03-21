using Hanzzz.MeshDemolisher;
using MPGame.Controller;
using MPGame.Utils;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

namespace MPGame.Props
{
	public class BreakableObject : SharableProp
	{
		[SerializeField]
		private Transform meshParent;

		private float objectHp = 100f;
		private bool isDestroyed = false;

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
		}

		public override void Interact(PlayerController player)
		{
			base.Interact(player);
			OnPickaxeToBreakServerRPC(34f);
		}

		[ServerRpc(RequireOwnership = false)]
		public void OnPickaxeToBreakServerRPC(float damage)
		{
			Debug.Log("Breakable Damaged : " + damage);
			if (isDestroyed) return;

			objectHp -= damage;
			if (objectHp <= 0)
			{
				isDestroyed = true;
				OnMeshBreak();
			}
		}

		public void OnMeshBreak()
		{
			GetComponent<MeshDemolisherExample>().Demolish();

			int count = meshParent.childCount;
			for (int i = count-1; i >= 0; i--)
			{
				Transform child = meshParent.GetChild(i);
				child.parent = null;
				child.AddComponent<BoxCollider>();
				child.AddComponent<Rigidbody>();
				child.GetComponent<Rigidbody>().useGravity = false;
				child.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;

				child.gameObject.layer = Constants.INT_VACUUMABLE;
				child.AddComponent<VacuumableObject>();
				child.AddComponent<NetworkObject>();
				child.AddComponent<NetworkTransform>();
				child.AddComponent<NetworkRigidbody>();

				child.GetComponent<NetworkObject>().Spawn();
			}

			GetComponent<NetworkObject>().Despawn();
			Destroy(gameObject);
		}
	}
}