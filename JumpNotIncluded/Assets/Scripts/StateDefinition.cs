using System;
using UnityEngine;

namespace JumpNotIncluded
{
    [Serializable]
    public class StateTransition
    { public DecisionDefinition decision; public StateDefinition target; }

    [CreateAssetMenu(menuName="Jump Not Included/FSM/State")]
    public class StateDefinition : ScriptableObject
    {
        public int key;
        public ActionDefinition[] enterActions;
        public StateTransition[] transitions;
    }
}
