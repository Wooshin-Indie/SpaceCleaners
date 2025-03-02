using UnityEngine;

namespace MPGame.Controller.StateMachine
{
	public class JumpState : StateBase
	{

		private float diagW;
		private float speed;
		public JumpState(PlayerController controller, PlayerStateMachine stateMachine)
			: base(controller, stateMachine)
		{
		}

		public override void Enter()
		{
			base.Enter();
			vertInputRaw = horzInputRaw = 0f;
			controller.ChangeAnimatorParam(controller.animIDJump, true);
		}

		public override void Exit()
		{
			base.Exit();
			controller.ChangeAnimatorParam(controller.animIDJump, false);
		}


		public override void HandleInput()
		{
			base.HandleInput();

			GetMovementInput(out vertInput, out horzInput);
			GetMovementInputRaw(out vertInputRaw, out horzInputRaw);
			GetMouseInput(out mouseX, out mouseY);
		}

		public override void LogicUpdate()
		{
			base.LogicUpdate();

			controller.DetectIsFallingWhileJump();

			controller.RotateWithMouse(mouseX, mouseY);
		}

		public override void PhysicsUpdate()
		{
			base.PhysicsUpdate();

			diagW = (Mathf.Abs(horzInput) > 0.5f && Mathf.Abs(vertInput) > 0.5f) ? 0.71f : 1.0f;
			controller.WalkWithArrow(horzInputRaw, vertInputRaw, diagW);
		}
	}
}
