#if !UNITY_WEBGL
using UnityEngine;

using VoxSimPlatform.Network;

namespace VoxSimPlatform
{
	namespace Examples
	{
		namespace IOClients
		{
			public class NLUIOClient : MonoBehaviour
			{
				RESTClients.NLURESTClient _nluRestClient;

				/// <summary>
				/// The associated REST client
				/// </summary>
				public RESTClients.NLURESTClient nluRestClient
				{
					get { return _nluRestClient; }
					set { _nluRestClient = value; }
				}

				CommunicationsBridge commBridge;

				// Use this for initialization
				void Start()
				{
					commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
					//_nlurestclient = (EpistemicState)commBridge.FindRestClientByLabel("EpiSim");
					_nluRestClient = (RESTClients.NLURESTClient)commBridge.FindRESTClientByLabel("NLTK");
				}

				// Update is called once per frame
				void Update()
				{
					if (_nluRestClient != null)
					{
						string epiSimUrl = string.Format("{0}:{1}", _nluRestClient.address, _nluRestClient.port);
						if (_nluRestClient.isConnected)
						{
							if (commBridge.tryAgainRest.ContainsKey(_nluRestClient.name))
							{
								if (commBridge.tryAgainSockets[epiSimUrl] == typeof(RESTClients.NLURESTClient))
								{
									_nluRestClient = (RESTClients.NLURESTClient)commBridge.FindRESTClientByLabel("NLTK"); // Maybe wrong
																														  //Debug.Log(_fusionSocket.IsConnected());
								}
							}

							//string inputFromFusion = _fusionSocket.GetMessage();
							//if (inputFromFusion != "") {
							//    Debug.Log(inputFromFusion);
							//    Debug.Log(_fusionSocket.HowManyLeft() + " messages left.");
							//    _fusionSocket.OnFusionReceived(this, new FusionEventArgs(inputFromFusion));
							//}
						}
						else
						{
							//SocketConnection _retry = socketConnections.FirstOrDefault(s => s.GetType() == typeof(FusionSocket));
							//TryReconnectSocket(_fusionSocket.Address, _fusionSocket.Port, typeof(FusionSocket), ref _retry);
							//_fusionSocket.OnConnectionLost(this, null);
							if (!commBridge.tryAgainRest.ContainsKey(epiSimUrl))
							{
								commBridge.tryAgainRest.Add(epiSimUrl, _nluRestClient.GetType());
							}
						}
					}
				}

				public void Get(string route)
				{
					nluRestClient.Get(route);

					//if (result.result.webRequest.isNetworkError) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
					//}
					//else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
					//        SendMessageOptions.DontRequireReceiver);
					//}
					//else {
					//    //Debug.Log (webRequest.downloadHandler.text);
					//    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
					//}
				}

				public void Post(string route, string content)
				{
					nluRestClient.Post(route, content);

					//if (result.result.webRequest.isNetworkError) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
					//}
					//else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
					//        SendMessageOptions.DontRequireReceiver);
					//}
					//else {
					//    //Debug.Log (webRequest.downloadHandler.text);
					//    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
					//}
				}

				public void Put(string route, string content)
				{
					nluRestClient.Put(route, content);

					//if (result.result.webRequest.isNetworkError) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
					//}
					//else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
					//        SendMessageOptions.DontRequireReceiver);
					//}
					//else {
					//    //Debug.Log (webRequest.downloadHandler.text);
					//    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
					//}
				}

				public void Delete(string route, string content)
				{
					nluRestClient.Delete(route, content);

					//if (result.result.webRequest.isNetworkError) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
					//}
					//else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
					//    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
					//        SendMessageOptions.DontRequireReceiver);
					//}
					//else {
					//    //Debug.Log (webRequest.downloadHandler.text);
					//    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
					//}
				}
			}
		}
	}
} 
#endif