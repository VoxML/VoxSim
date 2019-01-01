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
using Agent;
using Network;


public class ReferringExpressionGenerator : MonoBehaviour {

    GameObject behaviorController;
	Predicates preds;
	ObjectSelector objSelector;
    EventManager eventManager;
    RelationTracker relationTracker;
    GameObject focusObj;

    Animator spriteAnimator;
    Timer focusTimeoutTimer;
    Timer referWaitTimer;

    bool itemsSituated;
    bool timeoutFocus,refer;

    public List<GameObject> landmarks;

    public int focusTimeoutTime;
    public int referWaitTime;

    public JointGestureDemo world;
    public GameObject agent;
    public Image focusCircle;

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

        relationTracker = behaviorController.GetComponent<RelationTracker>();

        eventManager.EntityReferenced += ReferenceObject;
                    
        ObjectSelected += IndicateFocus;
	}

	// Update is called once per frame
	void Update () {
        if (!itemsSituated) {
            for (int i = 0; i < landmarks.Count; i++) {
                landmarks[i] = Helper.GetMostImmediateParentVoxeme(landmarks[i]);
            }

            for (int i = 0; i < world.blocks.Count; i++) {
                world.blocks[i] = world.blocks[i] != Helper.GetMostImmediateParentVoxeme(world.blocks[i]) ?
                    Helper.GetMostImmediateParentVoxeme(world.blocks[i]) : world.blocks[i];
            }

            PlaceRandomly(world.demoSurface != Helper.GetMostImmediateParentVoxeme(world.demoSurface) ?
                          Helper.GetMostImmediateParentVoxeme(world.demoSurface) : world.demoSurface,
                          landmarks, world.blocks);
            itemsSituated = true;
        }

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
            referWaitTimer.Enabled = true;
        }

        if (refer) {
            refer = false;
            eventManager.OnEntityReferenced(this, new EventReferentArgs(focusObj));
        }		
	}

    void PlaceRandomly(GameObject surface, List<GameObject> landmarkObjs, List<GameObject> focusObjs) {
        // place landmarks
        foreach (GameObject landmark in landmarkObjs) {
            landmark.transform.position = Helper.FindClearRegion(surface, landmark).center;
            landmark.GetComponent<Voxeme>().targetPosition = landmark.transform.position;
        }

        // place focus objects
        foreach (GameObject obj in focusObjs) {
            obj.transform.position = Helper.FindClearRegion(surface, obj).center;
            obj.GetComponent<Voxeme>().targetPosition = obj.transform.position;
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
        //Debug.Log(Helper.VectorToParsable(focusCircle.transform.position));
        focusTimeoutTimer.Interval = focusTimeoutTime;
        focusTimeoutTimer.Enabled = true;
        spriteAnimator.enabled = true;
        spriteAnimator.Play("circle_anim_test",0,0);
    }

    void ReferenceObject(object sender, EventArgs e) {
        Debug.Log(string.Format("Referring to {0}", focusObj.name));

        if (world.interactionPrefs.gesturalReference) {
            GameObject hand = InteractionHelper.GetCloserHand(agent, focusObj);
            world.PointAt(focusObj.transform.position, hand);
        }
    }

    void TimeoutFocus(object sender, ElapsedEventArgs e) {
        focusTimeoutTimer.Enabled = false;
        timeoutFocus = true;
    }

    void ReferToFocusedObject(object sender, ElapsedEventArgs e) {
        referWaitTimer.Interval = referWaitTime;
        referWaitTimer.Enabled = false;
        refer = true;
    }
}
