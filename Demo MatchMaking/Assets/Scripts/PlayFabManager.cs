using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;
using MyBox;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager instance; //Singleton

    public static string EntityId, SessionTicket, EntityToken;
    string encryptedPassword;

    public static string PlayerUsername;

    [Foldout("Sign Up / Login",true)]
    public string Email,Password,Username;

    private void Awake()
    {
        if (instance == null) instance = this;
        DontDestroyOnLoad(this);
    }

    #region Signup and Login
    [ButtonMethod]
    public void SignUp()
    {
        var registerRequest = new RegisterPlayFabUserRequest { Email = Email, Password = Encrypt(Password), Username = Username, DisplayName = Username };
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, RegisterSuccess, PlayFabErrorLog);
        PlayerUsername = Username;
    }

    void RegisterSuccess(RegisterPlayFabUserResult result)
    {
        SessionTicket = result.SessionTicket;
        EntityId = result.EntityToken.Entity.Id;
        Debug.Log("Registered");

        //SceneManager.LoadSceneAsync("Gameplay");
    }

    [ButtonMethod]
    public void Login()
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = Email,
            Password = Encrypt(Password),
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, LoginSuccess, PlayFabErrorLog);
    }

    void LoginSuccess(LoginResult login)
    {
        SessionTicket = login.SessionTicket;
        EntityId = login.EntityToken.Entity.Id;
        EntityToken = login.EntityToken.EntityToken;
        Debug.Log("Logged In SuccessFully");
        PlayerUsername = login.InfoResultPayload.PlayerProfile.DisplayName;
       
        //SceneManager.LoadSceneAsync("Gameplay");
    }
    #endregion

    #region Helper Functions 
    string Encrypt(string StringToEncrypt)
    {
        System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] bs = System.Text.Encoding.UTF8.GetBytes(StringToEncrypt);

        bs = x.ComputeHash(bs);
        System.Text.StringBuilder s = new System.Text.StringBuilder();

        foreach (byte b in bs)
        {
            s.Append(b.ToString("x2").ToLower());
        }
        return s.ToString();
    }

    #endregion

    #region MatchMaking
    public void EnterMatch()
    {

    }
    #endregion
    void PlayFabErrorLog(PlayFabError error)
    {
        UnityEngine.Debug.LogError(error);
    }

}