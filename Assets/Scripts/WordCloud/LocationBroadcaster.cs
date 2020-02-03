using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxSimPlatform {
    namespace Network {
        public class LocationBroadcaster : MonoBehaviour {
            // Start is called before the first frame update
            BrowserInterface bi;
            RectTransform rt;
            bool follow = false;
            int s = 10; // Scaling factor
            void Start() {
                rt = GetComponent<RectTransform>();
                bi = transform.parent.parent.GetComponentInChildren<BrowserInterface>(); // attached to Browser
                Debug.LogWarning(bi);
            }

            // Update is called once per frame
            void Update() {
                if (Input.GetKeyDown("l")) {
                    follow = !follow;
                }
                // Probably an easier way to summarize. But it's fine.
                if (Input.GetKeyDown(KeyCode.Equals)) { // because +
                    rt.sizeDelta += new Vector2(s, s);
                }
                if (Input.GetKeyDown(KeyCode.Minus)) {
                    rt.sizeDelta -= new Vector2(s, s);

                }
                if (Input.GetKeyDown(KeyCode.RightArrow)) {
                    rt.sizeDelta += new Vector2(s, 0);
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow)) { // Just horizontal
                    rt.sizeDelta -= new Vector2(s, 0);
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)) {
                    rt.sizeDelta += new Vector2(0, s);
                }
                if (Input.GetKeyDown(KeyCode.DownArrow)) {
                    rt.sizeDelta -= new Vector2(0, s);
                }
                if (follow && Input.mousePosition.y >= 0f
                            && Input.mousePosition.y <= Screen.height
                            && Input.mousePosition.x >= 0f
                            && Input.mousePosition.x <= Screen.width){
                    Move(Input.mousePosition);
                }
                if (Input.GetKeyDown("return")) {
                    SendSizeAndLocation();
                }
            }

            public void Grow() {
                rt.sizeDelta += new Vector2(s, s);
            }
            public void Shrink() {
                rt.sizeDelta -= new Vector2(s, s);
            }

            // Use this function to assign location to the green square
            void Move(Vector3 where) {
                // Set new location
                RectTransform rt = GetComponent<RectTransform>();
                //BrowserInterface bi = GetComponent<BrowserInterface>();
                rt.position = where;
            }

            public void SendSizeAndLocation() {
                // Render mode needs to change to Overlay to make the location mapping work
                // It needs to normally be in Camera mode for Fingers Drag/Drop to work
                transform.parent.parent.gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                if(bi == null) {
                    bi = transform.parent.parent.GetComponentInChildren<BrowserInterface>();
                }
                Debug.LogWarning(rt.position + " " + bi + " " + rt.sizeDelta[0]);
                Vector3 to_enter = bi.RemapToWindow(rt.position);
                bi.ZoomIn(to_enter, rt.sizeDelta);
                transform.parent.parent.gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
            }
        }
    }
}