using MPGame.Manager;
using MPGame.Props;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Controller.StateMachine
{
	public class FlightState : StateBase
	{
		private Quaternion fixedRotation = Quaternion.identity;

		private Chair spaceChair;
		private SpaceshipContoller spaceShip;
		bool isDriver = false;  // Cockpit = true, Passenger = false

        public void SetParams(Chair chair, bool isDriver)
		{
			spaceChair = chair;
			this.isDriver = isDriver;
		}

		public FlightState(PlayerController controller, PlayerStateMachine stateMachine)
			: base(controller, stateMachine)
		{
		}

		private bool isOutState = true;

		public override void Enter()
		{
			base.Enter();
			isOutState = false;
			vertInputRaw = horzInputRaw = 0f;
			Debug.Log("Entered Flight");
			spaceShip = spaceChair.GetComponentInParent<SpaceshipContoller>();
			if (spaceShip == null)
            {
                Debug.Log("Spaceship Null");
                controller.SetFlyState();
			}

			controller.SetParentServerRPC(spaceShip.GetComponent<NetworkObject>().NetworkObjectId,
				spaceChair.localEnterPosition,
				spaceChair.transform.localRotation);

			// TODO - Anim (sit)
			controller.SetKinematic(true);

			NetworkTransmission.instance.IsTheClientReadyServerRPC(true, GameManagerEx.Instance.MyClientId);
		}

		public override void Exit()
		{
			base.Exit();
			isOutState = true;

			controller.transform.localPosition = spaceChair.localExitPosition;
			controller.GetComponent<Rigidbody>().position = controller.transform.position;
			controller.transform.localRotation = spaceChair.transform.localRotation;
			controller.GetComponent<Rigidbody>().rotation = controller.transform.rotation;

			controller.SetKinematic(false);
			controller.Rigidbody.linearVelocity = spaceShip.Rigidbody.linearVelocity;

			spaceChair.EndInteraction(); 
			NetworkTransmission.instance.IsTheClientReadyServerRPC(false, GameManagerEx.Instance.MyClientId);

			controller.UnsetParentServerRPC();

			if (controller.IsMapping)
			{
				controller.ChangeRenderCameraToPlayer();
			}
		}

		public override void HandleInput()
		{
			base.HandleInput();
			if (isOutState) return;
			
			GetMouseInput(out mouseX, out mouseY);
			GetESCInput(out isESCPressed);

			if (isDriver)
			{
				GetRollInput(out roll);
				GetMovementInputRaw(out vertInputRaw, out horzInputRaw);
				GetUpDownInput(out isUpPressed, out isDownPressed);
			}
		}

		public override void LogicUpdate()
		{
			if (isOutState) return;

			if (isESCPressed)
            {
                Debug.Log("ESC Pressed while Flight");
                controller.SetInShipState();
				isESCPressed = false;
				return;
			}

			controller.transform.localPosition = spaceChair.localEnterPosition;

			if (isDriver)
			{
				controller.transform.localRotation = fixedRotation;

				if (Input.GetKeyDown(KeyCode.M))
				{
					controller.ToggleMapCamera();
				}
			}
		}

		public override void PhysicsUpdate()
		{
			base.PhysicsUpdate();

			if (isOutState) return;
			if (spaceShip == null) return;

			if (isDriver)
            {
                float depth = 0;
                if (isUpPressed == isDownPressed) depth = 0;
                else depth = isUpPressed ? 1 : -1;
                spaceShip.FlyServerRPC(vertInputRaw, horzInputRaw, depth);
                spaceShip.RotateBodyWithMouseServerRPC(mouseX, mouseY, roll); //isHost에서도 serverRPC로 실행됨
				Debug.Log("Outside Of ServerRPC");
            }
			else
			{
				if (controller.IsHost)
				{
					controller.PhysicsForNoneDriverFlight(mouseX, mouseY);
				}
				else
				{
                    controller.InputForPredictionFlight(new PlayerController.ClientInput
                    {
                        sequence = 0,
                        timestamp = 0,
                        moveDir = Vector3.zero,
                        rotateDir = new UnityEngine.Vector3(mouseX, mouseY, roll = 0)
                    });
                }
			}
        }
	}
}
