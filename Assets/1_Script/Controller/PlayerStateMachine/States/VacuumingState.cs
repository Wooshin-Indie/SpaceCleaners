using UnityEngine;

namespace MPGame.Controller.StateMachine
{
    public class VacuumingState : StateBase
    {

        private float diagW;
        private float speed;
        public VacuumingState(PlayerController controller, PlayerStateMachine stateMachine)
            : base(controller, stateMachine)
        {
        }

        
    }
}
