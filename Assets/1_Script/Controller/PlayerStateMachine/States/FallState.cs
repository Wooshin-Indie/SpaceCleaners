using Unity.Collections;
using UnityEngine;

namespace MPGame.Controller.StateMachine
{
	public class FallState : StateBase
	{

		private float diagW;

		public FallState(PlayerController controller, PlayerStateMachine stateMachine)
			: base(controller, stateMachine)
		{
		}

		public override void Enter()
		{
			base.Enter();
			vertInputRaw = horzInputRaw = 0f;

			controller.Animator.SetBool(controller.animIdFreeFall, true);
		}

		public override void Exit()
		{
			base.Exit();
			
				controller.TurnPlayerPM();
			controller.Animator.SetBool(controller.animIdFreeFall, false);
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

			if (!controller.OnSlope())
			{
				controller.DetectIsGround();
				controller.TurnPlayerPM();
			}
			else
			{
				controller.TurnSlopePM();
			}

			controller.RotateWithMouse(mouseX, mouseY);
		}

		public override void PhysicsUpdate()
		{
			base.PhysicsUpdate();

			//diagW = (Mathf.Abs(horzInput) > 0.5f && Mathf.Abs(vertInput) > 0.5f) ? 0.71f : 1.0f;
			//controller.WalkWithArrow(horzInputRaw, vertInputRaw, diagW);
		}
	}
}
