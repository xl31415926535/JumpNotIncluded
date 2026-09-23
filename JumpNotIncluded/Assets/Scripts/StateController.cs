using UnityEngine;

namespace JumpNotIncluded
{
    public class StateController : MonoBehaviour
    {
        public StateDefinition current;
        public StateDefinition[] states;
        public GameEvents events;
        public float elapsed;
        [System.NonSerialized] public string pendingSignal;
        public void Set(int key, bool quiet=false)
        {
            foreach(var state in states)
                if(state.key==key)
                {
                    current=state; elapsed=0;
                    if(!quiet && current.enterActions!=null)
                        foreach(var action in current.enterActions) action?.Apply(this);
                    return;
                }
        }
        public void Signal(string signal)
        { pendingSignal=signal; Evaluate(); pendingSignal=null; }
        public void Tick(float delta) { elapsed+=delta; Evaluate(); }
        private void Evaluate()
        {
            if(current==null || current.transitions==null) return;
            foreach(var transition in current.transitions)
                if(transition.decision!=null && transition.decision.Evaluate(this))
                { Set(transition.target.key); return; }
        }
    }
}
