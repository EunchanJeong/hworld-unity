using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ChangeShopSceneWithCharacter : MonoBehaviour
{
    public GameObject character;
    public GameObject characterInstance;
    public static ChangeShopSceneWithCharacter Instance { get; private set; }

    // npc
    public GameObject MLBNpc;
    public GameObject MLBNpcBalloon;
    private bool isPlayerNearbyMLB = false;  
    public GameObject PradaNpc;
    public GameObject PradaNpcBalloon;
    private bool isPlayerNearbyPrada = false;  
    public GameObject JesNpc;
    public GameObject JesNpcBalloon;
    private bool isPlayerNearbyJes = false;  
    public GameObject CKNpc;
    public GameObject CKNpcBalloon;
    private bool isPlayerNearbyCK = false;  
    
    public float detectionRadius = 3.0f;  

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
        if (SceneManager.GetActiveScene().name != "MainScene") {
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            LoadCharacterPrefab();
            Debug.Log("씬 전환");
            saveCharacterPosition();
            StartCoroutine(TransitionAfterPrefabLoad("ShopScene"));
        }

        checkDistance(MLBNpc, MLBNpcBalloon, isPlayerNearbyMLB, 2);
        checkDistance(PradaNpc, PradaNpcBalloon, isPlayerNearbyPrada, 0);
        checkDistance(JesNpc, JesNpcBalloon, isPlayerNearbyJes, 1);
        checkDistance(CKNpc, CKNpcBalloon, isPlayerNearbyCK, 3);
    }

    private IEnumerator TransitionAfterPrefabLoad(string sceneName)
    {
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(sceneName);
    }

    void checkDistance(GameObject Npc, GameObject NpcBallon, bool isPlayerNearBy, int selectedShopIndex) {
        float distanceToPlayer = Vector3.Distance(character.transform.position, Npc.transform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            isPlayerNearBy = true;
            NpcBallon.SetActive(true);
        }
        else
        {
            isPlayerNearBy = false;
            NpcBallon.SetActive(false);
        }
        if (isPlayerNearBy && Input.GetKeyDown(KeyCode.I))
        {
            ShopManager.selectedShopIndex = selectedShopIndex;  // 상점 선택

            LoadCharacterPrefab();
            saveCharacterPosition();
            StartCoroutine(TransitionAfterPrefabLoad("ShopScene"));
        }
    }

    void LoadCharacterPrefab()
    {
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
            Debug.Log("ChangeShopSceneWithCharacter.Instance.characterInstance -> " + ChangeShopSceneWithCharacter.Instance.characterInstance);
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
