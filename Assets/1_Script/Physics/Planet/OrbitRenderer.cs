using UnityEngine;

namespace MPGame.Physics
{
	[RequireComponent(typeof(LineRenderer))]
	public class OrbitRenderer : MonoBehaviour
	{
		private LineRenderer lineRenderer;

		public void DrawOrbit(int resolution, float orbitRadius, float orbitAngle)
		{
			lineRenderer = GetComponent<LineRenderer>();
			if (lineRenderer == null) return;


			lineRenderer.enabled = true;
			lineRenderer.startWidth = 10f;
			lineRenderer.endWidth = 10f;
			lineRenderer.positionCount = resolution + 1; 
			Vector3[] positions = new Vector3[resolution + 1];

			for (int i = 0; i <= resolution; i++)
			{
				float angle = i * 360f / resolution;
				float radianAngle = angle * Mathf.Deg2Rad;

				Vector3 localOrbitPosition = new Vector3(
					Mathf.Cos(radianAngle) * orbitRadius,
					0,
					Mathf.Sin(radianAngle) * orbitRadius
				);

				Quaternion orbitRotation = Quaternion.Euler(orbitAngle, 0, 0);
				Vector3 worldOrbitPosition = orbitRotation * localOrbitPosition;

				positions[i] = worldOrbitPosition;
			}

			lineRenderer.SetPositions(positions);
		}
		public void EraseOrbit()
		{
			if (lineRenderer == null) return;
			lineRenderer.enabled = false;
		}
	}
}