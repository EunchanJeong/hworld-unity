using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SaveCharacterData : MonoBehaviour
{
    private GameObject character;
    public GameObject characterInstance;
    public static SaveCharacterData Instance { get; private set; }

    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            LoadCharacterPrefab();
            Debug.Log("씬 전환");
            saveCharacterPosition();
            StartCoroutine(TransitionAfterPrefabLoad("CoordinationAddScene"));
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("씬 전환");
            saveCharacterPosition();
            StartCoroutine(TransitionAfterPrefabLoad("CoordinationListScene"));
        }
    }

    private IEnumerator TransitionAfterPrefabLoad(string sceneName)
    {
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(sceneName);
    }

    void LoadCharacterPrefab()
    {
        character = GameObject.FindGameObjectWithTag("Player");

        if (character != null)
        {
            // 캐릭터 프리팹 인스턴스화
            characterInstance = Instantiate(character);

            // 불필요한 컴포넌트 제거
            RemoveComponent<ThirdPersonController>(characterInstance);
            RemoveComponent<Animator>(characterInstance);
            RemoveComponent<CharacterController>(characterInstance);
            RemoveComponent<StarterAssetsInputs>(characterInstance); 
            RemoveComponent<PlayerInput>(characterInstance);

            DontDestroyOnLoad(characterInstance); // 씬 전환 시에도 삭제되지 않도록 설정

            Debug.Log("character instance -> " + characterInstance.ToString());
            Debug.Log("SaveCharacterData.Instance.characterInstance -> " + SaveCharacterData.Instance.characterInstance);
        }
        else
        {
            Debug.LogError("캐릭터 프리팹을 찾을 수 없습니다");
        }
    }

    private void RemoveComponent<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component != null)
        {
            Destroy(component);
        }
    }

    private void saveCharacterPosition()
    {
        // 캐릭터 위치 저장
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            PlayerPositionManager.SavePosition(player.transform.position);
        }
    }
}
