using System;
using System.Collections.Generic;

namespace JumpNotIncluded
{
    public enum Product { Jump = 0, FireFlower = 1, Purify = 2, MasterGuide = 3, DoubleJump = 4, Gatling = 5 }
    public enum Form { Small, Super, Fire, Hurt, Dead, HurtSuper }
    public enum Buff { None, Star, VIP }
    public enum ScreenMode { Playing, Shop, Ad, Dead, Pause, Results, Menu, Loading }
    public enum ItemKind { Coin, Mushroom, Poison, Flower, Star }
    public enum AdKind { Revive, Cash }
    public enum AdCampaign { Mario, SutdAI, SlCheater }

    // A shuffled set keeps all three creatives in rotation without adjacent repeats.
    public sealed class AdRotation
    {
        private readonly Random random;
        private readonly AdCampaign[] order={AdCampaign.Mario,AdCampaign.SutdAI,AdCampaign.SlCheater};
        private int next=3,last=-1;
        public AdRotation(Random random=null){this.random=random??new Random();}
        public AdCampaign Next()
        {
            if(next==order.Length)
            {
                for(int i=order.Length-1;i>0;i--)
                {int j=random.Next(i+1);var swap=order[i];order[i]=order[j];order[j]=swap;}
                if((int)order[0]==last)
                {int j=random.Next(1,order.Length);var swap=order[0];order[0]=order[j];order[j]=swap;}
                next=0;
            }
            var result=order[next++];last=(int)result;return result;
        }
    }

    [Serializable]
    public class Receipt
    {
        public Product product;
        public int coins;
        public bool refunded;
    }

    // Engine-independent rules: used by the game, editor checks and offline tests.
    [Serializable]
    public class RunModel
    {
        public const int WalletLimit=9900;
        public string runId = Guid.NewGuid().ToString("N");
        public int world = 1, score, coins, wallet, deaths, ads, refunds;
        public float playTime, adWatchTime, activeInputTime;
        public bool trialClaimed, trialPending, refundBonus;
        public List<Product> owned = new List<Product>();
        public List<string> claimed = new List<string>();
        public List<Receipt> receipts = new List<Receipt>();
        public List<string> collected = new List<string>();
        public PaymentAccount payments = new PaymentAccount();
        public PaymentAccount Payments => payments??(payments=new PaymentAccount());
        public static int Price(Product p)
        {
            switch(p)
            {case Product.Jump:return 199;case Product.FireFlower:return 299;case Product.Purify:return 1299;
                case Product.MasterGuide:return 1999;case Product.DoubleJump:return 2999;case Product.Gatling:return 5999;default:return -1;}
        }
        public static int PackCost(int pack)=>pack==0?100:pack==1?980:pack==2?1880:-1;
        public static int PackCoins(int pack)=>pack==0?100:pack==1?1000:pack==2?2000:0;
        public bool Owns(Product p) => owned.Contains(p)||p==Product.Jump&&owned.Contains(Product.DoubleJump);
        public bool CanCollect(ItemKind kind) => kind != ItemKind.Flower || Owns(Product.FireFlower);
        public bool CanFire(Form form, Buff buff) => Owns(Product.Gatling)||buff == Buff.VIP || form == Form.Fire && Owns(Product.FireFlower);
        public bool ExchangeCash(int pack)
        {
            return Payments.Purchase(this,pack,PaymentMethod.Wallet,Guid.NewGuid().ToString("N"),out _);
        }

        public int Credit(int cents)
        {
            int credited=Math.Min(Math.Max(cents,0),Math.Max(0,WalletLimit-wallet));
            wallet+=credited;return credited;
        }
        public bool Grant(string key, int cents)
        {
            if (cents < 0 || claimed.Contains(key)) return false;
            claimed.Add(key); Credit(cents);
            return true;
        }
        public bool Buy(Product product, int configuredPrice = -1)
        {
            int cost = configuredPrice == -1 ? Price(product) : configuredPrice;
            if (Price(product)<0 || cost < 0 || Owns(product) || coins < cost) return false;
            coins -= cost; owned.Add(product);
            receipts.Add(new Receipt { product = product, coins = cost });
            if (product == Product.Purify && refunds > 0 && !refundBonus)
            { score += 500; refundBonus = true; }
            return true;
        }
        public bool RefundFireFlower()
        {
            if (world != 2 || !Owns(Product.FireFlower)) return false;
            for (int i = receipts.Count-1; i >= 0; i--)
            {
                var receipt = receipts[i];
                if (receipt.product != Product.FireFlower || receipt.refunded) continue;
                if(receipt.coins>int.MaxValue-coins)return false;
                receipt.refunded = true; coins+=receipt.coins;
                owned.Remove(Product.FireFlower); refunds++; return true;
            }
            return false;
        }
        public bool ScoreOnce(string id, int points)
        {
            if (collected.Contains(id)) return false;
            collected.Add(id); score += points; return true;
        }
        public int PaidTotal()
        { int total=0; foreach (var r in receipts) total+=r.coins; return total; }
        public int RefundedTotal()
        { int total=0; foreach (var r in receipts) if(r.refunded) total+=r.coins; return total; }
    }
}
