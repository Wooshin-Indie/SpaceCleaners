using MPGame.Props;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace MPGame.Controller.StateMachine
{
	public class FlightState : StateBase
	{
		private Quaternion fixedRotation = Quaternion.identity;

		private Chair spaceChair;
		private SpaceshipContoller spaceShip;
		bool isDriver = false;

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
			spaceShip = spaceChair.GetComponentInParent<SpaceshipContoller>();
			if (spaceShip == null)
			{
				controller.TurnStateToFlyState();
			}

			controller.SetParentServerRPC(spaceShip.GetComponent<NetworkObject>().NetworkObjectId,
				spaceChair.localEnterPosition,
				spaceChair.transform.localRotation);

			controller.ChangeAnimatorParam(controller.animIdFreeFall, true);	// TODO - 앉는 모션으로 바꿔야됨
			controller.Capsule.isTrigger = true;
			controller.SetKinematic(true);

			controller.Rigidbody.constraints = RigidbodyConstraints.None;
	}

	public override void Exit()
		{
			base.Exit();
			isOutState = true;
			controller.transform.localPosition = spaceChair.localExitPosition;
			controller.transform.localRotation = spaceChair.transform.localRotation;
			
			controller.SetKinematic(false);
			controller.Rigidbody.linearVelocity = spaceShip.Rigidbody.linearVelocity;

			controller.Capsule.isTrigger = false;

			controller.Rigidbody.constraints = RigidbodyConstraints.None;
			spaceChair.EndInteraction();
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
				controller.TurnStateToFlyState();
				isESCPressed = false;
				return;
			}

			controller.UpdatePlayerLocalPositionServerRPC(spaceChair.localEnterPosition);
			controller.transform.localPosition = spaceChair.localEnterPosition;
			if (isDriver)
			{
				controller.transform.localRotation = fixedRotation;
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
				spaceShip.RotateBodyWithMouseServerRPC(mouseX, mouseY, roll);
			}
			else
			{
				controller.RotateWithoutRigidbody(mouseX, mouseY);
			}
		}
	}
}
