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
			controller.ChangeAnimatorParam(controller.animIdFreeFall, true);
		}

		public override void Exit()
		{
			base.Exit();
			
				controller.TurnPlayerPM();
			controller.ChangeAnimatorParam(controller.animIdFreeFall, false);
		}


		public override void HandleInput()
		{
			base.HandleInput();


			GetMovementInput(out vertInput, out horzInput);
			GetMovementInputRaw(out vertInputRaw, out horzInputRaw);
			GetMouseInput(out mouseX, out mouseY);
            GetEnableVacuumInput();
            GetVacuumInput(out isVacuumPressed);
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

			controller.Vacuuming();
			controller.RotateWithMouse(mouseX, mouseY);
		}

		public override void PhysicsUpdate()
		{
			base.PhysicsUpdate();

			if(!controller.OnSlope())
			{
				diagW = (Mathf.Abs(horzInput) > 0.5f && Mathf.Abs(vertInput) > 0.5f) ? 0.71f : 1.0f;
				controller.WalkWithArrow(horzInputRaw, vertInputRaw, diagW);
			}
		}
	}
}
