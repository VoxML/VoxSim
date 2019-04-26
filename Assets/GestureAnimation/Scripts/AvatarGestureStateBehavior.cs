using UnityEngine;

/// <summary>
/// StateMachineBehavior -- Attach to the Idle (or other central animation) state of the Animator
/// </summary>
public class AvatarGestureStateBehavior : StateMachineBehaviour {
	public string layerName;

	public delegate void StateMachineEventHandler(object sender);

	public event StateMachineEventHandler AnimationStart;
	public event StateMachineEventHandler AnimationEnd;

	private bool isInitialState = true;

	// OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (isInitialState) {
			return; // Skip call the first time around
		}

		if (AnimationEnd != null) {
			AnimationEnd(this); // Entering idle state -- so animation just finished
		}
	}

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		isInitialState = false;

		if (AnimationStart != null) {
			AnimationStart(this); // Exiting idle state -- so animation is starting
		}
	}

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	}
}