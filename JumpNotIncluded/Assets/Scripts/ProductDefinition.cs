using UnityEngine;

namespace JumpNotIncluded
{
    [CreateAssetMenu(menuName="Jump Not Included/Product")]
    public class ProductDefinition : ScriptableObject
    {
        public Product product;
        public string title, subtitle;
        [TextArea] public string description;
        public int price;
    }
}
