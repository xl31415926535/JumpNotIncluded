using UnityEngine;
using UnityEngine.InputSystem;

namespace JumpNotIncluded
{
    public class InputRouter : MonoBehaviour
    {
        public InputActionAsset actions;
        public InputAction move,jump,fire,pause,navigate,submit;
        private InputActionMap gameplay,ui;
        public void Init()
        {
            actions=ScriptableObject.CreateInstance<InputActionAsset>();
            gameplay=new InputActionMap("Gameplay"); ui=new InputActionMap("UI");
            actions.AddActionMap(gameplay); actions.AddActionMap(ui);
            move=gameplay.AddAction("Move",InputActionType.Value);
            move.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/a").With("Positive","<Keyboard>/d");
            move.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/leftArrow").With("Positive","<Keyboard>/rightArrow");
            jump=gameplay.AddAction("Jump",InputActionType.Button,"<Keyboard>/space");
            jump.AddBinding("<Keyboard>/w");
            fire=gameplay.AddAction("Fire",InputActionType.Button,"<Keyboard>/j");
            pause=ui.AddAction("Pause",InputActionType.Button,"<Keyboard>/escape");
            submit=ui.AddAction("Submit",InputActionType.Button,"<Keyboard>/enter");
            navigate=ui.AddAction("Navigate",InputActionType.Value);
            navigate.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/upArrow").With("Positive","<Keyboard>/downArrow");
            ui.Enable(); gameplay.Enable();
        }
        public void SetPlaying(bool active)
        { if(active) gameplay.Enable(); else gameplay.Disable(); }
        private void OnDestroy() { actions?.Disable(); if(actions!=null) Destroy(actions); }
    }
}
