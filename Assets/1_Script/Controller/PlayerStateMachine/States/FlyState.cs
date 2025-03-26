
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
            controller.Rigidbody.linearDamping = 0f;
        }

        public override void Exit()
        {
            base.Exit();
		}


        public override void HandleInput()
        {
            base.HandleInput();

            GetMovementInputRaw(out vertInputRaw, out horzInputRaw);
            GetUpDownInput(out isUpPressed, out isDownPressed);
            GetMouseInput(out mouseX, out mouseY);
            GetRollInput(out roll);
            GetInteractableInput();
            GetEnableVacuumInput();
            GetVacuumInput(out isVacuumPressed);
		}

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            controller.Vacuuming();
		}

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            controller.RaycastToInteractableObject();

			float depth = 0;
			if (isUpPressed == isDownPressed) depth = 0;
			else depth = isUpPressed ? 1 : -1;

			if (controller.IsHost)
            {
				controller.Move(vertInputRaw, horzInputRaw, depth);
				controller.RotateBodyWithMouse(mouseX, mouseY, roll);
			}
            else
            {
                controller.InputForPrediction(new PlayerController.ClientInput
                {
                    sequence = 0,
                    timestamp = 0,
                    moveDir = new UnityEngine.Vector3(vertInputRaw, horzInputRaw, depth),
                    rotateDir = new UnityEngine.Vector3(mouseX, mouseY, roll)
                });
            }

		}
    }
}
