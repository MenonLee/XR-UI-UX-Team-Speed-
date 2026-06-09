using UnityEngine;
using TMPro;
using System.Linq;

public class RacePosition : MonoBehaviour
{
    public TextMeshProUGUI positionText;
    public Transform[] allCars;        // 인스펙터에서 모든 차량 등록
    public Transform playerCar;        // 플레이어 차량

    // 결승선 Transform (거리 계산 기준)
    public Transform finishLine;

    void Update()
    {
        int position = CalculatePosition();
        positionText.text = GetPositionText(position);
    }

    int CalculatePosition()
    {
        // 결승선까지 거리가 짧을수록 앞 순위
        float playerDist = Vector3.Distance(playerCar.position, finishLine.position);

        int position = 1;
        foreach (Transform car in allCars)
        {
            if (car == playerCar) continue;
            float dist = Vector3.Distance(car.position, finishLine.position);
            if (dist < playerDist) position++;
        }
        return position;
    }

    string GetPositionText(int pos)
    {
        return pos switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{pos}th"
        };
    }
}