using UnityEngine;

namespace JumpNotIncluded
{
    [CreateAssetMenu(menuName="Jump Not Included/FSM/Action")]
    public class ActionDefinition : ScriptableObject
    {
        public string sound;
        public void Apply(StateController controller)
        { if (!string.IsNullOrEmpty(sound)) controller.events?.Sound(sound); }
    }
}
