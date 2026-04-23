using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Automatically connects to Photon servers and joins/creates a room, then loads the gameplay scene.
/// Attach this to a GameObject in your launcher scene.
/// </summary>
public class AutoConnectPUN : MonoBehaviourPunCallbacks
{
    [Header("Scene Settings")]
    [Tooltip("Name of the gameplay scene to load after connecting")]
    public string gameplaySceneName = "GameScene";

    [Header("Room Settings")]
    [Tooltip("Maximum players allowed in a room")]
    public int maxPlayersPerRoom = 4;

    [Tooltip("Room name to join/create")]
    public string roomName = "BreachRoom";

    [Header("Debug")]
    public bool showDebugLogs = true;

    void Start()
    {
        Log("Connecting to Photon servers...");

        // Make sure we're using the settings from PhotonServerSettings
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Log("Connected to Master Server. Joining/Creating room...");

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)maxPlayersPerRoom;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        // Try to join the room, if it doesn't exist it will be created
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}. Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
        Log($"Loading gameplay scene: {gameplaySceneName}");

        // Load the scene - PhotonNetwork.AutomaticallySyncScene will handle syncing
        // Or we manually load for everyone
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(gameplaySceneName);
        }
        else
        {
            // Client also loads the scene
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Log($"Disconnected from Photon: {cause}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Log($"Failed to create room: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Log($"Failed to join room: {message}");
    }

    void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[AutoConnectPUN] {message}");
        }
    }
}
