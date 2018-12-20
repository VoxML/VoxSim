using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;

using Episteme;
using Global;
using Network;

public class ReferringExpressionGenerator : MonoBehaviour {

    GameObject behaviorController;
	Predicates preds;
	ObjectSelector objSelector;
    EventManager eventManager;
    GameObject focusObj;

    Animator spriteAnimator;
    Timer focusTimeoutTimer;
    Timer referWaitTimer;

    bool timeoutFocus,refer;

    public int focusTimeoutTime;
    public int referWaitTime;

    public Image focusCircle;

    public JointGestureDemo world;

    public event EventHandler ObjectSelected;

    public void OnObjectSelected(object sender, EventArgs e)
    {
        if (ObjectSelected != null)
        {
            ObjectSelected(this, e);
        }
    }

	// Use this for initialization
	void Start () {

        focusCircle.enabled = false;
        spriteAnimator = focusCircle.GetComponent<Animator>();
        spriteAnimator.enabled = false;

        focusTimeoutTimer = new Timer();
        focusTimeoutTimer.Interval = focusTimeoutTime;
        focusTimeoutTimer.Enabled = false;
        focusTimeoutTimer.Elapsed += TimeoutFocus;
        timeoutFocus = false;

        referWaitTimer = new Timer();
        referWaitTimer.Interval = referWaitTime;
        referWaitTimer.Enabled = false;
        referWaitTimer.Elapsed += ReferToFocusedObject;
        timeoutFocus = false;

        behaviorController = GameObject.Find("BehaviorController");
		objSelector = GameObject.Find ("VoxWorld").GetComponent<ObjectSelector> ();
        preds = behaviorController.GetComponent<Predicates>();
        eventManager = behaviorController.GetComponent<EventManager>();

        ObjectSelected += IndicateFocus;
	}

	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // Casts the ray and get the first game object hit
            Physics.Raycast(ray, out hit);

            if (hit.collider != null) {
                if (world.blocks.Contains(Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject))) {
                    OnObjectSelected(this, new SelectionEventArgs(Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject)));
                }
            }
        }

        if (timeoutFocus) {
            timeoutFocus = false;
            focusCircle.enabled = false;
            spriteAnimator.enabled = false;
        }
		
	}

    void IndicateFocus(object sender, EventArgs e) {
        focusObj = ((SelectionEventArgs)e).Content as GameObject;
        Debug.Log(string.Format("Focused on {0}, world @ {1} screen @ {2}", focusObj.name,
            Helper.VectorToParsable(focusObj.transform.position),
            Helper.VectorToParsable(Camera.main.WorldToScreenPoint(focusObj.transform.position))));
        
        focusCircle.enabled = true;
        focusCircle.transform.position = new Vector3(focusObj.transform.position.x,
                                                     Helper.GetObjectWorldSize(focusObj).max.y,
                                                     focusObj.transform.position.z);
        focusTimeoutTimer.Interval = focusTimeoutTime;
        focusTimeoutTimer.Enabled = true;
        spriteAnimator.enabled = true;
        //spriteRenderer.transform.position = focusObj.transform.position;
        //spriteRenderer.transform.LookAt(Camera.main.transform.position, -Vector3.up);
        spriteAnimator.Play("circle_anim_test",0,0);

        //spriteRenderer.transform.position = focusObj.transform.position;
        //spriteRenderer.transform.LookAt(Camera.main.transform.position, -Vector3.up);
    }

    void TimeoutFocus(object sender, ElapsedEventArgs e) {
        focusTimeoutTimer.Enabled = false;
        timeoutFocus = true;
    }

    void ReferToFocusedObject(object sender, ElapsedEventArgs e) {
        referWaitTimer.Enabled = false;
        refer = true;

        Debug.Log(string.Format("Referring to {0}", focusObj.name));

    }
}
