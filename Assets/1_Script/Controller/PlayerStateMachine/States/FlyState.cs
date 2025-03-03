using UnityEngine;

namespace MPGame.Controller.StateMachine
{
    public class FlyState : StateBase
    {

        public FlyState(PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
			vertInputRaw = horzInputRaw = 0f;
            controller.Rigidbody.constraints = RigidbodyConstraints.None;
            controller.Rigidbody.linearDamping = 0f;
            controller.UseGravity = false;
            controller.TurnFlyPM();
            controller.UnsetParent();       // 날기 시작하면 Parent 없앰
        }

        public override void Exit()
        {
            base.Exit();
            controller.Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
			controller.Rigidbody.linearDamping = 1f;
			controller.UseGravity = true;
			controller.TurnPlayerPM();
		}


        public override void HandleInput()
        {
            base.HandleInput();

            GetMovementInputRaw(out vertInputRaw, out horzInputRaw);
            GetUpDownInput(out isUpPressed, out isDownPressed);
            GetMouseInput(out mouseX, out mouseY);
            GetRollInput(out roll);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (!controller.OnSlope() && controller.IsEnoughVelocityToLand())
            {
                controller.DetectIsGround();
            }
		}

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            controller.RaycastInteractableObject();
			controller.RotateBodyWithMouse(mouseX, mouseY, roll);

            float depth = 0;
            if (isUpPressed == isDownPressed) depth = 0;
            else depth = isUpPressed ? 1 : -1;
            controller.Fly(vertInputRaw, horzInputRaw, depth);
		}
    }
}
