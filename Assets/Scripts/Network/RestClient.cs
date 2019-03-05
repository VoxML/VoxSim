using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Network
{
	public class RestEventArgs : EventArgs {

		public object Content { get; set; }

		public RestEventArgs(object content)
		{
			this.Content = content;
		}
	}

    public class RestClient : MonoBehaviour
    {
		public event EventHandler GotData;

		public void OnGotData(object sender, EventArgs e)
		{
			if (GotData != null)
			{
				GotData(this, e);
			}
		}

		public event EventHandler PostError;

		public void OnPostError(object sender, EventArgs e)
		{
			if (PostError != null)
			{
				PostError(this, e);
			}
		}

		public bool isConnected = false;

        public void Get(string url, string success, string error){
			Request(url, "GET", null, "GET_"+success, "GET_"+error);
        }
 
        public void Post(string url, string jsonPayload, string success, string error){
			Request(url, "POST", jsonPayload, "POST_"+success, "POST_"+error);
        }

        public void Put(string url, string jsonPayload, string success, string error){
			Request(url, "PUT", jsonPayload, "PUT_"+success, "PUT_"+error);
        }

        public void Delete(string url, string jsonPayload, string success, string error){
			Request(url, "DELETE", jsonPayload, "DELETE_"+success, "DELETE_"+error);
        }

		private void Request(string url, string method, string jsonPayload, string success, string error){
            StartCoroutine(AsyncRequest(jsonPayload, method, url, success, error));
        }

		private IEnumerator AsyncRequest(string jsonPayload, string method, string url, string success, string error){
            var webRequest = new UnityWebRequest(url, method);
            var payloadBytes = string.IsNullOrEmpty(jsonPayload)
                ? System.Text.Encoding.UTF8.GetBytes("{}")
                : System.Text.Encoding.UTF8.GetBytes(jsonPayload);

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
				//Debug.Log (webRequest.downloadHandler.text);
				gameObject.BroadcastMessage(success, webRequest.downloadHandler.text);
            }
        }

		void POST_okay(object parameter) {
			isConnected = true;
		}

		void POST_error(object parameter) {
			OnPostError (this, null);
		}

		void GET_okay(object parameter) {
			OnGotData (this, new RestEventArgs(parameter));
		}
    }
}