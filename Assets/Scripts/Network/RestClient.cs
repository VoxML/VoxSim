using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Network
{
    public class RestClient : MonoBehaviour
    {
        public void Get(string url, string success, string error){
            Request(url, "GET", null, success, error);
        }
 
        public void Post(string url, string jsonPayload, string success, string error){
            Request(url, "POST", jsonPayload, success, error);
        }

        public void Put(string url, string jsonPayload, string success, string error){
            Request(url, "PUT", jsonPayload, success, error);
        }

        public void Delete(string url, string jsonPayload, string success, string error){
            Request(url, "DELETE", jsonPayload, success, error);
        }

		private void Request(string url, string method, string jsonPayload, string success, string error){
            StartCoroutine(AsyncRequest(jsonPayload, method, url, success, error));
        }

        private IEnumerator AsyncRequest(string jsonPayload, string method, string url, string success, string error){
            var webRequest = new UnityWebRequest(url, method);
            var payloadBytes = string.IsNullOrEmpty(jsonPayload)
                ? System.Text.Encoding.UTF8.GetBytes("{}")
                : System.Text.Encoding.UTF8.GetBytes(jsonPayload);

			Debug.Log (method);

            UploadHandler upload = new UploadHandlerRaw(payloadBytes);
            webRequest.uploadHandler = upload;
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            //yield return webRequest.SendWebRequest();	// 2017.2
			yield return webRequest.Send();

            if(webRequest.isNetworkError) {
				gameObject.BroadcastMessage(error, webRequest.error, SendMessageOptions.DontRequireReceiver);
            }
            else if(webRequest.responseCode < 200 || webRequest.responseCode >= 400) {
				gameObject.BroadcastMessage(error, webRequest.downloadHandler.text, SendMessageOptions.DontRequireReceiver);
            } 
            else
            {
				Debug.Log (webRequest.downloadHandler.text);
                //gameObject.BroadcastMessage(success, null);
            }
        } 
    }
}