using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using TMPro;
using UnityEngine.SceneManagement;
using MyBox;
using PlayFab.Networking;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager instance; //Singleton
    public string BuildID;

    [Foldout("Player Details", true)]
    [SerializeField] private string EntityId, SessionTicket, EntityToken;
    string encryptedPassword;

    public static string PlayerUsername;

    [Foldout("Sign Up / Login", true)]
    public string Email, Password, Username;

    [Foldout("Player Stats")]
    [Range(0, 100)] public int PlayerXP;

    private Coroutine checkMatchmakingCoroutine;

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
    [ButtonMethod]
    public void StartMatchMaking()
    {
        var _PlayerXP = PlayerXP.ToString();
        var createMatchmakingTicketRequest = new CreateMatchmakingTicketRequest
        {
            Creator = new MatchmakingPlayer
            {
                Entity = new PlayFab.MultiplayerModels.EntityKey
                {
                    Id = EntityId,
                    Type = "title_player_account"
                },
                Attributes = new MatchmakingPlayerAttributes
                {
                    DataObject = new
                    {
                        Skill = _PlayerXP
                    }
                }
            },
            QueueName = "MatchMaking",
            GiveUpAfterSeconds = 300
        };
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(createMatchmakingTicketRequest, OnCreateMatchmakingTicketSuccess, PlayFabErrorLog);
    }
    void OnCreateMatchmakingTicketSuccess(CreateMatchmakingTicketResult result)
    {
        Debug.Log("Matchmaking ticket created. TicketId: " + result.TicketId);
        checkMatchmakingCoroutine = StartCoroutine(CheckMatchmakingTicketStatusEveryTenSeconds(result.TicketId));
    }
    IEnumerator CheckMatchmakingTicketStatusEveryTenSeconds(string ticketId)
    {
        while (true)
        {
            CheckMatchmakingTicketStatus(ticketId);
            yield return new WaitForSeconds(3);
        }
    }

    public void CheckMatchmakingTicketStatus(string ticketId)
    {
        var getMatchmakingTicketRequest = new GetMatchmakingTicketRequest
        {
            TicketId = ticketId,
            QueueName = "MatchMaking"
        };
        PlayFabMultiplayerAPI.GetMatchmakingTicket(getMatchmakingTicketRequest, OnGetMatchmakingTicketSuccess, PlayFabErrorLog);
        Debug.Log("Looking for a match");
    }

    void OnGetMatchmakingTicketSuccess(GetMatchmakingTicketResult result)
    {
        if (result.Status == "Matched")
        {
            Debug.Log("Match found. MatchId: " + result.MatchId);
            Debug.Log("Number of players in the match: " + result.Members.Count); // Log the number of players

            foreach (var member in result.Members)
            {
                //Debug.Log("Player Id: " + member.Entity.Id);
                //Debug.Log("Player Type: " + member.Entity.Type);

            }
            // Now you can enter the match
            EnterMatch(result.MatchId);

            if (checkMatchmakingCoroutine != null)
            {
                StopCoroutine(checkMatchmakingCoroutine);
                checkMatchmakingCoroutine = null;
            }
        }
    }



    public void EnterMatch(string SessionId)
    {
        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest
        {
            BuildId = BuildID,
            SessionId = SessionId,
            PreferredRegions = new List<string> { "NorthEurope" }
        };

        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, PlayFabErrorLog);
    }


    void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
    {
        if (response == null) return;

        UnityNetworkServer.Instance.networkAddress = response.IPV4Address;
        UnityNetworkServer.Instance.GetComponent<kcp2k.KcpTransport>().Port = (ushort)response.Ports[0].Num;

        UnityNetworkServer.Instance.StartClient();
    }

    #endregion

    #region Cleanup and Error Handling
    [ButtonMethod]
    public void Logout()
    {
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            // Remove the player from all queues
            var cancelRequest = new CancelAllMatchmakingTicketsForPlayerRequest
            {
                Entity = new PlayFab.MultiplayerModels.EntityKey
                {
                    Id = EntityId,
                    Type = "title_player_account"
                },
                QueueName = "MatchMaking"
            };
            PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(cancelRequest, OnCancelSuccess, PlayFabErrorLog);
        }
    }
    void OnApplicationQuit()
    {
        Logout();
    }

    void OnCancelSuccess(CancelAllMatchmakingTicketsForPlayerResult result)
    {
        Debug.Log("All matchmaking tickets for player cancelled successfully");
    }
    void PlayFabErrorLog(PlayFabError error)
    {
        UnityEngine.Debug.LogError(error);
    }

    #endregion

    #region Statistics
    [ButtonMethod]
    public void UpdatePlayerStats()
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
    {
        new StatisticUpdate { StatisticName = "Skill", Value = PlayerXP }
    }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticsUpdated, PlayFabErrorLog);

    }

    void OnStatisticsUpdated(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Player statistics updated successfully");
    }

    #endregion
}



