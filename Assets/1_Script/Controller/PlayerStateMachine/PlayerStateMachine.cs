using Unity.Netcode;
using UnityEngine;

namespace MPGame.Controller.StateMachine
{
    public class PlayerStateMachine
    {
        private StateBase curState;
        public StateBase CurState { get=>curState;}
        private PlayerController Player;

        public void SetPlayerController(PlayerController PC)
        {
            Player = PC;
        }

        public void Init(StateBase state)
        {
            curState = state;
            curState.Enter();
        }

        public void ChangeState(StateBase newState)
        {
            curState.Exit();

            curState = newState;
            if(Player.IsHost)
                UpdateCurStateServerRPC(newState);

            curState.Enter();
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateCurStateServerRPC(StateBase clientState)
        {
            curState = clientState;
        }
    }
}