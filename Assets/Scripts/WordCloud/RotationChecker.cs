using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// Copy/pasted from user BenZed on Unity forums
// https://forum.unity.com/threads/manually-calculate-angular-velocity-of-gameobject.289462/
// Doesn't really need to be very accurate, just checks if
// the cloud is currently rotating substantially,
// and lets FormWordCloud know by flipping a bool.

namespace WordCloud {
    public class RotationChecker : MonoBehaviour {

        //Holds the previous frames rotation
        Quaternion lastRotation;
        FormWordCloud fwc;

        //References to the relevent axis angle variables
        float magnitude;
        Vector3 axis;
        bool rotating_currently = false;

        public Vector3 angularVelocity {

            get {
                //DIVDED by Time.deltaTime to give you the degrees of rotation per axis per second
                return (axis * magnitude) / Time.deltaTime;
            }

        }

        void Start() {
            fwc = GetComponent<FormWordCloud>();
            lastRotation = transform.rotation;

        }

        void FixedUpdate() {

            //The fancy, relevent math
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
            deltaRotation.ToAngleAxis(out magnitude, out axis);

            lastRotation = transform.rotation;
            if (angularVelocity.magnitude > 5) {
                if (!rotating_currently){
                    Debug.LogWarning("Flag activating " + angularVelocity.magnitude);
                    fwc.rotating_flag = true;
                }
                rotating_currently = true;
                fwc.rotating = true;
            }
            else {
                if (rotating_currently) {
                    fwc.rotating_flag = true;
                }
                rotating_currently = false;
                fwc.rotating = false;
            }
        }

        // Flip whether we can or cannot rotate this cloud right now.
        public void toggleRotatable() {
            var frccs = GetComponent<DigitalRubyShared.FingersRotateCameraComponentScript>();
            frccs.enabled = !frccs.enabled;
        }

    }
}
