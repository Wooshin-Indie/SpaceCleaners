using MPGame.Controller;
using System.Collections.Generic;
using UnityEngine;

namespace MPGame.UI.GameScene
{
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private Canvas HUDCanvas;

        #region RadarHUD
        [SerializeField] private GameObject radarCirclePrefab;
        private PlayerController player;
        private Camera playerCam;
        private List<GameObject> radarables = new List<GameObject>();
        private List<GameObject> radarCircles = new List<GameObject>();

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        public void SetPlayerCam(Camera cam)
        {
            playerCam = cam;
        }

        public void ClearRadarablesOfHUD()
        {
            radarables.Clear();

            for (int i = radarCircles.Count - 1; i >= 0; i--)
                Destroy(radarCircles[i]);

            radarCircles.Clear();
        }

        public void AddRadarablesToHUD(Radarable[] obs)
        {
            for (int i = 0; i < obs.Length; i++)
            {
                radarables.Add(obs[i].gameObject);

                GameObject tmpCircle = Instantiate(radarCirclePrefab);
                tmpCircle.transform.SetParent(HUDCanvas.transform); // HUDCanvas의 자식으로 설정
                radarCircles.Add(tmpCircle);
                tmpCircle.SetActive(false);
            }
        }

        private Vector3 tmpScreenPos;
        private float tmpDistance;
        public void OnUpdateRadarablesToScreen()
        {
            int i;
            for (i = 0; i < radarables.Count; i++)
            {
                tmpScreenPos = playerCam.WorldToScreenPoint(radarables[i].transform.position);
                if (tmpScreenPos.z > 0) // 물체가 카메라 화면 앞에 있을 때
                {
                    // radarCircle에 행성과의 거리, 스크린 좌표를 업데이트함

                    if (!radarCircles[i].activeSelf) // 꺼져있으면 켜줌
                        radarCircles[i].SetActive(true);

                    tmpScreenPos.z = 0;
                    radarCircles[i].transform.position = tmpScreenPos;

                    tmpDistance = Vector3.Distance(player.transform.position, radarables[i].transform.position);
                    radarCircles[i].GetComponent<RadarCircle>().SetDistanceTmp(tmpDistance.ToString("F0"));
                }
                else
                {
                    // 화면 뒤에 있을 때
                    radarCircles[i].SetActive(false);
                }
            }
        }
        #endregion
    }
}