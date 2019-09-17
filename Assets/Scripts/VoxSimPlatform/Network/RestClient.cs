using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace VoxSimPlatform {
    namespace Network {
        public class RestDataContainer {
            // Coroutine in which this is instantiated
            public Coroutine coroutine { get; private set; }
            // Current item from target
            public object result;
            private IEnumerator target;
            public RestDataContainer(MonoBehaviour owner, IEnumerator target) {
                // e.g. client.TryConnect(address, port)
                this.target = target;
                this.coroutine = owner.StartCoroutine(Run());
            }
         
            private IEnumerator Run() {
                while (target.MoveNext()) {
                    result = target.Current;
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

            public event EventHandler GetOkay;

            public void OnGetOkay(object sender, EventArgs e) {
                if (GetOkay != null) {
                    GetOkay(this, e);
                }
            }

            public event EventHandler GetError;

            public void OnGetError(object sender, EventArgs e) {
                if (GetError != null) {
                    GetError(this, e);
                }
            }

            public event EventHandler PostOkay;

            public void OnPostOkay(object sender, EventArgs e) {
                if (PostOkay != null) {
                    PostOkay(this, e);
                }
            }

            public event EventHandler PostError;

            public void OnPostError(object sender, EventArgs e) {
                if (PostError != null) {
                    PostError(this, e);
                }
            }

            public event EventHandler PutOkay;

            public void OnPutOkay(object sender, EventArgs e) {
                if (PutOkay != null) {
                    PutOkay(this, e);
                }
            }

            public event EventHandler PutError;

            public void OnPutError(object sender, EventArgs e) {
                if (PutError != null) {
                    PutError(this, e);
                }
            }

            public event EventHandler DeleteOkay;

            public void OnDeleteOkay(object sender, EventArgs e) {
                if (DeleteOkay != null) {
                    DeleteOkay(this, e);
                }
            }

            public event EventHandler DeleteError;

            public void OnDeleteError(object sender, EventArgs e) {
                if (DeleteError != null) {
                    DeleteError(this, e);
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

            public UnityWebRequest webRequest;

            public IEnumerator TryConnect(string _address, int _port) {
                address = _address;
                port = _port;
                RestDataContainer result = new RestDataContainer(owner, Post("","0"));
                yield return result.result;
            }

            public void ConnectionLost(object sender, EventArgs e) {
                isConnected = false;
            }

            public virtual IEnumerator Get(string route) {
                Debug.Log(string.Format("RestClient GET from {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner, 
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "GET", null, "GET_" + successStr, "GET_" + errorStr));
                yield return result.result;
            }

            public virtual IEnumerator Post(string route, string jsonPayload) {
                Debug.Log(route);
                Debug.Log(jsonPayload);
                Debug.Log(string.Format("RestClient POST to {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner, 
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "POST", jsonPayload, "POST_" + successStr, "POST_" + errorStr));
                //Debug.Log(string.Format("RestClient.Post: {0}", result));
                yield return result.result;
            }

            public virtual IEnumerator Put(string route, string jsonPayload) {
                Debug.Log(string.Format("RestClient PUT to {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner, 
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "PUT", jsonPayload, "PUT_" + successStr, "PUT_" + errorStr));
                yield return result.result;
            }

            public virtual IEnumerator Delete(string route, string jsonPayload) {
                Debug.Log(string.Format("RestClient DELETE from {0}", string.Format("{0}:{1}/{2}", address, port, route)));
                RestDataContainer result = new RestDataContainer(owner, 
                    Request(string.Format("{0}:{1}/{2}", address, port, route), "DELETE", jsonPayload, "DELETE_" + successStr, "DELETE_" + errorStr));
                yield return result.result;
            }

            public virtual IEnumerator Request(string url, string method, string jsonPayload, string success, string error) {
                //StartCoroutine(AsyncRequest(jsonPayload, method, url, success, error));
                //IEnumerator r = AsyncRequest(jsonPayload, method, url, success, error);
                //yield return r;
                //Debug.Log((UnityWebRequest)r);
                //Debug.Log(r.GetType());
                //Debug.Log(r.Current);
                //Debug.Log(r.Current.GetType());
                //url = url.Replace(":0/", ""); // filler port from before. Might as well handle it here
                //Debug.LogWarning("URL for request: " + url);
                Debug.Log(string.Format("RestClient Request {1} to {0}", url, method));
                RestDataContainer result = new RestDataContainer(owner, AsyncRequest(jsonPayload, method, url, success, error));
                //Debug.Log(string.Format("RestClient.Request: {0}", result));
                yield return result.result;
            }

            public virtual IEnumerator AsyncRequest(string jsonPayload, string method, string url, string success, string error) {
                // In this method, we actually invoke a request to the outside server
                Debug.Log(string.Format("RestClient AsyncRequest {1} to {0}", url, method));
                webRequest = new UnityWebRequest(url, method);
                var payloadBytes = string.IsNullOrEmpty(jsonPayload)
                    ? Encoding.UTF8.GetBytes("{}")
                    : Encoding.UTF8.GetBytes(jsonPayload);

                UploadHandler upload = new UploadHandlerRaw(payloadBytes);
                webRequest.uploadHandler = upload;
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();    // 2017.2
                //yield return webRequest.Send();

                if (webRequest.isNetworkError) {
                    MethodInfo responseMethod = GetType().GetMethod(error);
                    responseMethod.Invoke(this, new object[] { webRequest.error });
                    //gameObject.BroadcastMessage(error, webRequest.error, SendMessageOptions.DontRequireReceiver);
                }
                else if (webRequest.responseCode < 200 || webRequest.responseCode >= 400) {
                    MethodInfo responseMethod = GetType().GetMethod(error);
                    responseMethod.Invoke(this, new object[] { webRequest.downloadHandler.text });
                    //gameObject.BroadcastMessage(error, webRequest.downloadHandler.text,
                    //    SendMessageOptions.DontRequireReceiver);
                }
                else {
                    MethodInfo responseMethod = GetType().GetMethod(success);
                    responseMethod.Invoke(this, new object[] { webRequest.downloadHandler.text });
                    //gameObject.BroadcastMessage(success, webRequest.downloadHandler.text);
                }
            }

            public void GET_okay(object parameter) {
                OnGetOkay(this, new RestEventArgs(parameter));
            }

            public void GET_error(object parameter) {
                OnGetError(this, new RestEventArgs(parameter));
            }

            public void POST_okay(object parameter) {
                isConnected = true;
                OnPostOkay(this, null);
            }

            public void POST_error(object parameter) {
                OnPostError(this, null);
            }

            public void PUT_okay(object parameter) {
                OnPutOkay(this, null);
            }

            public void PUT_error(object parameter) {
                OnPutError(this, null);
            }

            public void DELETE_okay(object parameter) {
                OnDeleteOkay(this, null);
            }

            public void DELETE_error(object parameter) {
                OnDeleteError(this, null);
            }
        }
    }
}