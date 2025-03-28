using MPGame.Props;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Controller.StateMachine
{
	public class InShipState : StateBase
	{

        public InShipState(PlayerController controller, PlayerStateMachine stateMachine)
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
            GetInteractableInput();
            GetEnableVacuumInput();
            GetVacuumInput(out isVacuumPressed);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            controller.Vacuuming();
        }

        public override void PhysicsUpdate() // rotation.x, rotation.z를 spaceship.rotation에 맞추자
        {
            base.PhysicsUpdate();
            controller.RaycastToInteractableObject();

            float depth = 0;
            if (isUpPressed == isDownPressed) depth = 0;
            else depth = isUpPressed ? 1 : -1;

            if (controller.IsHost)
            {
                controller.PhysicsForInShip(vertInputRaw, horzInputRaw, depth, mouseX, mouseY);
            }
            else
            {
                controller.InputForPredictionInShip(new PlayerController.ClientInput
                {
                    sequence = 0,
                    timestamp = 0,
                    moveDir = new UnityEngine.Vector3(vertInputRaw, horzInputRaw, depth),
                    rotateDir = new UnityEngine.Vector3(mouseX, mouseY, 0)
                });
            }

        }
    }
}
