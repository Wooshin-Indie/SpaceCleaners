using Unity.Netcode;
using UnityEngine;

namespace MPGame.Physics
{
	[RequireComponent(typeof(Rigidbody))]	
    public class PlanetBody : NetworkBehaviour
    {
		[Header("Planet")]
        [SerializeField] private bool isSun;            // true -> don't move
        [SerializeField] private float gravityScale;
		[SerializeField] private float gravityRadius;

		[Header("Orbit")]
		[SerializeField] private float orbitSpeed;
		[SerializeField] private float startAngle;
		[SerializeField] private float orbitRadius;
		[SerializeField] private float orbitAngle;

		private Rigidbody rigid;
		public Rigidbody Rigid { get => rigid; }

		public float GravityScale { get => gravityScale; }
		public float GravityRadius { get => gravityRadius; }
		public bool IsSun { get => isSun; }

		private float currentAngle = 0f;
		private void Awake()
		{
			rigid = GetComponent<Rigidbody>();
		}

		public void SetPlanetSize(float planetSize, float orbitRadius, float orbitAngle = 0f)
		{
			transform.localScale = planetSize * Vector3.one;
			gravityScale = Mathf.Sqrt(planetSize);
			gravityRadius = planetSize * 1.2f;

			this.orbitRadius = orbitRadius;
			orbitSpeed = 1000f / orbitRadius;

			this.orbitAngle = orbitAngle;

			startAngle = Random.Range(0f, 360f);
			currentAngle = startAngle;
		}

		// TODO - Orbit Viewer 필요함
		// HACK - 항성이 항상 (0, 0, 0) 에 존재한다고 가정하고 만듦
		private void FixedUpdate()
		{
			if (isSun || !IsHost) return;

			currentAngle += orbitSpeed * Time.fixedDeltaTime;
			currentAngle %= 360f;

			float radianAngle = currentAngle * Mathf.Deg2Rad;
			Vector3 localOrbitPosition = new Vector3(
				Mathf.Cos(radianAngle) * orbitRadius,
				0,
				Mathf.Sin(radianAngle) * orbitRadius
			);

			Quaternion orbitRotation = Quaternion.Euler(orbitAngle, 0, 0);
			Vector3 worldTargetPosition = orbitRotation * localOrbitPosition;

			Vector3 velocity = (worldTargetPosition - rigid.position) / Time.fixedDeltaTime;
			rigid.linearVelocity = velocity;

			rigid.angularVelocity = Vector3.zero;
		}
	}
}