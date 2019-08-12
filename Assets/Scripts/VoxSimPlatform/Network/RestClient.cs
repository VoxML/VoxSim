using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

namespace VoxSimPlatform {
    namespace Network {
        public class RestDataContainer {
            public Coroutine coroutine { get; private set; }
            public object result;
            private IEnumerator target;
            public RestDataContainer(MonoBehaviour owner, IEnumerator target) {
                this.target = target;
                this.coroutine = owner.StartCoroutine(Run());
            }
         
            private IEnumerator Run() {
                while (target.MoveNext()) {
                    result = target.Current;
                    Debug.Log("result's type: "+result.GetType());
                    yield return result;
                }
            }
        }

        public class RestEventArgs : EventArgs {
            public object Content { get; set; }

            public RestEventArgs(object content) {
                this.Content = content;
            }
        }

        public class RestClient {
            public CommunicationsBridge owner;
            public Type clientType;

            public event EventHandler GotData;

            public void OnGotData(object sender, EventArgs e) {
                if (GotData != null) {
                    GotData(this, e);
                }
            }

            public event EventHandler PostError;

            public void OnPostError(object sender, EventArgs e) {
                if (PostError != null) {
                    PostError(this, e);
                }
            }

            public string name;
            public string address;
            public int port;
            public bool isConnected = false;

            string successStr = "okay";
            public string SuccessStr { 
                get { return successStr; }
            }

            string errorStr = "error";
            public string ErrorStr { 
                get { return errorStr; }
            }

            public IEnumerator TryConnect(string _address, int _port) {
                Debug.Log(string.Format("RestClient TryConnect to {0}", string.Format("{0}:{1}", address, port)));
                address = _address;
                port = _port;
                RestDataContainer result = new RestDataContainer(owner, Post("","0"));
                //Debug.Log(string.Format("RestClient.TryConnect: {0}", result));
                yield return result.result;
            }

            public void ConnectionLost(object sender, EventArgs e) {
                isConnected = false;
            }

            public IEnumerator Get(string route) {
                Debug.Log(string.Format("RestClient GET from {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner,
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "GET", null, "GET_" + successStr, "GET_" + errorStr));
                yield return result.coroutine;
            }

            public IEnumerator Post(string route, string jsonPayload) {
                Debug.Log(string.Format("RestClient POST to {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner,
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "POST", jsonPayload, "POST_" + successStr, "POST_" + errorStr));
                //Debug.Log(string.Format("RestClient.Post: {0}", result));
                yield return result.result;
            }

            public IEnumerator Put(string route, string jsonPayload) {
                Debug.Log(string.Format("RestClient PUT to {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner,
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "PUT", jsonPayload, "PUT_" + successStr, "PUT_" + errorStr));
                yield return result.coroutine;
            }

            public IEnumerator Delete(string route, string jsonPayload) {
                Debug.Log(string.Format("RestClient DELETE from {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner,
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "DELETE", jsonPayload, "DELETE_" + successStr, "DELETE_" + errorStr));
                yield return result.coroutine;
            }

            private IEnumerator Request(string url, string method, string jsonPayload, string success, string error) {
                //StartCoroutine(AsyncRequest(jsonPayload, method, url, success, error));
                //IEnumerator r = AsyncRequest(jsonPayload, method, url, success, error);
                //yield return r;
                //Debug.Log((UnityWebRequest)r);
                //Debug.Log(r.GetType());
                //Debug.Log(r.Current);
                //Debug.Log(r.Current.GetType());

                Debug.Log(string.Format("RestClient Request {1} to {0}", url, method));
                RestDataContainer result = new RestDataContainer(owner, AsyncRequest(jsonPayload, method, url, success, error));
                //Debug.Log(string.Format("RestClient.Request: {0}", result));
                yield return result.result;
            }

            private IEnumerator AsyncRequest(string jsonPayload, string method, string url, string success, string error) {
                Debug.Log(string.Format("RestClient AsyncRequest {1} to {0}", url, method));
                var webRequest = new UnityWebRequest(url, method);
                var payloadBytes = string.IsNullOrEmpty(jsonPayload)
                    ? Encoding.UTF8.GetBytes("{}")
                    : Encoding.UTF8.GetBytes(jsonPayload);

                UploadHandler upload = new UploadHandlerRaw(payloadBytes);
                webRequest.uploadHandler = upload;
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                //Debug.Log(string.Format("RestClient.AsyncRequest: {0}", webRequest));
                yield return webRequest.SendWebRequest();    // 2017.2
                //yield return webRequest.Send();

                //if (webRequest.isNetworkError) {
                //    gameObject.BroadcastMessage(error, webRequest.error, SendMessageOptions.DontRequireReceiver);
                //}
                //else if (webRequest.responseCode < 200 || webRequest.responseCode >= 400) {
                //    gameObject.BroadcastMessage(error, webRequest.downloadHandler.text,
                //        SendMessageOptions.DontRequireReceiver);
                //}
                //else {
                //    //Debug.Log (webRequest.downloadHandler.text);
                //    gameObject.BroadcastMessage(success, webRequest.downloadHandler.text);
                //}
            }

            void POST_okay(object parameter) { 
                isConnected = true;
            }

            void POST_error(object parameter) {
                OnPostError(this, null);
            }

            void GET_okay(object parameter) {
                OnGotData(this, new RestEventArgs(parameter));
            }
        }
    }
}