using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// 씬 전환 시 캐릭터를 로드하기 위한 스크립트
/// </summary>
/// <author>김지현</author>
/// <since>2024.09.09</since>
/// <version>1.0</version>
/// <remarks>
/// 수정일: 2024.09.09, 수정자: 김지현, 최초 생성
/// </remarks>
public class CharacterLoadManager : MonoBehaviour
{
    private GameObject characterInstance;
    public Slider imageRotationSlider;

    public CoordinationSaveManager saveManager;

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
            int instanceLayer = characterInstance.layer;
            Debug.Log("캐릭터 인스턴스 레이어 -> " + instanceLayer);

            // CoordinationSaveManager에 레이어 값을 전달
            if (saveManager != null)
            {
                Debug.Log("saveManager 호출");
                saveManager.instanceLayer = instanceLayer;
            }
        }

        imageRotationSlider.minValue = 0;
        imageRotationSlider.maxValue = 360;
        imageRotationSlider.value = 180; // 기본값 설정
        imageRotationSlider.onValueChanged.AddListener(OnSliderValueChanged);
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

}
