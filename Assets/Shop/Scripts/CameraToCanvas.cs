using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraToCanvas : MonoBehaviour
{

    public Canvas canvas;
    public Camera camera;
    // Start is called before the first frame update
    void Start()
    {
        // 만약 캔버스가 World Space 모드로 설정되어 있다면 아래 작업을 실행
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            // 캔버스의 RectTransform 컴포넌트를 가져옴 (캔버스의 크기를 얻기 위해 필요)
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float canvasWidth = canvasRect.rect.width;  // 캔버스의 너비 저장
            float canvasHeight = canvasRect.rect.height; // 캔버스의 높이 저장

            // 카메라의 위치를 캔버스의 중심에 맞추고, Z축 값을 -290으로 설정
            // 이는 카메라가 적절한 거리에서 캔버스를 바라보도록 하기 위함
            camera.transform.position = new Vector3(canvas.transform.position.x, canvas.transform.position.y, -290f);

            // 카메라의 Orthographic(정사영) 크기를 캔버스 높이의 절반으로 설정
            // 이를 통해 캔버스가 카메라 화면에 잘 맞도록 조정
            camera.orthographicSize = canvasHeight / 2f;

            // 카메라의 화면 비율(Aspect Ratio)을 캔버스의 비율과 동일하게 맞춤
            // 이를 통해 캔버스가 화면에 왜곡 없이 정확히 보이도록 설정
            camera.aspect = canvasWidth / canvasHeight;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
