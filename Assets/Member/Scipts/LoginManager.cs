using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using dotenv.net;
using System;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    private string apiUrl;
    private string basicApiUrl; 
    public Button loginButton;

    public InputField IDInput;
    public InputField PWInput;

    private static string authToken;
    private static string refreshToken;

    void Start()
    {
        // // .env 파일 로드
        // DotEnv.Load();
        
        // // 환경 변수 불러오기
        // apiUrl = Environment.GetEnvironmentVariable("UNITY_APP_API_URL");

        apiUrl = ServerConfig.hostUrl;
        basicApiUrl = apiUrl + "/members/login";

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;


        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClick);
        }
    }

    public void OnLoginButtonClick()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        string loginId = IDInput.text;
        string password = PWInput.text;

        // POST할 데이터 생성
        var postData = new
        {
            loginId = loginId,
            password = password
        };

        // JSON 데이터를 생성
        string jsonData = JsonConvert.SerializeObject(postData);
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(basicApiUrl, "POST"))
        {
            PlayerPrefs.DeleteKey("authToken");
            PlayerPrefs.DeleteKey("refreshToken");

            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending request...");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("로그인 실패: " + request.error + " Response: " + request.downloadHandler.text);
            }
            else
            {
                authToken = request.GetResponseHeader("auth");
                refreshToken = request.GetResponseHeader("refresh");

                Debug.Log("authToken -> " + authToken);
                Debug.Log("refreshToken -> " + refreshToken);

                if (!string.IsNullOrEmpty(authToken))
                {
                    PlayerPrefs.SetString("authToken", authToken);
                    PlayerPrefs.SetString("refreshToken", refreshToken);

                    Debug.Log("로그인 성공");
                    Debug.Log("authToken -> " + authToken);
                    Debug.Log("refreshToken -> " + refreshToken);

                    SceneManager.LoadScene("MainScene");
                }
                else
                {
                    Debug.LogError("토큰이 응답 헤더에 없습니다.");
                }
            }
        }
    }

}
