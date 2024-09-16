#if UNITY_EDITOR // 이 코드 블록은 에디터에서만 실행되도록 설정
using UnityEditor; // AssetDatabase를 사용하기 위해 필요한 네임스페이스
#endif

using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    public Transform[] characterSpawnPoints; // 캐릭터가 배치될 위치들
    private GameObject[] currentCharacters = new GameObject[4]; // 화면에 있는 캐릭터들
    private GameObject selectedCharacter = null; // 선택된 캐릭터를 저장할 변수

    // 버튼들
    public Button casual1Button;
    public Button casual2Button;
    public Button casual3Button;
    public Button suitButton;

    // 캐릭터 카테고리 파일 경로 패턴
    private string baseCharacterPath = "Assets/SelectCharacter/Assets/Characters/";
    
    // 현재 배치된 캐릭터 경로들 (예: casual1, suit)
    private string currentCategory = "casual1"; 

    void Start()
    {
        // 버튼 클릭 이벤트 연결
        casual1Button.onClick.AddListener(() => OnCategoryButtonClicked("casual1"));
        casual2Button.onClick.AddListener(() => OnCategoryButtonClicked("casual2"));
        casual3Button.onClick.AddListener(() => OnCategoryButtonClicked("casual3"));
        suitButton.onClick.AddListener(() => OnCategoryButtonClicked("suit"));

        // 기본적으로 casual1 캐릭터들을 로드
        LoadCharactersForCategory(currentCategory);
    }

    void Update()
    {
        // 마우스 클릭 이벤트 처리
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // Raycast로 캐릭터 클릭 확인
            if (Physics.Raycast(ray, out hit))
            {
                // 클릭된 객체가 캐릭터인 경우
                GameObject clickedCharacter = hit.collider.gameObject;
                OnCharacterSelected(clickedCharacter);
            }
        }
    }

    // 버튼 클릭 시 해당 카테고리의 캐릭터 4개 로드
    public void OnCategoryButtonClicked(string category)
    {
        currentCategory = category;
        LoadCharactersForCategory(currentCategory);
    }

    // 캐릭터 로드 및 배치
    private void LoadCharactersForCategory(string category)
    {
        for (int i = 0; i < characterSpawnPoints.Length; i++)
        {
            string path = baseCharacterPath + GetCharacterNameByIndex(category, i);
            LoadCharacter(path, i);
        }
    }

    // 캐릭터 이름 생성
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
        path = path + ".fbx";
        GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (characterPrefab != null)
        {
            // 캐릭터 생성 및 배치
            GameObject instantiatedCharacter = Instantiate(characterPrefab, characterSpawnPoints[index].position, characterSpawnPoints[index].rotation);
            currentCharacters[index] = instantiatedCharacter;

            // 스케일을 270, 270, 270로 설정
            currentCharacters[index].transform.localScale = new Vector3(270, 270, 270);

            // Y축으로 180도 회전
            currentCharacters[index].transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Collider 추가 (클릭을 위한 Collider가 없으면 추가)
            if (!instantiatedCharacter.GetComponent<Collider>())
            {
                instantiatedCharacter.AddComponent<BoxCollider>(); // 기본적으로 BoxCollider 추가
            }
        }
        else
        {
            Debug.LogError("Character not found at path: " + path);
        }
    }

    // 캐릭터 클릭 시 호출되는 함수
    private void OnCharacterSelected(GameObject clickedCharacter)
    {
        // 클릭된 캐릭터를 콘솔에 출력
        Debug.Log("Character clicked: " + clickedCharacter.name);

        // 이전에 선택된 캐릭터의 효과 제거
        if (selectedCharacter != null)
        {
            RemoveOutlineEffect(selectedCharacter);
        }

        // 새로운 캐릭터 선택
        selectedCharacter = clickedCharacter;

        // 선택된 캐릭터에 효과 추가
        AddOutlineEffect(selectedCharacter);
    }

    // 선택된 캐릭터에 하이라이트 효과 추가 (여기서는 단순히 이름을 로그로 출력)
    private void AddOutlineEffect(GameObject character)
    {
        Debug.Log("Selected character: " + character.name);
    }

    // 선택 해제 시 하이라이트 효과 제거 (여기서는 단순히 이름을 로그로 출력)
    private void RemoveOutlineEffect(GameObject character)
    {
        Debug.Log("Deselected character: " + character.name);
    }
}
