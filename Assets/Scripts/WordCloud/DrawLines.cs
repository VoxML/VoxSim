using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordCloud {


    public class DrawLines : MonoBehaviour {

        [SerializeField]
        private GameObject lineGeneratorPrefab;

        [SerializeField]
        private GameObject linePointPrefab;

        // Start is called before the first frame update
        //void Start()
        //{
        //    SpawnLineGenerator();
        //}

        // Update is called once per frame
        void Update() {
            //if (Input.GetMouseButtonDown(0)) {
            //    Debug.Log("leftmouse down");
            //    Vector3 newPos = Camera.main.ScreenPointToRay(Input.mousePosition).direction;
            //    //Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //    //newPos.z = 0; // 3d may need more
            //    CreatePointMarker(newPos);
            //}

            //if (Input.GetMouseButtonDown(1)) {
            //    ClearAllPoints();
            //}

            // Very limited, only the top 6 points (so the hihlighted and 5 closest to it)
            if (Input.GetKeyDown("e")) {
                GenerateNewLine(6);
            }
        }

        private void CreatePointMarker(Vector3 pointPosition) {
            Instantiate(linePointPrefab, pointPosition, Quaternion.identity);
        }

        private void ClearAllPoints() {
            GameObject[] allPoints = GameObject.FindGameObjectsWithTag("PointMarker");
            //YEET
            foreach (GameObject p in allPoints) {
                Destroy(p);
            }
        }


        // Draw lines from the highlighted word (at position 0) to all the other ones.
        // Only makes sense with a distance calculation
        private void GenerateNewLine(int howmany) {
            List<Phrase> phrases = transform.GetComponent<FormWordCloud>().GetPhrases();
            //GameObject[] allPoints = from phrase in phrases select phrase.obj;
            //GameObject[] allPoints = GameObject.FindGameObjectsWithTag("PointMarker");

            Vector3[] allPointPositions = new Vector3[Math.Min(phrases.Count, howmany + 1)];
            //Debug.LogWarning("erqiuguergbiuegr" + phrases + " " + phrases.Count);
            if (phrases.Count > 1) {
                // Need at least 2 lol
                for (int i = 1; i < Math.Min(phrases.Count, howmany + 1); i++) {
                    allPointPositions[i] = phrases[i].obj.transform.position;
                    // Avoid drawing quiiiite to the center.
                    Vector3 start = (phrases[0].obj.transform.position * 1.8f + phrases[i].obj.transform.position * 0.2f) / 2;
                    Vector3 finish = (phrases[i].obj.transform.position * 1.8f + phrases[0].obj.transform.position * 0.2f) / 2;
                    SpawnLineGenerator(new Vector3[] { start, finish });
                }
                
            }
            else {
                Debug.Log("Less than 2 points in the line, whoops");
            }
        }

        private void SpawnLineGenerator(Vector3[] linePoints) {
            GameObject newLineGen = Instantiate(lineGeneratorPrefab);
            LineRenderer lRend = newLineGen.GetComponent<LineRenderer>();
            lRend.positionCount = linePoints.Length; // Important to set this to total number of points (probably 2 yo)

            lRend.SetPositions(linePoints);

            Destroy(newLineGen, 5); // Delete in 5 seconds
        }
    }
}