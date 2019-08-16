using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using MajorAxes;
using RootMotion.FinalIK;
using VoxSimPlatform.Agent;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using TMPro;

namespace VoxSimPlatform {
    namespace Vox {


        //public class Phrase {
        //    // From tutorial. But the 'occurrences' should be remappable to whatever importance metric we wind up using.
        //    public string term;
        //    public float occurrences;

        //    //Items added to allow smooth transitions to new places/sizes
        //    public Vector3 size; // Font size
        //    public Vector3 ideal_position; // Where the phrase wants to be (will move there)
        //    public bool is_happy = false; // Whether is complacent, or will try to move to ideal location
        //    public GameObject obj; // Probably want an actual pointer to the object lol
        //}

        // New class, doe sthe direct manipulation of voxemes.
        public class Voxphrase : MonoBehaviour {

            private void Start() {
                
            }
            private void Update() {
                // Make sure the integrity of the voxphrase has withstood the evils of VoxemeInit
                Transform child = gameObject.transform.Find("phrase_text");
                BoxCollider bc = child.GetComponent<BoxCollider>();
                if (bc == null) {
                    TextMeshPro phraseText = child.GetComponent<TextMeshPro>();
                    bc = child.gameObject.AddComponent<BoxCollider>(); // The same level down as the phrase itself
                    Vector3 dimensions = phraseText.GetPreferredValues(phraseText.text, 800, Mathf.Infinity);
                    bc.size = dimensions; //Make the hit box about the right size
                }
            }

            // Give a number of parameters. Break away stuff from Sphere() in FormWordCloud to do it.
            public void setupVoxPhrase(Phrase phrase) {
                TextMeshPro phraseText = gameObject.transform.Find("phrase_text").GetComponent<TextMeshPro>();

                phraseText.text = phrase.term.ToUpper();

                gameObject.name = phrase.term;

                phrase.obj = gameObject;
                phrase.ideal_position = gameObject.transform.position; // Not happy, but we also don't want it moving
                //precious_children[child.name].size = phraseText.fontSize;
                phrase.size = gameObject.transform.localScale;
            }
        }
    }
}