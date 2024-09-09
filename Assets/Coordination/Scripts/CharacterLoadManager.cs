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
    public Rect captureArea = new Rect(0, 0, 2880, 1800);

    public Color backgroundColor = new Color(0, 0, 0, 0); 

    public int instanceLayer;

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

            // Player 레이어의 값을 가져오기
            instanceLayer = characterInstance.layer;
            Debug.Log("캐릭터 인스턴스 레이어 -> " + instanceLayer);
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

        // 기존 카메라 백업
        Color originalBackgroundColor = cameraToCapture.backgroundColor;
        int originalCullingMask = cameraToCapture.cullingMask;

        // 배경을 투명하게 설정
        cameraToCapture.clearFlags = CameraClearFlags.SolidColor;
        cameraToCapture.backgroundColor = backgroundColor; // 투명한 배경

        cameraToCapture.cullingMask = 1 << instanceLayer;

        // RenderTexture 설정
        RenderTexture rt = new RenderTexture((int)captureArea.width, (int)captureArea.height, 24, RenderTextureFormat.ARGB32);
        cameraToCapture.targetTexture = rt;
        cameraToCapture.Render();

        // RenderTexture에서 Texture2D로 변환
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D((int)captureArea.width, (int)captureArea.height, TextureFormat.RGBA32, false);
        screenShot.ReadPixels(new Rect(0, 0, captureArea.width, captureArea.height), 0, 0);
        screenShot.Apply();

        cameraToCapture.targetTexture = null;
        RenderTexture.active = null; // 활성화된 렌더 텍스처를 해제
        Destroy(rt); // 사용이 끝난 후 메모리 해제

        // 기존 카메라 설정 복원
        cameraToCapture.backgroundColor = originalBackgroundColor;
        cameraToCapture.cullingMask = originalCullingMask;

        // 이미지 크롭 (예: x=900, y=150, width=450, height=600)
        Rect cropRect = new Rect(1700, 300, 600, 1200);
        Texture2D croppedTexture = CropTexture(screenShot, cropRect);

        // 이미지를 PNG 형식으로 저장
        byte[] bytes = croppedTexture.EncodeToPNG();
        string filePath = Path.Combine(Application.dataPath, "CapturedCharacter_Cropped.png");
        File.WriteAllBytes(filePath, bytes);

        Debug.Log("크롭된 캐릭터 이미지 저장 경로: " + filePath);
    }

    // Texture2D를 크롭하는 함수
    private Texture2D CropTexture(Texture2D original, Rect cropRect)
    {
        int x = Mathf.FloorToInt(cropRect.x);
        int y = Mathf.FloorToInt(cropRect.y);
        int width = Mathf.FloorToInt(cropRect.width);
        int height = Mathf.FloorToInt(cropRect.height);

        Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = original.GetPixels(x, y, width, height);
        cropped.SetPixels(pixels);
        cropped.Apply();

        return cropped;
    }
}
