using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using dotenv.net;
using System;

using System;
using System.IO;

/// <summary>
/// 캐릭터 설정을 변경하기 위한 스크립트
/// </summary>
/// <author>조영욱</author>
/// <since>2024.09.09</since>
/// <version>1.0</version>
/// <remarks>
/// 수정일: 2024.09.09, 수정자: 조영욱, 최초 생성
/// </remarks>
public class PlayerSettingResponseDTO
{
    public int speed { get; set; }
    public int mouseSensitivity { get; set; }
    public int sound { get; set; }
    public int characterType { get; set; }
}

public class PlayerSettingDTO
{
    public int speed;
    public int sound;
    public int mouseSensitivity;
}

public class PlayerSettingManager : MonoBehaviour
{
    public ServerConfig serverConfig;
    public static int speed;
    public static int mouseSensitivity;
    public static int sound;
    public static bool hasSavedSetting = false;
    private static string playerSettingApiUrl;

    private static string authToken;
    private static string refreshToken;

    private void Start()
    {
        moveBar();
        
        // 나가기 버튼에 MainScene으로 이동 리스너 추가
        GameObject iconExit = GameObject.Find("IconExit");
        iconExit.GetComponent<Button>().onClick.AddListener(() => saveAndExit());

        // 커서 락 해제
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        // Esc 버튼 클릭 시 나가기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            saveAndExit();
        }
    }

    public static void GetPlayerSetting(MonoBehaviour caller, System.Action callback)
    {
        caller.StartCoroutine(GetPlayerSettingCoroutine(callback));
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
        } else {
            request.SetRequestHeader("auth", "");
            request.SetRequestHeader("refresh", "");
        }
        return request;
    }

    public static IEnumerator GetPlayerSettingCoroutine(System.Action callback)
    {
        if (playerSettingApiUrl == null) {
            string basicApiUrl = ServerConfig.hostUrl;

            playerSettingApiUrl = basicApiUrl + "/characters/state";
        }

        using (UnityWebRequest request = UnityWebRequest.Get(playerSettingApiUrl))
        {
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(playerSettingApiUrl);
                Debug.LogError("설정 가져오기 에러 : " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                PlayerSettingResponseDTO setting = JsonConvert.DeserializeObject<PlayerSettingResponseDTO>(jsonResponse);
                speed = setting.speed;
                sound = setting.sound;
                mouseSensitivity = setting.mouseSensitivity;

                Debug.Log($"Sound: {setting.sound}, mouse: {setting.mouseSensitivity}, speed: {setting.speed}");
                hasSavedSetting = true;

                 callback?.Invoke();
            }
        }
    }

    private void moveBar()
    {
        GameObject soundBox = GameObject.Find("SoundBox");
        GameObject mouseBox = GameObject.Find("MouseBox");
        GameObject speedBox = GameObject.Find("SpeedBox");
        soundBox.transform.Find("PercentText").GetComponent<Text>().text = $"{sound}%";
        mouseBox.transform.Find("PercentText").GetComponent<Text>().text = $"{mouseSensitivity}%";
        speedBox.transform.Find("PercentText").GetComponent<Text>().text = $"{speed}%";

        // Bar 오브젝트 위치 이동
        GameObject soundBar = soundBox.transform.Find("HorizonLine/Bar").gameObject;
        RectTransform soundHorizonLineRect = soundBox.transform.Find("HorizonLine").GetComponent<RectTransform>();
        RectTransform soundBarRect = soundBar.GetComponent<RectTransform>();

        GameObject mouseBar = mouseBox.transform.Find("HorizonLine/Bar").gameObject;
        RectTransform mouseBarRect = mouseBar.GetComponent<RectTransform>();

        GameObject speedBar = speedBox.transform.Find("HorizonLine/Bar").gameObject;
        RectTransform speedBarRect = speedBar.GetComponent<RectTransform>();

        // Bar 위치 설정 (0~100의 값으로 수평 이동)
        float maxPositionX = soundHorizonLineRect.rect.width;
        float soundNewPositionX = sound/100f*maxPositionX - maxPositionX/2;
        float mouseNewPositionX = mouseSensitivity/100f*maxPositionX - maxPositionX/2;
        float speedNewPositionX = speed/100f*maxPositionX - maxPositionX/2;

        soundBarRect.anchoredPosition = new Vector2(soundNewPositionX, soundBarRect.anchoredPosition.y);
        mouseBarRect.anchoredPosition = new Vector2(mouseNewPositionX, mouseBarRect.anchoredPosition.y);
        speedBarRect.anchoredPosition = new Vector2(speedNewPositionX, mouseBarRect.anchoredPosition.y);
    }

    // 설정 변동사항 적용 후 나가기
    private void saveAndExit()
    {
        StartCoroutine(saveSettingsCoroutine());
        SceneManager.LoadScene("MainScene");
    }

    private IEnumerator saveSettingsCoroutine()
    {
        PlayerSettingDTO dto = new PlayerSettingDTO();
        GameObject soundBox = GameObject.Find("SoundBox");
        GameObject mouseBox = GameObject.Find("MouseBox");
        GameObject speedBox = GameObject.Find("SpeedBox");
        float soundValue = float.Parse(soundBox.transform.Find("PercentText").GetComponent<Text>().text.Replace("%", ""));
        float mouseValue = float.Parse(mouseBox.transform.Find("PercentText").GetComponent<Text>().text.Replace("%", ""));
        float speedValue = float.Parse(speedBox.transform.Find("PercentText").GetComponent<Text>().text.Replace("%", ""));
        dto.sound = (int)soundValue;
        dto.mouseSensitivity = (int)mouseValue;
        dto.speed = (int)speedValue;

        sound = (int)soundValue;
        mouseSensitivity = (int)mouseValue;
        speed = (int)speedValue;

        string requestBody = JsonConvert.SerializeObject(dto);
        using (UnityWebRequest request = UnityWebRequest.Put(playerSettingApiUrl, requestBody))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("설정 저장 에러 : " + request.error);
            }
            else
            {
                Debug.Log("설정 저장 성공");
            }
        }
    }

    public static PlayerSettingDTO LoadSetting()
    {
        PlayerSettingDTO dto = new PlayerSettingDTO();
        dto.speed = speed;
        dto.mouseSensitivity = mouseSensitivity;
        dto.sound = sound;
        return dto;
    }
}
