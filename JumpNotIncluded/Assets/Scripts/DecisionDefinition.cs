using UnityEngine;

namespace JumpNotIncluded
{
    [CreateAssetMenu(menuName="Jump Not Included/FSM/Decision")]
    public class DecisionDefinition : ScriptableObject
    {
        public string signal;
        public bool timed;
        public float seconds;
        public bool Evaluate(StateController controller)
            => timed ? controller.elapsed >= seconds : !string.IsNullOrEmpty(signal) && controller.pendingSignal == signal;
    }
}
