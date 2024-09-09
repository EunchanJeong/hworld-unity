using UnityEngine;
using Cinemachine;

public class CameraDistanceController : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    private  Cinemachine3rdPersonFollow thirdPersonFollow;
    public float distance = 4f;
    private	float minDistance = 1.5f;	// 카메라와 target의 최소 거리
    private	float maxDistance = 10f;	// 카메라와 target의 최대 거리
    private	float wheelSpeed = 300f;	// 마우스 휠 스크롤 속도

    private void Start()
    {
        // CinemachineTransposer 가져오기
        thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    }

    private void Update()
    {
        distance -= Input.GetAxis("Mouse ScrollWheel") * wheelSpeed * Time.deltaTime;
		// 거리는 최소, 최대 거리를 설정해서 그 값을 벗어나지 않도록 한다
		distance = Mathf.Clamp(distance, minDistance, maxDistance);

        thirdPersonFollow.CameraDistance = distance;
    }
}
