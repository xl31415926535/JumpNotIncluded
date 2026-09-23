using UnityEngine;

namespace JumpNotIncluded
{
    public partial class GameUI
    {
        private readonly Color payGreen=new Color(.48f,.87f,.73f),payRed=new Color(1,.64f,.57f);
        private int activityPage;
        private bool paymentFocusPending=true;
        private string Coins(int value)=>value.ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
        private void PaymentCheckout()
        {
            var step=game.checkoutStep;var account=game.Run.Payments;
            if(paymentFocusPending)
            {focus=step==CheckoutStep.Packs?7:step==CheckoutStep.Processing?2:1;paymentFocusPending=false;}
            Veil();Box(120,80,1384,770,new Color(0,0,0,.35f));Box(108,66,1384,770,line);
            Gradient(109,67,1382,768,new Color(.065f,.12f,.20f),new Color(.025f,.05f,.09f));
            Text(144,89,245,44,"JNI / PAY",31,paper,true);
            Text(145,135,610,27,"SINGAPORE DOLLAR CHECKOUT",13,muted,true);
            SpriteImage("coin",989,109,18,25);
            Text(1024,101,204,34,Coins(game.Run.coins)+" coins",22,gold,true,TextAnchor.MiddleRight);
            Box(1263,103,125,28,new Color(.11f,.23f,.25f));
            Text(1266,105,119,24,"SIMULATION",12,payGreen,true,TextAnchor.MiddleCenter);
            Button(1410,93,44,44,"X",game.CloseRecharge,false,true,true);
            Box(144,176,1312,1,line);
            int active=step==CheckoutStep.Packs?0:step==CheckoutStep.Receipt?2:1;
            string[] steps={"01  CHOOSE YOUR PACK","02  PAYMENT DETAILS","03  YOUR RECEIPT"};
            for(int i=0;i<3;i++)
            {
                float x=144+i*444;
                Text(x,191,422,28,steps[i],14,i<=active?paper:muted,true);
                Box(x,225,422,2,i==active?gold:line);
            }
            if(step==CheckoutStep.Activity){PaymentActivity();return;}
            if(step==CheckoutStep.Receipt){PaymentReceiptView();return;}
            if(step==CheckoutStep.Packs)PaymentPacks();
            else if(step==CheckoutStep.LinkCard)
            {
                Text(144,249,826,48,"Your virtual card. Ready to link.",34,paper,true);
                Text(145,305,802,52,"A pre-issued card for this run. Linking is free; payment happens only after you confirm the amount.",19,muted);
                VirtualCard(168,387,744,236);
                Text(168,648,744,32,"AVAILABLE CREDIT   "+Money(account.AvailableCredit)+" SGD",20,payGreen,true);
                Text(168,692,744,52,"This generated card is a game prop. No real card details are needed.",17,muted);
            }
            else if(step==CheckoutStep.Review)
            {
                Text(144,249,826,48,"A little unfair advantage.",35,paper,true);
                Text(145,306,802,49,"Check your payment method and total. Your "+Coins(RunModel.PackCoins(game.selectedPack))+" coins will be added after confirmation.",20,muted);
                if(game.paymentMethod==PaymentMethod.VirtualCard)VirtualCard(168,387,744,236);
                else
                {
                    Gradient(168,387,744,236,new Color(.10f,.26f,.32f),panel);
                    Text(201,410,678,30,"SGD WALLET",17,payGreen,true);
                    Text(201,466,678,66,Money(game.Run.wallet),46,paper,true);
                    Text(201,558,678,34,game.Run.wallet>=RunModel.PackCost(game.selectedPack)?"AFTER PAYMENT  "+Money(game.Run.wallet-RunModel.PackCost(game.selectedPack)):"Insufficient wallet balance for this pack",20,muted);
                }
                Text(168,650,744,38,game.paymentMethod==PaymentMethod.VirtualCard?"AVAILABLE CREDIT   "+Money(account.AvailableCredit)+" SGD":"One payment. No subscription. No added fees.",20,paper);
                Text(168,698,744,35,"No subscription. Coins and upgrades belong to this run.",17,muted);
            }
            else
            {
                Text(144,267,826,55,"Confirming your payment",36,paper,true);
                Text(145,336,802,42,game.paymentMethod==PaymentMethod.VirtualCard?"Authorizing your virtual card ending "+PaymentAccount.LastFour:"Checking your SGD wallet balance",21,muted);
                float progress=game.paymentElapsed/SceneRoot.PaymentDuration;
                Box(168,465,744,8,line);Box(168,465,744*progress,8,payGreen);
                Text(168,505,744,45,progress<.55f?"01 / Confirming the amount":"02 / Preparing your coin delivery",22,payGreen,true);
                Text(168,571,744,34,game.paymentOrderId,17,muted);
                Text(168,634,744,58,"You can cancel before this completes. Leaving the checkout cancels the pending payment.",18,muted);
            }
            PaymentSummary();
            if(step==CheckoutStep.Packs)
            {
                Button(144,773,244,36,"PAYMENT ACTIVITY",()=>{activityPage=0;game.ShowPaymentActivity();},false,true,true);
                if(account.cardLinked)Button(406,773,164,36,"UNLINK CARD",game.UnlinkPaymentCard,false,true,true);
            }
            else Button(144,773,244,36,step==CheckoutStep.Processing?"CANCEL PAYMENT":"BACK TO PACKS",game.BackToPacks,false,true,true);
            Text(554,779,902,27,"Demo checkout / SGD / No real money is charged",13,muted,false,TextAnchor.MiddleRight);
        }
        private void PaymentPacks()
        {
            Text(144,249,826,45,"Small purchase. Big main-character energy.",29,paper,true);
            Text(145,300,802,28,"Choose a coin pack. All prices are in Singapore dollars.",18,muted);
            string[] labels={"STARTER","MOST POPULAR","BEST VALUE / SAVE 6%"};
            for(int i=0;i<3;i++)
            {
                int pack=i;float x=144+i*280;bool selected=i==game.selectedPack;
                Box(x,350,266,212,selected?gold:line);
                Gradient(x+1,351,264,210,selected?new Color(.17f,.24f,.29f):panel,ink);
                Text(x+17,363,232,25,labels[i],12,selected?gold:muted,true);
                Text(x+17,401,191,43,Coins(RunModel.PackCoins(i)),35,paper,true);
                SpriteImage("coin",x+217,408,20,28);
                Text(x+18,447,230,37,Money(RunModel.PackCost(i))+" SGD",23,selected?gold:paper,true);
                Text(x+18,485,226,25,i==1?"Save S$0.20 on 1,000 coins":i==2?"Save S$1.20 on 2,000 coins":"100 coins. A modest beginning.",12,muted);
                Button(x+15,515,236,32,selected?"SELECTED":"SELECT PACK",()=>game.SelectPack(pack),selected);
            }
            Text(145,580,826,30,"PAYMENT METHOD",14,muted,true);
            bool card=game.paymentMethod==PaymentMethod.VirtualCard;
            Button(144,620,826,49,game.Run.Payments.cardLinked?"VIRTUAL CARD  /  **** "+PaymentAccount.LastFour+"  /  "+Money(game.Run.Payments.AvailableCredit)+" AVAILABLE":"LINK A VIRTUAL CARD  /  "+Money(game.Run.Payments.AvailableCredit)+" CREDIT",()=>game.SelectPayment(PaymentMethod.VirtualCard),card);
            Button(144,681,569,49,"SGD WALLET  /  "+Money(game.Run.wallet),()=>game.SelectPayment(PaymentMethod.Wallet),!card);
            Button(728,681,242,49,"EARN SGD",()=>game.StartAd(AdKind.Cash),false,game.Run.wallet<RunModel.WalletLimit);
        }
        private void VirtualCard(float x,float y,float w,float h)
        {
            Box(x+7,y+9,w,h,new Color(0,0,0,.25f));
            Gradient(x,y,w,h,new Color(.20f,.31f,.42f),new Color(.075f,.13f,.21f));
            for(int i=0;i<7;i++)Box(x+w-155+i*16,y+24,1,h-48,new Color(.75f,.82f,.86f,.09f));
            Text(x+28,y+23,w-56,32,"JNI RESERVE",23,paper,true);
            Text(x+28,y+64,w-56,25,"VIRTUAL CREDIT / SGD",13,gold,true);
            Box(x+30,y+106,43,32,gold);Box(x+48,y+106,1,32,panel);Box(x+30,y+122,43,1,panel);
            Text(x+100,y+101,w-125,45,"****   ****   ****   "+PaymentAccount.LastFour,31,paper,true);
            Text(x+28,y+h-59,w-170,30,"PLAYER ONE",17,paper,true);
            Text(x+28,y+h-29,w-170,21,"GENERATED FOR THIS RUN",11,muted);
            Text(x+w-168,y+h-56,140,36,game.Run.Payments.cardLinked?"LINKED":"NOT LINKED",15,game.Run.Payments.cardLinked?payGreen:gold,true,TextAnchor.MiddleRight);
        }
        private void PaymentDetail(float y,string label,string value,Color color)
        {
            Text(1041,y,174,29,label,16,muted);
            Text(1196,y,221,32,value,18,color,true,TextAnchor.UpperRight);
        }
        private void PaymentSummary()
        {
            float x=1002;var step=game.checkoutStep;int cost=RunModel.PackCost(game.selectedPack);
            Box(x,249,454,490,line);Gradient(x+1,250,452,488,panel,ink);
            Text(1040,273,378,36,"Order summary",26,paper,true);
            PaymentDetail(332,"Coin pack",Coins(RunModel.PackCoins(game.selectedPack))+" coins",paper);
            PaymentDetail(373,"Processing fee","S$0.00",muted);
            Box(1040,418,378,1,line);
            Text(1040,440,100,34,"TOTAL",16,muted,true);
            Text(1136,431,282,52,Money(cost),39,paper,true,TextAnchor.MiddleRight);
            Text(1150,486,268,26,"SGD / ONE-TIME PAYMENT",12,muted,false,TextAnchor.MiddleRight);
            string method=game.paymentMethod==PaymentMethod.VirtualCard?"Virtual card ending "+PaymentAccount.LastFour:"SGD wallet balance";
            Text(1040,533,378,31,method,17,paper);
            string problem=step==CheckoutStep.Review?game.Run.Payments.Problem(game.Run,game.selectedPack,game.paymentMethod):null;
            string message=game.paymentError??problem;
            Text(1040,580,378,57,message??(step==CheckoutStep.LinkCard?"Linking this card does not charge it.":"Coins are delivered to your current run. Your payment will appear in Activity."),16,message!=null?payRed:muted);
            string label=step==CheckoutStep.Packs?"CONTINUE":step==CheckoutStep.LinkCard?"LINK VIRTUAL CARD":step==CheckoutStep.Review?"PAY "+Money(cost):"PROCESSING...";
            Button(1039,660,380,54,label,()=>
            {if(step==CheckoutStep.Packs)game.ContinueCheckout();else if(step==CheckoutStep.LinkCard)game.LinkPaymentCard();else if(step==CheckoutStep.Review)game.ConfirmTopUp();},true,step!=CheckoutStep.Processing&&(step!=CheckoutStep.Review||problem==null));
        }
        private void PaymentReceiptView()
        {
            var receipt=game.PaymentReceipt;
            if(receipt==null){game.BackToPacks();return;}
            Box(144,256,826,482,line);Gradient(145,257,824,480,new Color(.075f,.19f,.20f),ink);
            Box(177,288,79,42,payGreen);Text(179,294,75,28,"PAID",19,ink,true,TextAnchor.MiddleCenter);
            Text(177,356,752,55,"Your advantage has arrived.",38,paper,true);
            Text(177,431,752,71,"+"+Coins(receipt.coins)+" coins",51,gold,true);
            Text(178,535,730,35,"BALANCE AFTER PAYMENT   "+Coins(receipt.balanceAfter)+" coins",20,paper,true);
            Text(178,598,730,30,receipt.createdAt,17,muted);
            Text(178,641,730,34,receipt.id,20,payGreen,true);
            Text(178,694,730,26,"SIMULATED PAYMENT / NO REAL MONEY CHARGED",13,muted);
            Box(1002,256,454,482,line);Gradient(1003,257,452,480,panel,ink);
            Text(1040,286,378,38,"Payment receipt",27,paper,true);
            PaymentDetail(354,"Currency","SGD",paper);
            PaymentDetail(405,"Amount paid",Money(receipt.cents),gold);
            PaymentDetail(456,"Status","Completed",payGreen);
            Text(1040,524,378,60,receipt.method==PaymentMethod.VirtualCard?"JNI virtual card  /  **** "+PaymentAccount.LastFour:"Paid from SGD wallet",20,paper);
            PaymentDetail(583,receipt.method==PaymentMethod.VirtualCard?"Credit left":"Wallet left",Money(receipt.fundsAfter),paper);
            Text(1040,626,378,27,"BALANCE AT TIME OF PAYMENT",12,muted);
            Button(1039,660,380,54,"BACK TO UPGRADES",game.CloseRecharge,true);
            Button(144,773,225,36,"BUY ANOTHER PACK",game.BackToPacks,false,true,true);
            Button(391,773,225,36,"PAYMENT ACTIVITY",()=>{activityPage=0;game.ShowPaymentActivity();},false,true,true);
            Text(665,779,791,27,"Receipt saved for this run",14,muted,false,TextAnchor.MiddleRight);
        }
        private void PaymentActivity()
        {
            var orders=game.Run.Payments.orders;int pages=Mathf.Max(1,(orders.Count+5)/6);
            activityPage=Mathf.Clamp(activityPage,0,pages-1);
            Text(144,251,860,48,"Payment activity",33,paper,true);
            Text(1020,258,436,33,orders.Count+" COMPLETED ORDERS",15,muted,true,TextAnchor.MiddleRight);
            Text(145,307,1310,35,"Virtual card: "+Money(game.Run.Payments.Total(PaymentMethod.VirtualCard))+" SGD    /    Wallet: "+Money(game.Run.Payments.Total(PaymentMethod.Wallet))+" SGD",19,gold);
            if(orders.Count==0)
            {
                Text(250,430,1100,60,"Your first receipt starts here.",36,paper,true,TextAnchor.MiddleCenter);
                Text(250,513,1100,46,"Completed payments appear here. Browsing, linking and cancelled payments do not create charges.",20,muted,false,TextAnchor.MiddleCenter);
            }
            for(int row=0;row<6;row++)
            {
                int index=orders.Count-1-activityPage*6-row;if(index<0)break;
                var order=orders[index];float y=363+row*60;
                Box(144,y,1312,51,panel);
                Text(162,y+12,286,29,order.createdAt,14,muted);
                Text(457,y+10,209,32,"+"+Coins(order.coins)+" coins",20,paper,true);
                Text(677,y+12,293,28,order.method==PaymentMethod.VirtualCard?"VIRTUAL CARD **** "+PaymentAccount.LastFour:"SGD WALLET",14,muted);
                Text(976,y+10,205,32,Money(order.cents),20,gold,true,TextAnchor.MiddleRight);
                Text(1205,y+14,78,24,"PAID",13,payGreen,true);
                Button(1300,y+6,134,38,"RECEIPT",()=>game.ShowPaymentReceipt(order.id),false,true,true);
            }
            Button(144,773,244,36,"BACK TO PACKS",game.BackToPacks,false,true,true);
            Button(1020,773,123,36,"PREVIOUS",()=>activityPage--,false,activityPage>0,true);
            Text(1152,780,151,29,(activityPage+1)+" / "+pages,16,muted,false,TextAnchor.MiddleCenter);
            Button(1320,773,136,36,"NEXT",()=>activityPage++,false,activityPage+1<pages,true);
        }
    }
}
