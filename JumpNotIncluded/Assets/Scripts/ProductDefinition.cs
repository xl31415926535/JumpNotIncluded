using UnityEngine;

namespace JumpNotIncluded
{
    public enum ProductCurrency { Coins, Wallet }
    [CreateAssetMenu(menuName="Jump Not Included/Product")]
    public class ProductDefinition : ScriptableObject
    {
        public Product product;
        public string title, subtitle;
        [TextArea] public string description;
        public int price;
        public ProductCurrency currency;
    }
}
