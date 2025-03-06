using Unity.VisualScripting;
using UnityEngine;

namespace MPGame.Controller.StateMachine
{
	public class FlightState : StateBase
	{

		public SpaceshipContoller spaceShip;
		private Quaternion fixedRotation = Quaternion.identity;

		public FlightState(PlayerController controller, PlayerStateMachine stateMachine)
			: base(controller, stateMachine)
		{
		}

		private bool isOut = true;
		public override void Enter()
		{
			base.Enter();
			isOut = false;
			vertInputRaw = horzInputRaw = 0f;
			spaceShip = controller.Spaceship;
			if (spaceShip == null)
			{
				controller.TurnStateToIdleState();
			}
			controller.ChangeAnimatorParam(controller.animIdFreeFall, true);
			controller.Capsule.isTrigger = true;
			controller.Rigidbody.isKinematic = true;
			controller.cameraTransform.localRotation = Quaternion.identity;
			controller.transform.localPosition = spaceShip.enterPosition;

			controller.Rigidbody.constraints = RigidbodyConstraints.None;
		}

		public override void Exit()
		{
			base.Exit();
			isOut = true;

			spaceShip.EndInteraction();
			controller.Capsule.isTrigger = false;
			controller.Rigidbody.isKinematic = false;
			controller.transform.localPosition = spaceShip.exitPosition;
			spaceShip = null;

			controller.Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
		}

		public override void HandleInput()
		{
			base.HandleInput();

			GetMouseInput(out mouseX, out mouseY);
			GetRollInput(out roll);
			GetMovementInputRaw(out vertInputRaw, out horzInputRaw);
			GetUpDownInput(out isUpPressed, out isDownPressed);
			GetESCInput(out isESCPressed);
		}

		public override void LogicUpdate()
		{
			if (isOut) return;

			if (isESCPressed)
			{
				controller.TurnStateToIdleState();
				return;
			}

			controller.transform.localPosition = spaceShip.enterPosition;
			controller.transform.localRotation = fixedRotation;
		}

		public override void PhysicsUpdate()
		{
			base.PhysicsUpdate();

			if (spaceShip == null) return;

			float depth = 0;
			if (isUpPressed == isDownPressed) depth = 0;
			else depth = isUpPressed ? 1 : -1;
			spaceShip.FlyServerRPC(vertInputRaw, horzInputRaw, depth);
			spaceShip.RotateBodyWithMouseServerRPC(mouseX, mouseY, roll);
		}
	}
}
