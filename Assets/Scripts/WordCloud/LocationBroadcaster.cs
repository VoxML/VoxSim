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
                bi = transform.parent.GetComponentInChildren<BrowserInterface>(); // attached to Browser
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

            // Use this function to assign location to the green square
            void Move(Vector3 where) {
                // Set new location
                RectTransform rt = GetComponent<RectTransform>();
                BrowserInterface bi = GetComponent<BrowserInterface>();
                rt.position = where;
            }

            void SendSizeAndLocation() {
                Debug.LogWarning(rt.position + " " + bi + " " + rt.sizeDelta[0]);
                Vector3 to_enter = bi.RemapToWindow(rt.position);
                bi.ZoomIn(to_enter, rt.sizeDelta);
            }
        }
    }
}