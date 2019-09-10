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

namespace WordCloud {


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
            TextMeshPro phraseText = child.GetComponent<TextMeshPro>();

            if (bc == null) {
                bc = child.gameObject.AddComponent<BoxCollider>(); // The same level down as the phrase itself
            }
            else {
                phraseText.enabled = false; // this is, perhaps, not the best way to fix a visual glitch where new words show up translucent
                phraseText.enabled = true;
            }
            Vector3 dimensions = phraseText.GetPreferredValues(phraseText.text, 800, Mathf.Infinity);
            bc.size = dimensions; //Make the hit box about the right size


            // Maybe check if there is a highlight point below??? Nah, put that in its own class
        }

        // Give a number of parameters. Break away stuff from Sphere() in FormWordCloud to do it.
        public void setupVoxPhrase(Phrase phrase) {
            TextMeshPro phraseText = gameObject.transform.Find("phrase_text").GetComponent<TextMeshPro>();

            phraseText.text = phrase.term.ToUpper();

            if (gameObject.name.EndsWith("*")) {
                //new_child = Instantiate(childObject, pos, Quaternion.identity) as GameObject;
                gameObject.transform.parent.name = phrase.term;
                phrase.asterisk = gameObject;
                phrase.obj = gameObject.transform.parent.gameObject;
            }
            else {
                // Ugh, workaround due to voxemeinit moving around what's the direct child.
                //new_child = Instantiate(childObject.transform.parent.gameObject, pos, Quaternion.identity) as GameObject;
                gameObject.name = phrase.term;
                phrase.obj = gameObject;
                // asterisk wouldn't be getting set yet?
                //GameObject asterisk = gameObject.transform.Find(gameObject.name + "*").gameObject;
                //if (asterisk != null) {

                //}
            }

            phrase.ideal_position = gameObject.transform.position; // Not happy, but we also don't want it moving
                                                                   //precious_children[child.name].size = phraseText.fontSize;
                                                                   //phrase.size = gameObject.transform.localScale; // We started setting this outside, I guess.
            
        }
    }
}