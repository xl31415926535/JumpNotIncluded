namespace JumpNotIncluded
{
    public class BuffStateController : StateController
    {
        public Buff Value => current == null ? Buff.None : (Buff)current.key;
        public float Remaining => UnityEngine.Mathf.Max(0, (Value == Buff.VIP ? 8 : 6)-elapsed);
    }
}
