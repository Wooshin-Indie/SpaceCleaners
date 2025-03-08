using UnityEngine;

namespace MPGame.Controller.StateMachine
{
    public class IdleState : StateBase
    {

        public IdleState(PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            vertInputRaw = horzInputRaw = 0f;
			controller.Rigidbody.linearDamping = 3f;
            controller.TurnIdlePM();
		}

        public override void Exit()
        {
            base.Exit();
		}


        public override void HandleInput()
        {
            base.HandleInput();

            GetMovementInputRaw(out vertInputRaw, out horzInputRaw);
            GetMouseInput(out mouseX, out mouseY);
            GetInteractableInput();
            GetJumpInput(out isJumpPrssed);
			GetFlyStateInput();
            GetEnableVacuumInput();
            GetVacuumInput(out isVacuumPressed);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (Mathf.Abs(vertInputRaw) >= 0.9f || Mathf.Abs(horzInputRaw) >= 0.9f) 
            {
                stateMachine.ChangeState(controller.walkState);
            }

			controller.DetectIsFalling();

            controller.Vacuuming();
            controller.RotateWithMouse(mouseX, mouseY);
            controller.Jump(isJumpPrssed);
		}

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            controller.RaycastInteractableObject();
			controller.WalkWithArrow(0f, 0f, 0f);
			controller.RotateWithMouse(mouseX, mouseY);
		}
    }
}
