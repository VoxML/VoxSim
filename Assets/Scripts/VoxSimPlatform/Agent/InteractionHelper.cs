using RootMotion.FinalIK;
using UnityEngine;
using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Agent {
    	public static class InteractionHelper {
    		public static GameObject GetCloserHand(GameObject agent, GameObject obj) {
    			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
    			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
    			GameObject grasper;

    			Bounds bounds = Helper.GetObjectWorldSize((obj as GameObject));

    			// which hand is closer?
    			float leftToGoalDist =
    				(leftGrasper.transform.position - bounds.ClosestPoint(leftGrasper.transform.position)).magnitude;
    			float rightToGoalDist =
    				(rightGrasper.transform.position - bounds.ClosestPoint(rightGrasper.transform.position)).magnitude;

    			if (leftToGoalDist < rightToGoalDist) {
    				grasper = leftGrasper;
    			}
    			else {
    				grasper = rightGrasper;
    			}

    //			Debug.Log (grasper);
    			return grasper;
    		}

    		public static GameObject GetCloserHand(GameObject agent, Vector3 coord) {
    			GameObject leftGrasper = agent.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
    			GameObject rightGrasper = agent.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
    			GameObject grasper;

    			// which hand is closer?
    			float leftToGoalDist = (leftGrasper.transform.position - coord).magnitude;
    			float rightToGoalDist = (rightGrasper.transform.position - coord).magnitude;

    			if (leftToGoalDist < rightToGoalDist) {
    				grasper = leftGrasper;
    			}
    			else {
    				grasper = rightGrasper;
    			}

    			//          Debug.Log (grasper);
    			return grasper;
    		}

    		public static void SetLeftHandTarget(GameObject agent, Transform target,
    			float positionWeight = 1.0f, float pullWeight = 1.0f) {
    			FullBodyBipedIK ik = agent.GetComponent<FullBodyBipedIK>();
    			if (target != null) {
    				ik.solver.GetEffector(FullBodyBipedEffector.LeftHand).target = target;
    				ik.solver.GetEffector(FullBodyBipedEffector.LeftHand).positionWeight = positionWeight;
    				ik.solver.GetChain(FullBodyBipedChain.LeftArm).pull = pullWeight;
    			}
    			else {
    				ik.solver.GetEffector(FullBodyBipedEffector.LeftHand).target = null;
    				ik.solver.GetEffector(FullBodyBipedEffector.LeftHand).positionWeight = 0.0f;
    				ik.solver.GetEffector(FullBodyBipedEffector.LeftHand).rotationWeight = 0.0f;
    				ik.solver.GetChain(FullBodyBipedChain.LeftArm).pull = 1.0f;
    			}
    		}

    		public static void SetRightHandTarget(GameObject agent, Transform target,
    			float positionWeight = 1.0f, float pullWeight = 1.0f) {
    			FullBodyBipedIK ik = agent.GetComponent<FullBodyBipedIK>();
    			if (target != null) {
    				ik.solver.GetEffector(FullBodyBipedEffector.RightHand).target = target;
    				ik.solver.GetEffector(FullBodyBipedEffector.RightHand).positionWeight = positionWeight;
    				ik.solver.GetChain(FullBodyBipedChain.RightArm).pull = pullWeight;
    			}
    			else {
    				ik.solver.GetEffector(FullBodyBipedEffector.RightHand).target = null;
    				ik.solver.GetEffector(FullBodyBipedEffector.RightHand).positionWeight = 0.0f;
    				ik.solver.GetEffector(FullBodyBipedEffector.RightHand).rotationWeight = 0.0f;
    				ik.solver.GetChain(FullBodyBipedChain.RightArm).pull = 1.0f;
    			}
    		}

    		public static void SetHeadTarget(GameObject agent, Transform target) {
    			LookAtIK ik = agent.GetComponent<LookAtIK>();
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
}