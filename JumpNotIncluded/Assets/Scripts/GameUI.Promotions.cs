using System;
using UnityEngine;

namespace JumpNotIncluded
{
    public partial class GameUI
    {
        private readonly Color promoRed=new Color(.72f,.08f,.15f),promoWine=new Color(.20f,.035f,.095f);

        // The same imported 10x14 coin is used for gameplay and every UI balance.
        // Currency is an image; the amount remains real text, including every zero.
        private void CoinAmount(float x,float y,float width,float height,string value,int size,Color color,
            TextAnchor anchor=TextAnchor.MiddleLeft,bool retro=false)
        {
            var style=new GUIStyle(textStyle){font=retro?pixelFont:font,fontSize=size,fontStyle=FontStyle.Bold,wordWrap=false};
            float iconHeight=Mathf.Min(height-4,size*1.2f),iconWidth=iconHeight*10/14;
            float textWidth=style.CalcSize(new GUIContent(value)).x+4,gap=10;
            float total=iconWidth+gap+textWidth;
            if(anchor==TextAnchor.MiddleCenter)x+=Mathf.Max(0,(width-total)*.5f);
            else if(anchor==TextAnchor.MiddleRight)x+=Mathf.Max(0,width-total);
            SpriteImage("coin",x,y+(height-iconHeight)*.5f,iconWidth,iconHeight);
            Text(x+iconWidth+gap,y,textWidth,height,value,size,color,true,TextAnchor.MiddleLeft,retro,false);
        }

        private void CoinPriceButton(float x,float y,float w,float h,int amount,Action action,bool enabled)
        {
            Button(x,y,w,h,"",action,enabled,enabled);
            CoinAmount(x+12,y+2,w-24,h-4,Coins(amount),20,enabled?ink:muted,TextAnchor.MiddleCenter);
        }

        private void PromoFrame(string eyebrow,string badge)
        {
            Veil();Box(110,82,1400,756,new Color(0,0,0,.48f));
            Box(98,70,1404,756,gold);
            Gradient(102,74,1396,748,new Color(.20f,.075f,.16f),ink);
            Gradient(104,76,1392,58,promoRed,promoWine);
            Box(104,134,1392,3,gold);
            Text(134,86,988,39,eyebrow,22,paper,true,TextAnchor.MiddleLeft);
            Box(1150,85,314,40,gold);
            Text(1155,89,304,32,badge,18,ink,true,TextAnchor.MiddleCenter);
        }

        private void PromoTag(float x,float y,float w,string text,Color color)
        {
            Box(x,y,w,32,color);
            Text(x+6,y+2,w-12,28,text,15,ink,true,TextAnchor.MiddleCenter);
        }

        private void Spark(float x,float y,float size,Color color)
        {Box(x+size*.4f,y,size*.2f,size,color);Box(x,y+size*.4f,size,size*.2f,color);}

        private void PromoPoster(AdCampaign campaign,bool revive,float x,float y,float w,float h)
        {
            Color accent=campaign==AdCampaign.SutdAI?cyan:campaign==AdCampaign.SlCheater?new Color(.76f,.66f,1):gold;
            Gradient(x,y,w,h,campaign==AdCampaign.Mario?new Color(.40f,.055f,.115f):new Color(.16f,.105f,.30f),ink);
            Box(x,y,w,4,accent);
            for(int i=0;i<8;i++)Spark(x+24+(i*137)%(w-48),y+170+(i*83)%(h-230),i%2==0?12:7,new Color(accent.r,accent.g,accent.b,.45f));
            string heading=campaign==AdCampaign.Mario?(revive?"DEFEAT IS\nTEMPORARY.":"GRAVITY HAS\nA NEW BOSS."):
                campaign==AdCampaign.SutdAI?"UPGRADE YOUR\nHUMAN POTENTIAL.":"THE MOON\nIS ON NOTICE.";
            Text(x+26,y+19,w-52,28,campaign==AdCampaign.Mario?"MARIO / THE PREMIUM COLLECTION":
                campaign==AdCampaign.SutdAI?"SUTD / DESIGN + AI":"SL CHEATER / WAR GOD REPLICA",18,accent,true);
            Text(x+24,y+56,w-48,126,heading,campaign==AdCampaign.SutdAI?43:44,paper,true);
            if(campaign==AdCampaign.Mario)
            {
                float bob=Mathf.Sin(Time.unscaledTime*1.5f)*3;
                SpriteImage("star",x+55,y+206,62,62);
                SpriteImage("flower",x+w-113,y+245,66,66);
                SpriteImage("coin",x+139,y+288,30,42);
                SpriteImage("coin",x+w-162,y+178,30,42);
                if(revive)SpriteImage("small-jump",x+w*.5f-100,y+191+bob,200,178,true);
                else SpriteImage("fire-idle",x+w*.5f-53,y+178+bob,106,212,true);
                for(int i=0;i<5;i++)SpriteImage(i==2?"question":"brick",x+w*.5f-120+i*48,y+390,48,48);
                Text(x+30,y+456,w-60,60,revive?"Your legend deserves a better ending.":"Own the jump. Own the fire. Own the ending.",22,paper,true,TextAnchor.MiddleCenter);
            }
            else if(campaign==AdCampaign.SutdAI)
            {
                if(revive)
                {
                    GUI.DrawTexture(new Rect(x+w*.54f,y+192,w*.41f,355),game.assets.sutdRobotAd,ScaleMode.ScaleToFit,true);
                    Text(x+30,y+220,w*.47f,128,"DESIGN.\nBUILD.\nOUTTHINK.",30,cyan,true);
                    Text(x+30,y+376,w*.47f,138,"Command AI.\nPrototype tomorrow\nbefore it launches.",23,paper,true);
                }
                else
                {
                    Box(x+24,y+187,w-48,219,Color.black);
                    GUI.DrawTexture(new Rect(x+30,y+193,w-60,207),game.assets.sutdAIAd,ScaleMode.ScaleToFit,true);
                    Text(x+30,y+424,w-60,67,"Design the impossible. Command AI.\nPrototype tomorrow before it launches.",22,paper,true);
                }
                Text(x+30,y+h-31,w-60,25,"SUTD artwork / in-game parody promotion",14,muted);
            }
            else
            {
                if(game.assets.mechPortrait!=null)
                    GUI.DrawTexture(new Rect(x+w*.48f,y+175,w*.49f,332),game.assets.mechPortrait,ScaleMode.ScaleToFit,true);
                Text(x+30,y+206,w*.48f,79,"MOON KILLER\nEDITION",29,gold,true);
                Text(x+30,y+303,w*.46f,84,"STARFIELD\nCAMOUFLAGE",21,paper,true);
                Text(x+30,y+388,w*.47f,69,"1% LIGHT SPEED.\n100% MAIN CHARACTER.",19,accent,true);
                Text(x+30,y+h-38,w-60,30,"Orbital authority. S$79.99 SGD wallet.",21,gold,true);
            }
        }

        private void PromoProduct(float x,float y,float w,string icon,string name,int price,bool owned=false)
        {
            Box(x,y,w,113,gold);Gradient(x+2,y+2,w-4,109,new Color(.23f,.17f,.24f),panel);
            SpriteImage(icon,x+16,y+18,47,47,icon=="small-jump");
            Text(x+79,y+12,w-91,31,name,19,paper,true);
            if(owned)Text(x+79,y+48,w-91,40,"OWNED",21,cyan,true);
            else CoinAmount(x+79,y+48,w-91,40,Coins(price),25,gold);
            Text(x+16,y+88,w-32,22,"UPGRADE / THIS RUN",13,muted,true);
        }

        private void PromotionDeath()
        {
            PromoFrame("DEFEATED? CONGRATULATIONS. YOU QUALIFY.","THE COMEBACK COLLECTION");
            PromoPoster(AdCampaign.Mario,true,128,159,594,615);
            PromoTag(752,162,224,"EXCLUSIVE TO THE FALLEN",gold);
            Text(750,214,708,118,"YOUR NEXT LIFE.\nNOW WITH BENEFITS.",43,paper,true);
            Text(754,343,690,53,game.deathReason,21,muted);
            PromoProduct(754,412,335,"small-jump","JUMP DLC",RunModel.Price(Product.Jump),game.Run.Owns(Product.Jump));
            PromoProduct(1105,412,335,"flower","FIRE FLOWER",RunModel.Price(Product.FireFlower),game.Run.Owns(Product.FireFlower));
            Text(754,542,686,30,"WHY ACCEPT DEFEAT WHEN YOU CAN FINANCE VICTORY?",17,gold,true);
            Button(754,587,686,65,"REVIVE NOW  /  WATCH 2 SECONDS",()=>game.StartAd(AdKind.Revive),true);
            Button(754,668,334,55,game.Run.wallet<RunModel.WalletLimit?"EARN S$1 / SECOND":"WALLET FULL / S$99",()=>game.StartAd(AdKind.Cash),false,game.Run.wallet<RunModel.WalletLimit);
            Button(1106,668,334,55,"SHOP THE ADVANTAGE",game.OpenShop,false);
            Text(754,742,270,28,"SCORE "+game.Run.score.ToString("000000")+" / DEATHS "+game.Run.deaths,16,muted);
            Button(1050,737,187,40,"RESTART RUN",game.NewGame,false,true,true);
            Button(1253,737,187,40,"MAIN MENU",game.ToMenu,false,true,true);
            Text(129,788,1312,25,"Upgrades sold separately. Every comeback begins with a 2-second revival ad.",15,muted,false,TextAnchor.MiddleCenter);
        }

        private void WalletReward(float x,float y,float w,int second,bool earned)
        {
            Box(x,y,w,96,earned?gold:line);
            Gradient(x+2,y+2,w-4,92,earned?new Color(.30f,.23f,.10f):panel,ink);
            Text(x+4,y+8,w-8,23,"SECOND "+second,14,earned?gold:muted,true,TextAnchor.MiddleCenter);
            Text(x+4,y+30,w-8,40,"+S$1",26,earned?gold:paper,true,TextAnchor.MiddleCenter);
            Text(x+4,y+69,w-8,23,earned?"BANKED":"SGD WALLET",13,earned?gold:muted,true,TextAnchor.MiddleCenter);
        }

        private void PromotionAd()
        {
            bool revive=game.adKind==AdKind.Revive;
            PromoFrame(revive?"THE COMEBACK EVENT / YOUR NEXT LIFE IS LOADING":"WATCH. EARN. BECOME UNREASONABLY POWERFUL.",revive?"2 SECONDS TO GLORY":"S$1 EVERY FULL SECOND");
            PromoPoster(game.adCampaign,revive,128,159,754,615);
            Box(908,159,564,615,gold);Gradient(911,162,558,609,new Color(.20f,.17f,.21f),ink);
            PromoTag(932,179,516,revive?"RESURRECTION REWARD":"YOUR ATTENTION. YOUR PAYDAY.",gold);
            if(revive)
            {
                SpriteImage("small-idle",952,247,70,70,true);
                Text(1044,240,385,85,"ONE MORE\nSHOT AT GLORY",30,paper,true);
                Text(936,349,508,97,Mathf.Max(1,Mathf.CeilToInt(SceneRoot.ReviveAdDuration-game.adTime))+"s",68,gold,true,TextAnchor.MiddleCenter);
                Text(936,451,508,65,"CHECKPOINT REVIVAL\nYour purchases come with you.",23,paper,true,TextAnchor.MiddleCenter);
                Text(940,548,500,56,"The comeback is included.\nCash and upgrades are sold separately.",18,muted,false,TextAnchor.MiddleCenter);
            }
            else
            {
                Text(936,231,508,32,"CASH EARNED THIS AD",18,muted,true,TextAnchor.MiddleCenter);
                Text(936,272,508,89,"+"+Money(game.adEarned),61,gold,true,TextAnchor.MiddleCenter);
                Text(936,372,508,34,"SGD WALLET   "+Money(game.Run.wallet)+" / S$99.00",21,paper,true,TextAnchor.MiddleCenter);
                int seconds=Mathf.FloorToInt(game.adTime),first=seconds/3*3;
                for(int i=0;i<3;i++)WalletReward(935+i*174,433,162,first+i+1,seconds>first+i);
                Box(935,547,510,46,new Color(.23f,.065f,.105f));
                Text(948,554,484,31,"S$1.00",24,gold,true);
                Text(1060,556,153,27,"BUYS YOU",16,paper,true,TextAnchor.MiddleCenter);
                CoinAmount(1222,551,200,36,"100",26,gold);
            }
            float progress=revive?game.adTime/SceneRoot.ReviveAdDuration:game.adTime-Mathf.Floor(game.adTime);
            Box(935,617,510,12,line);Box(935,617,510*Mathf.Clamp01(progress),12,gold);
            Text(935,641,510,36,revive?"AUTO-REVIVE WHEN THE AD FINISHES":
                "NEXT S$1 IN "+(1-progress).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"s",19,paper,true,TextAnchor.MiddleCenter);
            if(revive)
            {
                Gradient(935,696,510,54,new Color(.40f,.30f,.10f),new Color(.24f,.17f,.09f));
                Text(943,704,494,37,"PREPARING YOUR COMEBACK...",21,gold,true,TextAnchor.MiddleCenter);
            }
            else Button(935,696,510,54,"COLLECT CASH & RETURN",game.FinishAd,true);
            Text(129,788,1312,25,revive?"Revival only / no cash reward / watch the full 2 seconds":
                "Full seconds are banked instantly. Stop any time. SGD cash converts to coins in the shop. All rewards are simulated.",15,muted,false,TextAnchor.MiddleCenter);
        }
    }
}
