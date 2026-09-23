using System;
using System.Collections.Generic;

namespace JumpNotIncluded
{
    public enum PaymentMethod { VirtualCard, Wallet }
    public enum CheckoutStep { Packs, LinkCard, Review, Processing, Receipt, Activity }

    [Serializable]
    public class TopUpReceipt
    {
        public string id, createdAt;
        public int pack, cents, coins, balanceAfter, fundsAfter;
        public PaymentMethod method;
    }

    // A local game simulation. No PAN, CVV, credentials, bank or payment network.
    [Serializable]
    public class PaymentAccount
    {
        public const int CreditLimit = 9900;
        public const string LastFour = "3141";
        public bool cardLinked;
        public int cardCharged;
        public List<TopUpReceipt> orders = new List<TopUpReceipt>();
        public int AvailableCredit => Math.Max(0, CreditLimit-cardCharged);
        public void LinkCard(){cardLinked=true;}
        public void UnlinkCard(){cardLinked=false;} // Re-linking cannot reset spent credit.
        public TopUpReceipt Find(string id)=>orders.Find(order=>order.id==id);
        public long Total(PaymentMethod method)
        {long total=0;foreach(var order in orders)if(order.method==method)total+=order.cents;return total;}
        public string Problem(RunModel run,int pack,PaymentMethod method)
        {
            int cost=RunModel.PackCost(pack),amount=RunModel.PackCoins(pack);
            if(cost<0||amount<=0)return "Choose a valid coin pack.";
            if(method!=PaymentMethod.VirtualCard&&method!=PaymentMethod.Wallet)return "Choose a payment method.";
            if(run.coins<0||run.coins>int.MaxValue-amount)return "Your coin balance cannot hold this pack.";
            if(method==PaymentMethod.VirtualCard)
            {
                if(!cardLinked)return "Link your virtual card to continue.";
                if(AvailableCredit<cost)return "Not enough virtual credit. Choose a smaller pack or pay from your SGD wallet.";
            }
            else if(run.wallet<cost)return "Your SGD wallet is short. Earn a reward or use your virtual card.";
            return null;
        }
        public bool Purchase(RunModel run,int pack,PaymentMethod method,string orderId,out string error)
        {
            if(string.IsNullOrWhiteSpace(orderId)||orderId.Length>64)
            {error="This payment session is invalid. Start a new checkout.";return false;}
            if(Find(orderId)!=null){error="This order has already been paid.";return false;}
            error=Problem(run,pack,method);if(error!=null)return false;
            int cost=RunModel.PackCost(pack),amount=RunModel.PackCoins(pack);
            if(method==PaymentMethod.VirtualCard)cardCharged+=cost;else run.wallet-=cost;
            run.coins+=amount;
            orders.Add(new TopUpReceipt{id=orderId,createdAt=DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                pack=pack,cents=cost,coins=amount,balanceAfter=run.coins,
                fundsAfter=method==PaymentMethod.VirtualCard?AvailableCredit:run.wallet,method=method});
            return true;
        }
    }
}
