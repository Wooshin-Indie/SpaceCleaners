using UnityEngine;

namespace MPGame.Controller {

	public class MapCameraController : MonoBehaviour
	{
		public float minZoom = 5f;   
		public float maxZoom = 50f;  

		private Vector3 lastMousePos;
		private bool isDragging = false;

		private Vector3 targetPos;
		private Quaternion targetRot;

		[SerializeField] private Vector3 defaultTargetPos;
		[SerializeField] private Vector3 defaultTargetRot;

		[SerializeField] private float moveSpeed = 10f;
		[SerializeField] private float zoomSpeed = 5f; 

		private void Update()
		{
			HandleMouseDrag();
			HandleZoom();
			UpdateTransform();
		}

		public void SetStartTransform(Vector3 curPos, Quaternion curRot)
		{
			transform.position = curPos;
			transform.rotation = curRot;

			targetPos = defaultTargetPos;
			targetRot = Quaternion.Euler(defaultTargetRot);
		}

		private void HandleMouseDrag()
		{
			if (Input.GetMouseButtonDown(0))
			{
				lastMousePos = Input.mousePosition;
				isDragging = true;
			}

			else if (Input.GetMouseButtonUp(0))
			{
				isDragging = false;
			}

			if (isDragging)
			{
				Vector3 delta = Input.mousePosition - lastMousePos;

				Vector3 move = new Vector3(-delta.x, 0, -delta.y) * moveSpeed * Time.deltaTime;

				targetPos += transform.right * move.x;
				targetPos += transform.up * move.z;

				lastMousePos = Input.mousePosition;
			}
		}

		private void HandleZoom()
		{
			float scroll = Input.GetAxis("Mouse ScrollWheel");
			if (scroll != 0f)
			{
				Vector3 zoomDirection = transform.forward * scroll * zoomSpeed;
				Vector3 newTargetPos = targetPos + zoomDirection;

				targetPos = newTargetPos;
			}
		}


		private void UpdateTransform()
		{
			transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime);
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime);
		}
	}
}