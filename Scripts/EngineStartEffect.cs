using UnityEngine;
using System.Collections;

public class EngineStartEffect : MonoBehaviour
{
    [Header("시동 떨림 설정")]
    public float shakeDuration = 0.6f;     // 부릉! 하고 진동하는 시간 (사운드 길이에 맞춰 조절)
    public float positionIntensity = 0.015f; // 상하좌우 덜덜거리는 강도
    public float rotationIntensity = 0.8f;  // 엔진 토크로 인해 차체가 살짝 비틀리는 강도

    private Vector3 originalPos;
    private Quaternion originalRot;

    void Start()
    {
        // 차체의 원래 위치와 회전값을 기억해 둡니다.
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
    }

    // ⭐ 라이트가 켜지고 시동 사운드가 날 때 이 함수를 딱 한 번 호출해주세요!
    public void PlayEngineShake()
    {
        StopAllCoroutines(); // 혹시 실행 중인 진동이 있다면 멈춤
        StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        // 지정된 시간 동안 진동
        while (elapsed < shakeDuration)
        {
            // 시간이 지날수록 진동이 부드럽게 잦아들도록 감쇠(Damping) 계산
            float damping = 1.0f - (elapsed / shakeDuration); 

            // 상하좌우 미세한 떨림
            float offsetX = Random.Range(-1f, 1f) * positionIntensity * damping;
            float offsetY = Random.Range(-1f, 1f) * positionIntensity * damping;
            
            // 엔진 회전력에 의한 Z축(차체 옆면) 비틀림
            float offsetRotZ = Random.Range(-1f, 1f) * rotationIntensity * damping;

            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            transform.localRotation = originalRot * Quaternion.Euler(0, 0, offsetRotZ);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 진동이 끝나면 부드럽게 원래 자리로 완벽 복귀
        transform.localPosition = originalPos;
        transform.localRotation = originalRot;
    }
}