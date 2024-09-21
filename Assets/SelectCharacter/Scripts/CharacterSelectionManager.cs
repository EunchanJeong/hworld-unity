using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement; // 씬 전환을 위한 네임스페이스 추가
using dotenv.net;
using System;

public class CharacterSelectionManager : MonoBehaviour
{

    // 캐릭터들이 배치될 위치 배열
    public Transform[] characterSpawnPoints;
    private GameObject[] currentCharacters = new GameObject[4]; // 현재 배치된 캐릭터들
    private GameObject selectedCharacter = null; // 선택된 캐릭터

    // UI 버튼들 (옷 종류 변경용)
    public Button casual1Button;
    public Button casual2Button;
    public Button casual3Button;
    public Button suitButton;
    public Button createCharacterButton; // 캐릭터 생성 버튼

    // 기본 버튼 색상과 선택된 버튼 색상
    public Color defaultButtonColor = Color.white;
    public Color selectedButtonColor = new Color(0.7f, 0.7f, 0.7f); // 선택된 버튼에 사용할 회색 색상

    // 캐릭터 FBX 파일들이 저장된 기본 경로
    private string baseCharacterPath = "Characters/";

    // 현재 선택된 카테고리(예: casual1, suit)
    private string currentCategory = "casual1";

    // 이전에 선택된 캐릭터의 텍스트 테두리 제거를 위해 저장할 변수
    private Text previousTextWithOutline;
    private Button previousSelectedButton; // 이전에 선택된 버튼

    private string AddCharacterApiUrl; // 캐릭터 생성 API URL

    // 팝업창 UI 요소들
    public GameObject popupPanel; // 팝업창 패널
    public Text popupMessage; // 팝업창에 표시할 메시지
    public Button popupConfirmButton; // 팝업창 확인 버튼

    private static string authToken;
    private static string refreshToken;

    void Start()
    {
        // 환경 변수에서 API URL 가져오기
        // DotEnv.Load();
        // string basicApiUrl = Environment.GetEnvironmentVariable("UNITY_APP_API_URL");

        string basicApiUrl = ServerConfig.hostUrl;
        AddCharacterApiUrl = basicApiUrl + "/characters";

        // 옷 변경 버튼에 클릭 이벤트 등록
        casual1Button.onClick.AddListener(() => OnCategoryButtonClicked("casual1", casual1Button));
        casual2Button.onClick.AddListener(() => OnCategoryButtonClicked("casual2", casual2Button));
        casual3Button.onClick.AddListener(() => OnCategoryButtonClicked("casual3", casual3Button));
        suitButton.onClick.AddListener(() => OnCategoryButtonClicked("suit", suitButton));

        // 캐릭터 생성 버튼 클릭 이벤트 등록
        createCharacterButton.onClick.AddListener(() => OnCreateCharacterButtonClicked());

        // 팝업창 확인 버튼 클릭 이벤트 등록
        popupConfirmButton.onClick.AddListener(OnConfirmButtonClicked);

        // 첫 번째 카테고리(casual1) 기본 로드 및 첫 번째 캐릭터 선택
        OnCategoryButtonClicked("casual1", casual1Button);
        SelectFirstCharacter();

        // 팝업창 기본적으로 비활성화
        popupPanel.SetActive(false);
    }

    void Update()
    {
        // 마우스 클릭 이벤트 처리
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast로 캐릭터 클릭 여부 확인
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedCharacter = hit.collider.gameObject;
                OnCharacterSelected(clickedCharacter); // 캐릭터 선택
            }
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

    // 카테고리(옷 종류) 버튼 클릭 시 호출
    public void OnCategoryButtonClicked(string category, Button selectedButton)
    {
        currentCategory = category; // 현재 카테고리 업데이트
        LoadCharactersForCategory(currentCategory); // 해당 카테고리의 캐릭터들 로드
        UpdateButtonColors(selectedButton); // 선택된 버튼의 색상 변경
    }

    // 선택된 버튼의 색상을 업데이트하는 함수
    private void UpdateButtonColors(Button selectedButton)
    {
        // 이전에 선택된 버튼이 있으면 색상을 기본 색상으로 복원
        if (previousSelectedButton != null)
        {
            previousSelectedButton.GetComponent<Image>().color = defaultButtonColor;
        }

        // 현재 선택된 버튼의 색상을 회색으로 변경
        selectedButton.GetComponent<Image>().color = selectedButtonColor;
        previousSelectedButton = selectedButton; // 현재 선택된 버튼 저장
    }

    // 현재 카테고리에 해당하는 캐릭터들을 로드하는 함수
    private void LoadCharactersForCategory(string category)
    {
        int selectedCharacterIndex = -1; // 현재 선택된 캐릭터의 인덱스 저장

        // 현재 선택된 캐릭터가 있으면 인덱스를 저장해 둠
        if (selectedCharacter != null)
        {
            for (int i = 0; i < currentCharacters.Length; i++)
            {
                if (currentCharacters[i] == selectedCharacter)
                {
                    selectedCharacterIndex = i; // 현재 캐릭터의 인덱스 저장
                    break;
                }
            }
        }

        // 새로운 캐릭터들 로드 및 배치
        for (int i = 0; i < characterSpawnPoints.Length; i++)
        {
            string path = baseCharacterPath + GetCharacterNameByIndex(category, i);
            LoadCharacter(path, i);
        }

        // 저장된 캐릭터 인덱스를 이용해 다시 선택
        if (selectedCharacterIndex != -1 && currentCharacters[selectedCharacterIndex] != null)
        {
            OnCharacterSelected(currentCharacters[selectedCharacterIndex]); // 선택된 캐릭터 유지
        }
        else if (currentCharacters.Length > 0)
        {
            OnCharacterSelected(currentCharacters[0]); // 없으면 첫 번째 캐릭터 선택
        }
    }

    // 캐릭터 이름 생성 함수 (카테고리 및 인덱스를 기반으로 파일명 생성)
    private string GetCharacterNameByIndex(string category, int index)
    {
        switch (index)
        {
            case 0: return "male_normal_" + category;  // 남성 일반 체형
            case 1: return "male_fat_" + category;     // 남성 비만 체형
            case 2: return "female_normal_" + category; // 여성 일반 체형
            case 3: return "female_fat_" + category;   // 여성 비만 체형
            default: return "male_normal_" + category; // 기본값
        }
    }

    // 캐릭터 로드 및 배치 함수
    private void LoadCharacter(string path, int index)
    {
        // 현재 캐릭터가 있다면 제거
        if (currentCharacters[index] != null)
        {
            Destroy(currentCharacters[index]);
        }

        // 새로운 캐릭터 로드 및 배치
        GameObject characterPrefab = Resources.Load<GameObject>(path);
        if (characterPrefab != null)
        {
            currentCharacters[index] = Instantiate(characterPrefab, characterSpawnPoints[index].position, characterSpawnPoints[index].rotation);
            currentCharacters[index].transform.localScale = new Vector3(270, 270, 270); // 크기 조정
            currentCharacters[index].transform.localRotation = Quaternion.Euler(0, 180, 0); // 캐릭터 회전

            // 캐릭터에 Collider 추가
            if (!currentCharacters[index].GetComponent<Collider>())
            {
                currentCharacters[index].AddComponent<BoxCollider>(); // BoxCollider 추가
            }
        }
        else
        {
            Debug.LogError("Character not found at path: " + path);
        }
    }

    // 첫 번째 캐릭터 선택 함수
    private void SelectFirstCharacter()
    {
        if (currentCharacters.Length > 0 && currentCharacters[0] != null)
        {
            OnCharacterSelected(currentCharacters[0]); // 첫 번째 캐릭터 선택
        }
    }

    // 캐릭터 클릭 시 호출되는 함수 (Raycast를 통한 선택)
    private void OnCharacterSelected(GameObject clickedCharacter)
    {
        for (int i = 0; i < currentCharacters.Length; i++)
        {
            if (currentCharacters[i] == clickedCharacter)
            {
                // 이전 선택된 캐릭터의 텍스트 테두리 제거
                if (previousTextWithOutline != null)
                {
                    RemoveOutline(previousTextWithOutline);
                }

                // 선택된 캐릭터의 텍스트에 테두리 추가
                AddTextOutline(characterSpawnPoints[i]);

                selectedCharacter = clickedCharacter; // 선택된 캐릭터 업데이트
                Debug.Log("Selected character: " + clickedCharacter.name);

                break;
            }
        }
    }

    // 캐릭터 타입을 계산하는 함수
    private int CalculateCharacterType(string characterName)
    {
        // (Clone) 접미사가 있으면 제거
        if (characterName.Contains("(Clone)"))
        {
            characterName = characterName.Replace("(Clone)", "").Trim();
        }

        // 캐릭터 이름을 파싱하여 타입을 계산
        string[] parts = characterName.Split('_');
        int gender = parts[0] == "male" ? 1 : 2;
        int bodyType = parts[1] == "normal" ? 1 : 2;
        int outfit = 1; // 기본값 casual1
        switch (parts[2])
        {
            case "casual1": outfit = 1; break;
            case "casual2": outfit = 2; break;
            case "casual3": outfit = 3; break;
            case "suit": outfit = 4; break;
        }

        return gender * 100 + bodyType * 10 + outfit; // 계산된 캐릭터 타입 반환
    }

    // 캐릭터 생성 버튼 클릭 시 호출되는 함수 (API 호출)
    private void OnCreateCharacterButtonClicked()
    {
        if (selectedCharacter != null)
        {
            // 팝업창을 비활성화
            popupPanel.SetActive(false);

            string characterName = selectedCharacter.name;
            int characterType = CalculateCharacterType(characterName); // 캐릭터 타입 계산

            // StartCoroutine(PostCharacterType(characterType)); // API 호출

            ShowPopup("캐릭터가 생성되었습니다."); // 성공 시 팝업창 표시
        }
        else
        {
            Debug.LogWarning("캐릭터가 선택되지 않았습니다.");
        }
    }

    // 캐릭터 타입을 POST로 전송하는 코루틴
    IEnumerator PostCharacterType(int characterType)
    {
        Debug.Log("전송할 characterType: " + characterType);

        // 요청 데이터 준비
        string jsonData = "{\"characterType\":" + characterType + "}";

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(AddCharacterApiUrl, jsonData))
        {
            SetHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest(); // 요청 전송

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("POST 요청 실패: " + request.error);
            }
            else
            {
                Debug.Log("POST 요청 성공: " + request.downloadHandler.text);
                ShowPopup("캐릭터가 생성되었습니다."); // 성공 시 팝업창 표시
            }
        }
    }

    // 팝업창 표시 함수
    private void ShowPopup(string message)
    {
        popupMessage.text = message;
        popupPanel.SetActive(true); // 팝업창 활성화
    }

    // 팝업 확인 버튼 클릭 시 씬 전환 함수
    private void OnConfirmButtonClicked()
    {
        popupPanel.SetActive(false); // 팝업창 비활성화
        SceneManager.LoadScene("MainScene"); // 새로운 씬으로 전환
    }

    // 캐릭터의 텍스트에 테두리 추가 함수
    private void AddTextOutline(Transform spawnPoint)
    {
        Text textComponent = spawnPoint.GetComponentInChildren<Text>();

        if (textComponent != null)
        {
            if (previousTextWithOutline != null)
            {
                RemoveOutline(previousTextWithOutline); // 이전 테두리 제거
            }

            // 현재 텍스트에 테두리 추가
            Outline outline = textComponent.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 144, 0, 255); // 주황색
            outline.effectDistance = new Vector2(2, -2); // 테두리 두께 설정

            previousTextWithOutline = textComponent; // 현재 텍스트 저장
        }
    }

    // 텍스트에서 테두리 제거 함수
    private void RemoveOutline(Text text)
    {
        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            Destroy(outline); // 테두리 제거
        }
    }
}
