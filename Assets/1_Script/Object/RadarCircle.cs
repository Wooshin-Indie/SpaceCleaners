using TMPro;
using UnityEngine;

public class RadarCircle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI distanceTmp;

    public void SetDistanceTmp(string distance)
    {
        distanceTmp.text = distance;
    }
}
