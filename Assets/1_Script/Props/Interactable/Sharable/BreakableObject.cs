using MPGame.Controller;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Props
{
	public class BreakableObject : SharableProp
	{
		[SerializeField]
		private Transform meshParent;

		[SerializeField]
		private List<GameObject> partPrefabs = new List<GameObject>();

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
			// GetComponent<MeshDemolisherExample>().Demolish();

			for (int i = 0; i < partPrefabs.Count; i++)
			{
				Transform part = Instantiate(partPrefabs[i], transform.position, transform.rotation).transform;

				part.parent = null;
				part.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
				part.GetComponent<NetworkObject>().Spawn();
			}

			GetComponent<NetworkObject>().Despawn();
			Destroy(gameObject);
		}
	}
}