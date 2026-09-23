namespace JumpNotIncluded
{
    public class PlayerFormController : StateController
    {
        private Form State=>current==null?Form.Small:(Form)current.key;
        public bool IsHurt=>State==Form.Hurt||State==Form.HurtSuper;
        // Hurt states retain the visible, playable size after losing exactly one level.
        public Form Value=>State==Form.Hurt?Form.Small:State==Form.HurtSuper?Form.Super:State;
    }
}
