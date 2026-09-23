using System;

namespace JumpNotIncluded.EditorTools
{
    public static class RuleChecks
    {
        private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        private static RunModel FirstWorld(bool flower=true)
        {
            var r=new RunModel();
            Check(!r.Buy(Product.Jump),"Cannot buy before funding.");
            r.Credit(200);
            Check(!r.Buy(Product.Jump),"Cash cannot bypass the coin exchange.");
            Check(r.ExchangeCash(0)&&r.ExchangeCash(0)&&r.coins==200&&r.wallet==0,"Two small packs exchange two dollars for 200 coins.");
            Check(r.Buy(Product.Jump)&&!r.Buy(Product.Jump)&&r.coins==1,"Jump spends coins once.");
            r.Credit(300);for(int i=0;i<3;i++)r.ExchangeCash(0);
            if(flower)Check(r.Buy(Product.FireFlower),"Fire flower purchase.");
            Check(!r.RefundFireFlower(),"Refund is only available in world 2.");
            r.Grant("world1",400);r.world=2;r.Grant("support",600);return r;
        }
        public static void Run()
        {
            var refund=FirstWorld();Check(refund.wallet==1000&&refund.coins==2,"Cash bonuses and coin change stay separate.");
            Check(refund.CanFire(Form.Fire,Buff.None),"Purchased flower permits fireballs in Fire form.");
            Check(!refund.CanFire(Form.Small,Buff.None)&&!refund.CanFire(Form.Super,Buff.None),"Damage removes fireball access.");
            Check(refund.RefundFireFlower()&&!refund.RefundFireFlower(),"Refund once per receipt.");
            Check(!refund.Owns(Product.FireFlower)&&!refund.CanCollect(ItemKind.Flower)&&!refund.CanFire(Form.Fire,Buff.None),"Refund revokes flower access.");
            Check(refund.CanFire(Form.Small,Buff.VIP)&&!refund.CanFire(Form.Small,Buff.Star),"VIP fire is independent of flower ownership.");
            Check(refund.wallet==1000&&refund.coins==301,"Refund returns the exact number of coins without altering cash.");
            Check(refund.ExchangeCash(1)&&refund.wallet==20&&refund.coins==1301,"Discounted pack converts exactly $9.80 into 1000 coins.");
            Check(refund.Buy(Product.Purify)&&refund.coins==2,"Refund route reaches Mushroom ID.");
            Check(refund.PaidTotal()==1797&&refund.RefundedTotal()==299&&refund.score==500,"Coin receipt arithmetic and one refund reward.");
            var skip=FirstWorld(false);Check(skip.ExchangeCash(1)&&skip.Buy(Product.Purify)&&skip.coins==2,"Skip-flower route reaches Mushroom ID.");
            Check(skip.ScoreOnce("enemy-a",200)&&!skip.ScoreOnce("enemy-a",200),"Enemy score deduplication.");
            var fresh=new RunModel();Check(fresh.wallet==0&&fresh.coins==0&&fresh.owned.Count==0,"New run clears both currencies and ownership.");
            Check(!fresh.ExchangeCash(0)&&!fresh.ExchangeCash(-1)&&!fresh.ExchangeCash(3)&&!fresh.Buy((Product)42,0),"Unfunded or invalid transactions fail.");
            Check(!fresh.Grant("negative",-10)&&fresh.Credit(-100)==0,"Negative cash rewards are rejected.");
            fresh.Credit(1879);Check(!fresh.ExchangeCash(2)&&fresh.wallet==1879&&fresh.coins==0,"Insufficient pack purchase is atomic.");
            fresh.Credit(1);Check(fresh.ExchangeCash(2)&&fresh.wallet==0&&fresh.coins==2000,"Largest pack converts $18.80 into 2000 coins.");
            fresh.world=2;
            Check(fresh.Buy(Product.FireFlower,450)&&fresh.coins==1550,"Configured ScriptableObject coin price is honored.");
            Check(fresh.RefundFireFlower()&&fresh.coins==2000,"Refund uses the recorded coin price.");
            Check(fresh.Buy(Product.FireFlower,450)&&fresh.RefundFireFlower()&&fresh.coins==2000&&!fresh.RefundFireFlower(),"Repurchase refunds only the new receipt.");
            var capped=new RunModel();
            Check(capped.Credit(9899)==9899&&capped.Credit(100)==1&&capped.wallet==RunModel.WalletLimit,"Cash cap clips partial rewards at $99.");
            Check(capped.Credit(int.MaxValue)==0&&capped.Grant("bonus",400)&&!capped.Grant("bonus",400),"Large rewards cannot overflow or repeat.");
            Check(capped.ExchangeCash(1)&&capped.wallet==8920&&capped.coins==1000,"Exchanging cash makes room for more ads.");
            capped.Buy(Product.FireFlower);capped.Credit(int.MaxValue);capped.world=2;
            Check(capped.RefundFireFlower()&&capped.coins==1000&&capped.wallet==9900,"Full cash wallet does not block a coin refund.");
            capped.coins=int.MaxValue;Check(!capped.ExchangeCash(0)&&capped.wallet==9900,"Overflowing coin conversion is rejected without charging cash.");
            var premium=new RunModel{coins=20000};
            foreach(var item in new[]{Product.MasterGuide,Product.DoubleJump,Product.Gatling})
                Check(premium.Buy(item)&&premium.Owns(item)&&!premium.Buy(item),item+" unlocks once with coins.");
            Check(premium.Owns(Product.Jump)&&!premium.Buy(Product.Jump),"Wings include basic jump and prevent a redundant purchase.");
            Check(premium.CanFire(Form.Small,Buff.None)&&premium.CanFire(Form.Super,Buff.None),"Gatling works without Fire Mario.");
            Check(premium.coins==9003&&premium.wallet==0,"Premium upgrades deduct their exact coin prices.");
            var rotation=new AdRotation(new Random(237));int previous=-1;
            for(int round=0;round<100;round++)
            {
                int seen=0;
                for(int slot=0;slot<3;slot++)
                {
                    int campaign=(int)rotation.Next();
                    Check(campaign>=0&&campaign<3&&campaign!=previous,"Every ad is valid and differs from its predecessor.");
                    seen|=1<<campaign;previous=campaign;
                }
                Check(seen==7,"Each shuffled round covers all three campaigns.");
            }
            int first=(int)new AdRotation(new Random(0)).Next();bool varied=false;
            for(int seed=1;seed<20;seed++)varied|=(int)new AdRotation(new Random(seed)).Next()!=first;
            Check(varied,"Different seeds produce different opening ads.");
        }
    }
}
