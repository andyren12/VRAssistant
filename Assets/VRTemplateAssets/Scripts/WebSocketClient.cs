using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // Uncomment if you're using TextMeshPro
using Newtonsoft.Json;
using MikeSchweitzer.WebSocket;

public class WebSocketClient : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "https://api.mypurecloud.com/api/v2/notifications/channels";
    [SerializeField] private string accessToken = "YOUR_ACCESS_TOKEN"; // Replace with your actual access token

    [Header("User Configuration")]
    [SerializeField] private string userId = "8e8e71ed-6570-4e4c-9d48-2aeea9f7c035";

    [Header("Topics")]
    [SerializeField]
    private List<string> topics = new List<string>
    {
        "v2.users.{userId}.activity"
    };

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI webSocketMessageText; // Uncomment if you're using TextMeshPro

    private WebSocketConnection webSocket;
    private string connectUri;
    private string channelId;

    void Start()
    {
        Debug.Log("Starting WebSocket Client...");
        StartCoroutine(CreateChannel());
    }

    IEnumerator CreateChannel()
    {
        Debug.Log("Creating channel...");
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        request.SetRequestHeader("Content-Type", "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error creating channel: " + request.error);
        }
        else
        {
            Debug.Log("Channel created successfully.");
            Debug.Log("Response: " + request.downloadHandler.text);
            var response = JsonConvert.DeserializeObject<ChannelResponse>(request.downloadHandler.text);
            connectUri = response.connectUri;
            channelId = response.id;
            ConnectWebSocket(connectUri);
        }
    }

    void ConnectWebSocket(string uri)
    {
        Debug.Log($"Connecting to WebSocket at {uri}...");
        GameObject webSocketObject = new GameObject("WebSocketConnection");
        webSocket = webSocketObject.AddComponent<WebSocketConnection>();

        webSocket.DesiredConfig = new WebSocketConfig
        {
            Url = uri
        };

        webSocket.StateChanged += OnStateChanged;
        webSocket.MessageReceived += OnMessageReceived;
        webSocket.ErrorMessageReceived += OnErrorMessageReceived;

        webSocket.Connect();
    }

    private void OnStateChanged(WebSocketConnection connection, WebSocketState oldState, WebSocketState newState)
    {
        Debug.Log($"WebSocket State Changed from {oldState} to {newState}");
        if (newState == WebSocketState.Connected)
        {
            Debug.Log("WebSocket connected. Subscribing to topics...");
            SubscribeToTopics(channelId);
        }
    }

    private void OnMessageReceived(WebSocketConnection connection, WebSocketMessage message)
    {
        Debug.Log($"Message received from server: {message.String}");

        // Deserialize the message JSON
        var messageData = JsonConvert.DeserializeObject<MessageData>(message.String);

        // Display the parsed data in the UI
        webSocketMessageText.text = $"User ID: {messageData.eventBody.id}\n" +
                                    $"Status: {messageData.eventBody.routingStatus.status}\n" +
                                    $"Start Time: {messageData.eventBody.routingStatus.startTime}\n" +
                                    $"Presence: {messageData.eventBody.presence.presenceDefinition.systemPresence}";
    }

    private void OnErrorMessageReceived(WebSocketConnection connection, string errorMessage)
    {
        Debug.LogError($"WebSocket error: {errorMessage}");
    }

    void SubscribeToTopics(string channelId)
    {
        Debug.Log("Subscribing to topics...");
        var subscriptionUrl = $"{apiUrl}/{channelId}/subscriptions";
        var topicList = new List<ChannelTopic>();

        foreach (var topic in topics)
        {
            topicList.Add(new ChannelTopic { id = topic.Replace("{userId}", userId) });
        }

        var json = JsonConvert.SerializeObject(topicList);
        StartCoroutine(PostRequest(subscriptionUrl, json));
    }

    IEnumerator PostRequest(string uri, string json)
    {
        Debug.Log($"Posting request to {uri} with JSON: {json}");
        var request = new UnityWebRequest(uri, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error subscribing to topics: " + request.error);
        }
        else
        {
            Debug.Log("Successfully subscribed to topics.");
        }
    }

    [Serializable]
    public class ChannelResponse
    {
        public string id;
        public string connectUri;
    }

    [Serializable]
    public class ChannelTopic
    {
        public string id;
    }

    [Serializable]
    public class MessageData
    {
        public string topicName;
        public string version;
        public EventBody eventBody;
    }

    [Serializable]
    public class EventBody
    {
        public string id;
        public RoutingStatus routingStatus;
        public Presence presence;
    }

    [Serializable]
    public class RoutingStatus
    {
        public string status;
        public string startTime;
    }

    [Serializable]
    public class Presence
    {
        public PresenceDefinition presenceDefinition;
    }

    [Serializable]
    public class PresenceDefinition
    {
        public string id;
        public string systemPresence;
    }
}
