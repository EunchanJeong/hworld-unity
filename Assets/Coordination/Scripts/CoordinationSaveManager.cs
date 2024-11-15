using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using System.Globalization;
using UnityEngine.SceneManagement;
using dotenv.net;


[System.Serializable]
public class UploadResponse
{
    public bool success;
    public string message;
}

/// <summary>
/// 코디 저장 스크립트
/// </summary>
/// <author>김지현</author>
/// <since>2024.09.09</since>
/// <version>1.0</version>
/// <remarks>
/// 수정일: 2024.09.09, 수정자: 김지현, 최초 생성
/// </remarks>
public class CoordinationSaveManager : MonoBehaviour
{
    public class CharacterItemResponseDTO
    {
        public int categoryId {get; set;}
        public int itemOptionId {get; set;}
        public string itemOption {get; set;}
        public int itemId {get; set;}
        public string itemName {get; set;}
        public int itemPrice {get; set;}
        public string imageUrl {get; set;}
    }

    public Camera cameraToCapture;
    public Rect captureArea = new Rect(0, 0, 2880, 1800);

    public Color backgroundColor = new Color(0, 0, 0, 0); 

    public int instanceLayer;

    public GameObject item;
    public Transform contentParent;

    public Button saveCoordinationButton;
    public InputField titleInput;
    public List<int> itemOptionIdList;

    private string basicApiUrl;
    private string apiUrl; 

    private static string authToken;
    private static string refreshToken;

    public GameObject savePopup;
    public Transform popupParent;

    void Start()
    {
        // .env 파일 로드
        // DotEnv.Load();
        
        // 환경 변수 불러오기
        // basicApiUrl = Environment.GetEnvironmentVariable("UNITY_APP_API_URL");
        basicApiUrl = ServerConfig.hostUrl;
        apiUrl = basicApiUrl + "/characters/item";

        if (cameraToCapture == null)
        {
            cameraToCapture = Camera.main; // 기본적으로 메인 카메라 사용
        }

        GetCharacterItem();

        if (saveCoordinationButton != null)
        {
            saveCoordinationButton.onClick.AddListener(OnSaveCoordinationButtonClick);
        }

        // 나가기 버튼에 MainScene으로 이동 리스너 추가
        GameObject iconExit = GameObject.Find("IconExit");
        iconExit.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene("MainScene"));
    }

    void Update()
    {
        // titleInput이 포커스된 상태라면 다른 입력을 처리하지 않음
        if (titleInput != null && titleInput.isFocused)
        {
            return;
        }

        // Esc 버튼이나 V 클릭 시 나가기
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.V))
        {
            SceneManager.LoadScene("MainScene");
        }
    }

    // 공통 헤더 설정 메서드
    private static UnityWebRequest SetHeaders(UnityWebRequest request)
    {
        // PlayerPrefs에서 저장된 토큰 불러오기
        authToken = PlayerPrefs.GetString("authToken", null);
        refreshToken = PlayerPrefs.GetString("refreshToken", null);

        if (!string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(refreshToken))
        {
            request.SetRequestHeader("auth", authToken);
            request.SetRequestHeader("refresh", refreshToken);
        }
        return request;
    }

    private IEnumerator CaptureScreen(System.Action<Texture2D> callback)
    {
        yield return new WaitForEndOfFrame(); 

        Debug.Log("instance layer -> " + instanceLayer);

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

        // 이미지 크롭 (예: x=1700, y=300, width=600, height=1200)
        Rect cropRect = new Rect(1700, 300, 600, 1200);
        Texture2D croppedTexture = CropTexture(screenShot, cropRect);

        // 이미지를 PNG 형식으로 저장
        // byte[] bytes = croppedTexture.EncodeToPNG();
        // string filePath = Path.Combine(Application.dataPath, "CapturedCharacter_Cropped.png");
        // File.WriteAllBytes(filePath, bytes);

        // Debug.Log("크롭된 캐릭터 이미지 저장 경로: " + filePath);

        // 콜백을 사용해 croppedTexture 반환
        callback?.Invoke(croppedTexture);
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

    // 캐릭터가 현재 착용하고 있는 아이템 목록을 가져오는 메서드
    public void GetCharacterItem()
    {
        StartCoroutine(GetCharacterItemCoroutine());
    }

    // 서버에서 데이터를 가져오는 코루틴
    private IEnumerator GetCharacterItemCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            SetHeaders(request);

            // 요청을 보내고 기다림
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                // 서버로부터 받은 JSON 데이터를 파싱
                string jsonResponse = request.downloadHandler.text;
                List<CharacterItemResponseDTO> charactetItemList = JsonConvert.DeserializeObject<List<CharacterItemResponseDTO>>(jsonResponse);

                foreach (var characterItem in charactetItemList)
                {
                    itemOptionIdList.Add(characterItem.itemOptionId);
                    Debug.Log(characterItem.itemOptionId);

                    // CoordinationTitle 프리팹을 Content 안에 생성
                    GameObject CharactetItem = Instantiate(item, contentParent);

                    // CoordinationTitle의 자식 오브젝트에서 Text 컴포넌트를 찾아서 이름 설정
                    Text itemTitle = CharactetItem.transform.Find("ItemName").GetComponent<Text>();
                    itemTitle.text = characterItem.itemName;

                    // CoordinationTitle의 자식 오브젝트에서 Text 컴포넌트를 찾아서 가격 설정
                    Text itemPrice = CharactetItem.transform.Find("ItemPrice").GetComponent<Text>();
                    string formattedPrice = characterItem.itemPrice.ToString("N0", CultureInfo.InvariantCulture);
                    itemPrice.text = formattedPrice + "원";

                    // CoordinationItem의 자식 오브젝트에서 Image 컴포넌트를 찾아서 이미지 설정
                    Image itemImage = CharactetItem.transform.Find("ItemImage").GetComponent<Image>();
                    if (itemImage != null)
                    {
                        yield return StartCoroutine(LoadImageFromUrl(characterItem.imageUrl, itemImage));
                    }
                }
            }
        }
    }

    private IEnumerator LoadImageFromUrl(string url, Image imageComponent)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("이미지 로드 오류: " + www.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = SpriteFromTexture(texture);

                imageComponent.sprite = sprite; 
            }
        }
    }

    private Sprite SpriteFromTexture(Texture2D texture)
    {
        Rect rect = new Rect(0, 0, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
    }

    private void OnSaveCoordinationButtonClick()
    {
        // StartCoroutine(CaptureScreen());

        // string title = titleInput.text; // 제목을 가져옵니다
        // string imageUrl = Application.dataPath + "CapturedCharacter_Cropped.png";
        // StartCoroutine(SendCoordinationData(title, imageUrl, itemOptionIdList));

        StartCoroutine(CaptureScreen((Texture2D croppedTexture) => 
        {
            string title = titleInput.text; // 제목을 가져옵니다
            StartCoroutine(SendCoordinationData(title, croppedTexture, itemOptionIdList));
        }));
    }

    private IEnumerator SendCoordinationData(string title, Texture2D imageToSend, List<int> itemOptionIdList)
    {
        string postCoordiApiUrl = basicApiUrl + "/coordinations"; 
        string postImageApiUrl = postCoordiApiUrl + "/image";


        // Texture2D를 PNG 형식의 byte[]로 변환
        byte[] imageData = imageToSend.EncodeToPNG();

        // FormData를 준비
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageData, "image.png", "image/png");

        using (UnityWebRequest request = UnityWebRequest.Post(postImageApiUrl, form))
        {
            SetHeaders(request);

            // 요청 전송
            yield return request.SendWebRequest();

            // 요청 결과 확인
            if (request.result == UnityWebRequest.Result.Success)
            {
                // JSON 응답을 파싱
                string jsonResponse = request.downloadHandler.text;
                UploadResponse response = JsonUtility.FromJson<UploadResponse>(jsonResponse);
                string uploadedUrl = response.message;


                // POST할 데이터 생성
                var postData = new
                {
                    title = title,
                    imageUrl = uploadedUrl,
                    itemOptionIdList = itemOptionIdList
                };

                // JSON으로 변환
                string jsonData = JsonConvert.SerializeObject(postData);
                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

                using (UnityWebRequest request2 = new UnityWebRequest(postCoordiApiUrl, "POST"))
                {
                    SetHeaders(request2);
                    request2.uploadHandler = new UploadHandlerRaw(jsonBytes);
                    request2.downloadHandler = new DownloadHandlerBuffer();
                    request2.SetRequestHeader("Content-Type", "application/json");

                    // 요청을 보내고 기다림
                    yield return request2.SendWebRequest();

                    if (request2.result == UnityWebRequest.Result.ConnectionError || request2.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError("Error: " + request2.error);
                    }
                    else
                    {
                        Debug.Log("성공적으로 데이터를 전송했습니다: " + request2.downloadHandler.text);

                        GameObject popup = Instantiate(savePopup, popupParent);
                        Button yesButton = popup.transform.Find("YesButton").GetComponent<Button>();
                        yesButton.onClick.AddListener(() => Destroy(popup));
                    }
                }

            }
            else
            {
                Debug.LogError($"Upload failed: {request.error}");
            }
        }
        
    }

}
