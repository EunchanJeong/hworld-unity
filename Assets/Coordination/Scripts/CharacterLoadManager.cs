using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CharacterLoadManager : MonoBehaviour
{
    private GameObject characterInstance;
    public Slider imageRotationSlider;

    public Camera cameraToCapture;
    public Rect captureArea = new Rect(0, 0, 1440, 900);

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        characterInstance = SaveCharacterData.Instance.characterInstance;
        Debug.Log("전달받은 characterInstance -> " + characterInstance);
        if (characterInstance != null)
        {
            Debug.Log("캐릭터 인스턴스를 찾았습니다.");
            
            // 캐릭터를 2D 환경에 맞게 설정
            SetupCharacterFor2D(characterInstance);

            GameObject canvasObject = GameObject.Find("Canvas");
            characterInstance.transform.SetParent(canvasObject.transform);
            characterInstance.transform.localPosition = new Vector3(90, -80, -530);
            characterInstance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            characterInstance.transform.localScale = new Vector3(100, 100, 100);
        }

        imageRotationSlider.minValue = 0;
        imageRotationSlider.maxValue = 360;
        imageRotationSlider.value = 180; // 기본값 설정
        imageRotationSlider.onValueChanged.AddListener(OnSliderValueChanged);

        if (cameraToCapture == null)
        {
            cameraToCapture = Camera.main; // 기본적으로 메인 카메라 사용
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            {
                StartCoroutine(CaptureScreen());
            }
    }

    private void OnSliderValueChanged(float value)
    {
        // Slider의 값에 따라 회전 조정
        if (characterInstance != null)
        {
            characterInstance.transform.localRotation = Quaternion.Euler(0, value, 0);
        }
    }

    private void SetupCharacterFor2D(GameObject character)
    {
        var rb = character.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 물리 기반 이동 비활성화
        }

        var colliders = character.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false; // 충돌체 비활성화
        }
    }

    private IEnumerator CaptureScreen()
    {
        yield return new WaitForEndOfFrame(); 
        
        // RenderTexture 설정
        RenderTexture rt = new RenderTexture((int)captureArea.width, (int)captureArea.height, 24);
        cameraToCapture.targetTexture = rt;
        cameraToCapture.Render();

        // RenderTexture에서 Texture2D로 변환
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D((int)captureArea.width, (int)captureArea.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, captureArea.width, captureArea.height), 0, 0);
        screenShot.Apply();

        cameraToCapture.targetTexture = null;
        RenderTexture.active = null; // 활성화된 렌더 텍스처를 해제
        Destroy(rt); // 사용이 끝난 후 메모리 해제

        // 이미지를 PNG 형식으로 저장
        byte[] bytes = screenShot.EncodeToPNG();
        string filePath = Path.Combine(Application.dataPath, "CapturedImage2.png");
        File.WriteAllBytes(filePath, bytes);

        Debug.Log("캡처된 이미지 저장 경로: " + filePath);
    }
}
