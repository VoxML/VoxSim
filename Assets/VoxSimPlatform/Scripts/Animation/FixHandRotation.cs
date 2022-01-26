/* FixHandRotation.cs
 * USAGE: Attach component to InteractionObjects as needed. This script will automatically
 *        look for any child InteractionTargets that define hand poses. The interaction system
 *        and root joint need to be specified.
 *        
 *        This script will rotate the desired hand pose to point at the root joint (which should
 *        be a reference to the shoulder). This will prevent any contortion caused by impossible
 *        hand positioning.
 *        
 *        The hand direction is specified either as the local X-axis direction, or specified
 *        manually with localDirection. (For Diana, this needs to be overriden with the default
 *        localDirection.)
 *        
 *        This script operates on a single InteractionTarget. To support two hands, add this
 *        component twice on the InteractionObject, one script with references to the left hand
 *        and shoulder, and the other with references to the right hand and shoulder.
 */

using UnityEngine;

//using RootMotion.FinalIK;

namespace VoxSimPlatform {
    namespace Animation {
        public class FixHandRotation : MonoBehaviour { 
        	
        }
    }
}