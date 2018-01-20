﻿using System;
using UnityEngine;

using Global;
using RootMotion.FinalIK;

namespace Agent
{
	public static class InteractionHelper
	{
		public static GameObject GetCloserHand(GameObject agent, GameObject obj) {
			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK> ().references.leftHand.gameObject;
			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK> ().references.rightHand.gameObject;
			GameObject grasper;

			Bounds bounds = Helper.GetObjectWorldSize ((obj as GameObject));

			// which hand is closer?
			float leftToGoalDist = (leftGrasper.transform.position - bounds.ClosestPoint (leftGrasper.transform.position)).magnitude;
			float rightToGoalDist = (rightGrasper.transform.position - bounds.ClosestPoint (rightGrasper.transform.position)).magnitude;

			if (leftToGoalDist < rightToGoalDist) {
				grasper = leftGrasper;
			}
			else {
				grasper = rightGrasper;
			}

//			Debug.Log (grasper);
			return grasper;
		}

		public static void SetLeftHandTarget(GameObject agent, Transform target) {
			FullBodyBipedIK ik = agent.GetComponent<FullBodyBipedIK>();
			if (target != null) {
				ik.solver.GetEffector (FullBodyBipedEffector.LeftHand).target = target;
				ik.solver.GetEffector (FullBodyBipedEffector.LeftHand).positionWeight = 1.0f;
			}
			else {
				ik.solver.GetEffector (FullBodyBipedEffector.LeftHand).target = null;
				ik.solver.GetEffector (FullBodyBipedEffector.LeftHand).positionWeight = 0.0f;
				ik.solver.GetEffector (FullBodyBipedEffector.LeftHand).rotationWeight = 0.0f;
			}
		}

		public static void SetRightHandTarget(GameObject agent, Transform target) {
			FullBodyBipedIK ik = agent.GetComponent<FullBodyBipedIK> ();
			if (target != null) {
				ik.solver.GetEffector (FullBodyBipedEffector.RightHand).target = target;
				ik.solver.GetEffector (FullBodyBipedEffector.RightHand).positionWeight = 1.0f;
			}
			else {
				ik.solver.GetEffector (FullBodyBipedEffector.RightHand).target = null;
				ik.solver.GetEffector (FullBodyBipedEffector.RightHand).positionWeight = 0.0f;
				ik.solver.GetEffector (FullBodyBipedEffector.RightHand).rotationWeight = 0.0f;
			}
		}

		public static void SetHeadTarget(GameObject agent, Transform target) {
			LookAtIK ik = agent.GetComponent<LookAtIK> ();
			if (target != null) {
				ik.solver.target.position = target.position;
				ik.solver.IKPositionWeight = 1.0f;
			}
			else {
				ik.solver.IKPositionWeight = 0.0f;
			}
		}
	}
}

