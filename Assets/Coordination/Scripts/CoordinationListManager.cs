using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;
using UnityEngine.SceneManagement;
using dotenv.net;

namespace Coordination {
    /// <summary>
    /// 코디 상품 리스트 출력 스크립트
    /// </summary>
    /// <author>김지현</author>
    /// <since>2024.09.09</since>
    /// <version>1.0</version>
    /// <remarks>
    /// 수정일: 2024.09.09, 수정자: 김지현, 최초 생성
    /// </remarks>
    public class CoordinationListResponseDTO
    {
        public int coordinationId {get; set;}
        public string title {get; set;}
        public string imageUrl {get; set;}
    }

    public class CoordinationListManager : MonoBehaviour
    {
        public int currentCoordinationId;
        public event Action<int> OnCoordinationIdChanged;

        public GameObject coordinationTitle;
        public Transform contentParent;
        public GameObject coordinationDetailView;

        public Color selectedButtonColor = Color.black; // 선택된 버튼의 색상
        public Color defaultButtonColor;  // 기본 버튼 색상
        public Color selectedTextColor = Color.white; // 선택된 텍스트의 색상
        public Color defaultTextColor;  // 기본 텍스트 색상
        private Button selectedButton; // 현재 선택된 버튼을 저장할 변수

        private string apiUrl;

        private static string authToken;
        private static string refreshToken;
        // public ServerConfig serverConfig;

        private void Start()
        {
            // .env 파일 로드
            // DotEnv.Load();
            

            // 환경 변수 불러오기
            // apiUrl = Environment.GetEnvironmentVariable("UNITY_APP_API_URL");
            apiUrl = ServerConfig.hostUrl;
            apiUrl += "/members/my-coordinations";

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // #2F3744 색상을 defaultButtonColor에 설정
            ColorUtility.TryParseHtmlString("#2F3744", out defaultButtonColor);

            // #2F3744 색상을 defaultTextColor에 설정
            ColorUtility.TryParseHtmlString("#AAAAAA", out defaultTextColor);

            // 코디 목록을 가져오는 메서드를 호출
            GetMemberCoordination();

            // 나가기 버튼에 MainScene으로 이동 리스너 추가
            GameObject iconExit = GameObject.Find("IconExit");
            iconExit.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene("MainScene"));
        }

        void Update()
        {
            // Esc 버튼이나 C 클릭 시 나가기
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.C))
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

        // 코디 목록을 가져오는 메서드
        public void GetMemberCoordination()
        {
            StartCoroutine(GetCoordinationListCoroutine());
        }

        // 서버에서 데이터를 가져오는 코루틴
        private IEnumerator GetCoordinationListCoroutine()
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
                    List<CoordinationListResponseDTO> coordinationList = JsonConvert.DeserializeObject<List<CoordinationListResponseDTO>>(jsonResponse);

                    foreach (var coordination in coordinationList)
                    {
                        // CoordinationTitle 프리팹을 Content 안에 생성
                        GameObject newCoordinationTitle = Instantiate(coordinationTitle, contentParent);

                        // CoordinationTitle의 자식 오브젝트에서 Text 컴포넌트를 찾아서 제목을 설정
                        Text titleText = newCoordinationTitle.transform.Find("CoordinationTitleText").GetComponent<Text>();
                        titleText.text = coordination.title;

                        Button button = newCoordinationTitle.GetComponent<Button>();

                        if (button != null)
                        {
                            // 선택 시 coordinationId와 함께 OnCoordinationTitleSelected 호출
                            button.onClick.AddListener(() => StartCoroutine(OnButtonClick(button, coordination)));
                        }
                    }
                }
            }
        }

        // 버튼 클릭 시 호출될 메서드
        private IEnumerator OnButtonClick(Button button, CoordinationListResponseDTO coordination)
        {
            // CoordinationItem의 자식 오브젝트에서 Image 컴포넌트를 찾아서 이미지 설정
            Image itemImage = coordinationDetailView.transform.Find("CoordinationImage")?.GetComponent<Image>();
            Debug.Log(itemImage);
            if (itemImage != null)
            {
                // 이미지 초기화
                itemImage.sprite = null;
                itemImage.color = Color.white; // 이미지 색상을 기본 색상으로 설정
                itemImage.preserveAspect = true; // 비율 유지

                // 새로운 이미지 로드
                Debug.Log(coordination.imageUrl);
                yield return StartCoroutine(LoadImageFromUrl(coordination.imageUrl, itemImage));
            }

            // 선택된 버튼 처리
            OnCoordinationTitleSelected(coordination.coordinationId, button);
        }

        // coordinationId에 따라 프리팹을 생성하는 메서드, 버튼 선택 관리
        private void OnCoordinationTitleSelected(int coordinationId, Button clickedButton)
        {
            Debug.Log("Selected Coordination ID: " + coordinationId);
            FindObjectOfType<CoordinationListManager>().currentCoordinationId = coordinationId;
            OnCoordinationIdChanged?.Invoke(coordinationId);

            // 이전에 선택된 버튼이 있다면 색을 원래대로 복원
            if (selectedButton != null)
            {
                // 이전에 선택된 버튼의 이미지 색상 복원
                var selectedButtonImage = selectedButton.GetComponent<Image>();
                if (selectedButtonImage != null)
                {
                    selectedButtonImage.color = defaultButtonColor; // 기본 색상으로 복원
                }

                // 이전에 선택된 버튼의 Text 색상 복원
                var selectedButtonText = selectedButton.GetComponentInChildren<Text>();
                if (selectedButtonText != null)
                {
                    selectedButtonText.color = defaultTextColor; // 기본 텍스트 색상으로 복원 
                }
            }

            // 현재 클릭된 버튼, 텍스트 설정
            selectedButton = clickedButton;

            // 클릭된 버튼의 이미지 색상 변경
            var clickedButtonImage = clickedButton.GetComponent<Image>();
            if (clickedButtonImage != null)
            {
                clickedButtonImage.color = selectedButtonColor; // 선택된 색상으로 변경
            }

            // 클릭된 버튼의 텍스트 색상 변경
            var clickedButtonText = clickedButton.GetComponentInChildren<Text>();
            if (clickedButtonText != null)
            {
                clickedButtonText.color = selectedTextColor; // 선택된 텍스트 색상으로 변경
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
    }
}
