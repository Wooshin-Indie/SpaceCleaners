using MPGame.Props;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Controller.StateMachine
{
    public class StateBase
    {
        protected PlayerController controller;      // Needed to control player (ex. move)
        protected PlayerStateMachine stateMachine;

        /** Input Values **/
        protected float vertInput = 0f;
        protected float horzInput = 0f;
        protected float vertInputRaw = 0f;
        protected float horzInputRaw = 0f;
        protected float mouseX = 0f;
        protected float mouseY = 0f;
        protected float roll = 0f;

        protected bool isESCPressed = false;
        protected bool isJumpPrssed = false;
        protected static bool isVacuumEnabled = false;
        public static bool IsVacuumEnabled { get => isVacuumEnabled; }
        protected static bool isVacuumPressed = false;
        public static bool IsVacuumPressed { get => isVacuumPressed; }

        protected bool isUpPressed = false;
        protected bool isDownPressed = false;

        public StateBase(PlayerController controller, PlayerStateMachine stateMachine)
        {
            this.controller = controller;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter() { }             // Run once when Enter State
        public virtual void HandleInput() { }       // Manage Input in particular state
        public virtual void LogicUpdate()           // Logic Update  
		{

		}           
        public virtual void PhysicsUpdate()         // Only Physics Update
		{
		}     
        public virtual void Exit() { }              // Run once when Exit State


        #region Input Modules

        protected void GetMouseInput(out float mouseX, out float mouseY)
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
        }

        protected void GetRollInput(out float roll)
        {
            bool left = Input.GetKey(KeyCode.Q);
            bool right = Input.GetKey(KeyCode.E);
            if (left == right) roll = 0f;
            else roll = right ? 1f : -1f;
        }


		protected void GetMovementInputRaw(out float vert, out float horz)
        {
            vert = Input.GetAxisRaw("Vertical");
            horz = Input.GetAxisRaw("Horizontal");
        }

        protected void GetMovementInput(out float vert, out float horz)
        {
            vert = Input.GetAxis("Vertical");
            horz = Input.GetAxis("Horizontal");
        }

        protected void GetUpDownInput(out bool isUpPressed, out bool isDownPressed)
        {
            isUpPressed = Input.GetKey(KeyCode.Space);
            isDownPressed = Input.GetKey(KeyCode.LeftControl);
        }

        protected void GetInteractableInput()
        {
            if (!controller.IsDetectInteractable) return;   // TOOD - Interact 시 UI 띄워야됨

            if(Input.GetKeyDown(KeyCode.F) && controller.IsDetectInteractable)
            {
                if (controller.RecentlyDetectedProp is OwnableProp)
                    (controller.RecentlyDetectedProp as OwnableProp).TryInteract();

                if (controller.RecentlyDetectedProp is SharableProp)
					(controller.RecentlyDetectedProp as SharableProp).Interact(controller);
			}
        }

        protected void GetESCInput(out bool isEscPressed)
        {
            isEscPressed = Input.GetKeyDown(KeyCode.Escape);
        }

        protected void GetJumpInput(out bool isPressed)
        {
            isPressed = Input.GetKeyDown(KeyCode.Space);
        }

        protected void GetFlyStateInput()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				controller.StateMachine.ChangeState(controller.flyState);
			}
		}
        protected void GetEnableVacuumInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                isVacuumEnabled = !isVacuumEnabled;
            }
        }

        protected void GetVacuumInput(out bool isPressed)
        {
            isPressed = Input.GetMouseButton(0);
        }


        #endregion
    }

}
