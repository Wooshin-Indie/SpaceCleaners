using UnityEngine;

namespace MPGame.UI.GameScene
{
    public class PlayerHUD : MonoBehaviour
    {
        protected Camera playerCam;

        public void SetPlayerCam(Camera cam)
        {
            playerCam = cam;
        }
    }
}