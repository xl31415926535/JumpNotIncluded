using System;
using UnityEngine;

namespace JumpNotIncluded
{
    public class GameUI : MonoBehaviour
    {
        private SceneRoot game;
        private Font font,pixelFont;
        private int focus,buttonIndex,lastCount,displayedScore;
        private bool submit;
        private float navCooldown;
        private GUIStyle textStyle;
        private GameEvents channel;
        private static readonly string[] AdBrands={"MARIO POWER-UPS","SUTD / AI","SL CHEATER / CYBERWARE"};
        private static readonly string[] AdHeadlines={"Gravity respects premium members.","The future called. It wants your brain.","Mortality is an outdated specification."};
        private static readonly string[] AdCopy={"Conquer an entire new dimension. Own the sun. Make the laws of physics negotiate with your wallet.","Design the impossible. Command AI. Let yesterday's geniuses watch you prototype tomorrow.","The War God chassis: trade your biological limitations for orbital authority. Become the final boss."};
        private readonly Color ink=new Color(.025f,.055f,.10f),panel=new Color(.055f,.105f,.18f),
            muted=new Color(.61f,.69f,.78f),paper=new Color(.95f,.95f,.92f),gold=new Color(.86f,.71f,.43f),
            cyan=new Color(.53f,.79f,.94f),line=new Color(.22f,.31f,.43f);
        private bool Classic=>game.mode==ScreenMode.Menu||game.mode==ScreenMode.Loading||game.mode==ScreenMode.Pause||game.mode==ScreenMode.Playing;
        public void Init(SceneRoot root)
        {
            game=root;
            font=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},24);
            pixelFont=Font.CreateDynamicFontFromOSFont(new[]{"Consolas","Courier New"},24);
            channel=game.events;displayedScore=game.Run.score;channel.ScoreChanged+=OnScoreChanged;
        }
        private void OnScoreChanged(int value){displayedScore=value;}
        private void OnDestroy()
        {if(channel!=null)channel.ScoreChanged-=OnScoreChanged;if(font!=null)Destroy(font);if(pixelFont!=null)Destroy(pixelFont);}
        public void ResetFocus(){focus=0;submit=false;}
        private void Update()
        {
            navCooldown-=Time.unscaledDeltaTime;
            float n=game.input.navigate.ReadValue<float>();
            if(Mathf.Abs(n)>.5f&&navCooldown<=0)
            {focus=(focus+(n>0?1:-1)+Mathf.Max(lastCount,1))%Mathf.Max(lastCount,1);navCooldown=.18f;}
            if(game.input.submit.WasPressedThisFrame())submit=true;
        }
        private void OnGUI()
        {
            if(game==null||font==null)return;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            Vector2 offset=new Vector2((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f);
            GUI.matrix=Matrix4x4.TRS(offset,Quaternion.identity,new Vector3(scale,scale,1));
            textStyle=new GUIStyle(GUI.skin.label){font=font,wordWrap=true};buttonIndex=0;
            if(game.mode==ScreenMode.Menu)Menu();
            else if(game.mode==ScreenMode.Loading)Loading();
            else
            {
                Hud();
                if(game.mode==ScreenMode.Shop){if(game.rechargeOpen)Recharge();else Shop();}
                else if(game.mode==ScreenMode.Ad)Advertisement();
                else if(game.mode==ScreenMode.Dead)Death();
                else if(game.mode==ScreenMode.Pause)Pause();
                else if(game.mode==ScreenMode.Results)Results();
                if(game.mode==ScreenMode.Shop&&Time.unscaledTime<game.toastUntil)
                {Box(200,839,1200,47,ink);Text(224,844,1152,36,game.toast,20,gold,false,TextAnchor.MiddleCenter);}
            }
            lastCount=buttonIndex;
            if(Event.current.type==EventType.Repaint)submit=false;
        }
        private void Box(float x,float y,float w,float h,Color color)
        {var prev=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=prev;}
        private void Text(float x,float y,float w,float h,string value,int size,Color color,bool bold=false,TextAnchor anchor=TextAnchor.UpperLeft,bool retro=false)
        {
            textStyle.font=retro?pixelFont:font;textStyle.fontSize=size;textStyle.normal.textColor=color;
            textStyle.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;textStyle.alignment=anchor;
            GUI.Label(new Rect(x,y,w,h),value,textStyle);
        }
        private void Button(float x,float y,float w,float h,string label,Action action,bool primary=false,bool enabled=true,bool quiet=false)
        {
            int index=buttonIndex++;var rect=new Rect(x,y,w,h);bool hover=rect.Contains(Event.current.mousePosition);
            if(!Classic)
            {
                bool active=enabled&&(hover||focus==index);
                if(!quiet)
                {
                    Box(x,y+3,w,h,new Color(0,0,0,.25f));
                    Box(x,y,w,h,active?gold:enabled&&primary?gold:line);
                    Color fill=!enabled?panel:primary?gold:active?new Color(.13f,.24f,.37f):new Color(.08f,.16f,.26f);
                    Gradient(x+1,y+1,w-2,h-2,fill,Color.Lerp(fill,ink,.15f));
                    if(primary&&enabled)Box(x+2,y+2,w-4,1,new Color(1,.91f,.69f));
                }
                else if(active)Box(x+14,y+h-3,w-28,1,gold);
                if(focus==index&&enabled)Box(x-5,y+8,2,h-16,gold);
            }
            else if(focus==index||hover)Text(x-30,y,w,h,">",28,paper,true,TextAnchor.MiddleLeft,true);
            Text(x+14,y+4,w-28,h-8,label,Classic?25:quiet?17:20,enabled?(!Classic&&primary&&!quiet?ink:paper):muted,true,TextAnchor.MiddleCenter,Classic&&game.mode!=ScreenMode.Pause);
            bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);
            bool selected=submit&&focus==index&&Event.current.type==EventType.Repaint;
            if(enabled&&(clicked||selected)){submit=false;action?.Invoke();}
        }
        private string Money(int cents)=>"$"+(cents/100f).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
        private void Veil()
        {
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            Box(0,0,Screen.width,Screen.height,new Color(.015f,.03f,.055f,.83f));GUI.matrix=matrix;
        }
        private void Gradient(float x,float y,float w,float h,Color top,Color bottom)
        {for(int i=0;i<24;i++)Box(x,y+h*i/24,w,h/24+1,Color.Lerp(top,bottom,i/23f));}
        private void Window(float x,float y,float w,float h,string eyebrow,string title)
        {
            Box(x+10,y+12,w,h,new Color(0,0,0,.4f));Box(x-1,y-1,w+2,h+2,gold);
            Gradient(x,y,w,h,new Color(.09f,.18f,.29f),ink);
            Box(x+20,y+20,w-40,1,line);Box(x+20,y+h-20,w-40,1,line);
            Box(x+30,y+34,4,18,gold);Text(x+48,y+31,w-125,31,eyebrow,17,gold,true);
            Text(x+30,y+78,w-90,72,title,40,paper,true);
        }
        private void SpriteImage(string key,float x,float y,float w,float h,bool mario=false)
        {
            if(Event.current.type!=EventType.Repaint)return;
            mario=mario||key.StartsWith("small-",StringComparison.Ordinal);
            var sprite=game.assets.Sprite(key);var texture=sprite.texture;Rect r=sprite.rect;
            var uv=new Rect(r.x/texture.width,r.y/texture.height,r.width/texture.width,r.height/texture.height);
            if(mario){uv.x+=uv.width;uv.width=-uv.width;}
            Graphics.DrawTexture(new Rect(x,y,w,h),texture,uv,0,0,0,0,Color.white,key=="wings"?null:mario?game.assets.blueKey:game.assets.greenKey);
        }
        private void HeroArt(float x,float y,float w,float h,bool fire=false)
        {
            Gradient(x,y,w,h,new Color(.12f,.27f,.42f),new Color(.045f,.095f,.16f));
            Box(x+18,y+18,w-36,1,line);Box(x+18,y+h-19,w-36,1,line);
            Text(x+24,y+28,w-48,28,fire?"THE SOLAR SOVEREIGN COLLECTION":"THE COMEBACK OF THE CENTURY",14,gold,true,TextAnchor.MiddleCenter);
            Text(x+24,y+68,w-48,80,fire?"BECOME\nTHE SUN.":"RISE AGAIN.\nRULE AGAIN.",32,paper,true,TextAnchor.MiddleCenter);
            float bob=Mathf.Sin(Time.unscaledTime*1.3f)*4;
            SpriteImage("coin",x+43,y+h*.43f+bob,30,42);
            SpriteImage(fire?"flower":"star",x+w-83,y+h*.37f-bob,46,46);
            float height=h*.49f,width=fire?height*.5f:height*1.125f;
            SpriteImage(fire?"fire-idle":"small-jump",x+(w-width)*.5f,y+h-height-70+bob,width,height,true);
            for(int i=0;i<3;i++)SpriteImage(i==1?"question":"brick",x+w*.5f-72+i*48,y+h-70,48,48);
        }
        private void TechnologyAdArt(AdCampaign campaign,float x,float y,float w,float h)
        {
            bool campus=campaign==AdCampaign.SutdAI;
            Box(x,y,w,h,Color.black);
            if(campus&&game.adKind==AdKind.Cash)
            {
                Text(x+24,y+20,w-48,34,"SUTD / DESIGN + AI",25,paper,true,TextAnchor.MiddleCenter);
                GUI.DrawTexture(new Rect(x+8,y+72,w-16,310),game.assets.sutdAIAd,ScaleMode.ScaleToFit,true);
                Text(x+24,y+h-66,w-48,28,"DESIGN AND ARTIFICIAL INTELLIGENCE",17,cyan,true,TextAnchor.MiddleCenter);
                Text(x+24,y+h-34,w-48,22,"sutd.edu.sg/dai",14,muted,false,TextAnchor.MiddleCenter);
            }
            else
            {
                GUI.DrawTexture(new Rect(x,y,w,h),campus?game.assets.sutdRobotAd:game.assets.slCheaterAd,ScaleMode.ScaleToFit,true);
                if(!campus)
                {
                    Text(x+18,y+18,150,72,"SL\nCHEATER",26,paper,true);
                    Text(x+18,y+105,120,100,"WAR GOD\nORBITAL\nASSAULT",14,gold,true);
                    Text(x+18,y+h-44,w-36,27,"FLESH IS OPTIONAL. DOMINANCE IS NOT.",17,paper,true,TextAnchor.MiddleCenter);
                }
            }
        }
        private void RewardTile(float x,float y,float w,string icon,string heading,string detail,bool earned=false)
        {
            Box(x,y,w,112,earned?gold:line);Gradient(x+1,y+1,w-2,110,earned?new Color(.22f,.26f,.25f):new Color(.10f,.19f,.30f),panel);
            Text(x+5,y+9,w-10,25,heading,13,earned?gold:muted,true,TextAnchor.MiddleCenter);
            SpriteImage(icon,x+(w-32)*.5f,y+36,32,32);
            Text(x+4,y+81,w-8,26,detail,16,earned?gold:paper,true,TextAnchor.MiddleCenter);
        }
        private void Menu()
        {
            Hud();
            Box(473,168,666,263,new Color(.27f,.08f,.035f));Box(461,156,666,263,new Color(.69f,.2f,.07f));
            Text(495,174,595,195,"SUPER\nMARIO BROS.",72,paper,true,TextAnchor.MiddleCenter,true);
            Button(581,473,438,57,"1 PLAYER GAME",game.NewGame);
            Button(581,550,438,51,"MUSIC  "+(game.audioDirector.MusicEnabled?"ON":"OFF"),game.audioDirector.ToggleMusic);
            Button(581,617,438,51,"SOUND  "+(game.audioDirector.EffectsEnabled?"ON":"OFF"),game.audioDirector.ToggleEffects);
            Button(581,684,438,51,"RESET TOP SCORE",game.ClearHigh);
            Text(500,768,600,40,"TOP-"+game.HighScore.ToString("000000"),28,paper,true,TextAnchor.MiddleCenter,true);
        }
        private void Loading()
        {
            Box(0,0,1600,900,Color.black);
            Text(400,245,800,80,"WORLD 1-"+game.session.nextWorld,40,paper,true,TextAnchor.MiddleCenter,true);
            Text(400,357,800,50,"MARIO",30,paper,true,TextAnchor.MiddleCenter,true);
            if(game.loading)
                Text(400,500,800,50,"LOADING...",27,paper,true,TextAnchor.MiddleCenter,true);
            else
            {
                Button(574,490,452,57,"START",game.EnterWorld);
                Button(574,572,452,57,"MAIN MENU",game.ToMenu);
            }
        }
        private void Hud()
        {
            string[] values={"MARIO\n"+(game.world>0?displayedScore:0).ToString("000000"),
                "\n"+"o x"+(game.world>0?game.Run.coins:0).ToString("00"),"WORLD\n1-"+Mathf.Max(game.world,1),
                "TIME\n"+(game.world>0?Mathf.Max(0,400-Mathf.FloorToInt(game.Run.playTime)).ToString("000"):"400")};
            for(int i=0;i<values.Length;i++)Text(120+i*360,32,300,90,values[i],29,paper,true,TextAnchor.UpperLeft,true);
        }
        private void Shop()
        {
            CommerceHeader("UPGRADE SHOP");
            Gradient(176,184,1238,63,new Color(.13f,.29f,.45f),new Color(.065f,.14f,.24f));
            Text(202,194,1160,46,"WHY PLAY FAIR WHEN YOU CAN PLAY PREMIUM?",31,gold,true);
            float footer=178;
            if(game.world==2&&game.Run.Owns(Product.FireFlower))
            {Button(footer,721,257,42,"REFUND FIRE FLOWER",game.Refund,false,true,true);footer+=263;}
            if(game.world==2&&(!game.Run.trialClaimed||game.Run.trialPending))
            {Button(footer,721,279,42,"FREE 8-SECOND VIP TRIAL",game.StartTrial,false,true,true);}
            string[] icons={"small-jump","flower","mushroom","question","wings","solid"};
            string[] badges={"BREAK THE FLAT-EARTH MONOPOLY","BECOME THE SUN","NATURE NOW WORKS FOR YOU","OMNISCIENCE, NOW ON SALE","THE PALE KING'S INHERITANCE","PURCHASE YOUR INEVITABLE VICTORY"};
            for(int i=0;i<game.assets.products.Length;i++)
            {
                var product=game.assets.products[i];float x=176+i%3*420,y=262+i/3*226;
                Box(x,y,396,216,line);Gradient(x+1,y+1,394,214,new Color(.12f,.22f,.34f),panel);
                Text(x+12,y+6,372,21,badges[(int)product.product],12,gold,true,TextAnchor.MiddleCenter);
                if(product.product==Product.Gatling)
                {
                    Box(x+21,y+41,48,23,muted);Box(x+30,y+64,12,9,muted);
                    for(int barrel=0;barrel<3;barrel++)Box(x+29,y+44+7*barrel,47,3,paper);
                }
                else SpriteImage(icons[(int)product.product],x+24,y+31,40,40,product.product==Product.Jump);
                Text(x+82,y+28,296,46,product.title,24,paper,true,TextAnchor.MiddleLeft);
                Text(x+18,y+79,360,87,product.description,16,muted);
                bool owns=game.Run.Owns(product.product),canBuy=!owns&&game.Run.coins>=product.price;
                Button(x+16,y+173,364,33,owns?"OWNED":product.price+" COINS",()=>game.Buy(product.product),canBuy,canBuy);
            }
            Text(181,774,805,25,"Collect 1 coin per pickup. Upgrades last this run. All payments are simulated.",14,muted);
            Button(1014,725,400,56,"BACK TO THE GAME",game.CloseOverlay,true);
        }
        private void CommerceHeader(string title)
        {
            Veil();Box(150,92,1320,740,new Color(0,0,0,.4f));Box(139,79,1322,742,gold);
            Gradient(140,80,1320,740,new Color(.09f,.18f,.29f),ink);
            Box(160,100,1280,1,line);Box(160,799,1280,1,line);
            Box(176,109,306,58,line);Gradient(177,110,304,56,new Color(.09f,.2f,.31f),panel);
            SpriteImage("coin",194,122,23,32);
            string balance=game.Run.coins.ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
            Text(230,113,178,49,balance,game.Run.coins<1000000?29:22,gold,true,TextAnchor.MiddleLeft);
            Button(428,109,54,58,"+",game.OpenRecharge,true,!game.rechargeOpen);
            Text(526,111,488,50,title,29,paper,true,TextAnchor.MiddleCenter);
            if(game.rechargeOpen)
            {
                Text(1086,110,263,24,"AD CASH",13,muted,true,TextAnchor.MiddleRight);
                Text(1086,134,263,38,Money(game.Run.wallet),26,gold,true,TextAnchor.MiddleRight);
            }
            Button(1391,106,42,42,"X",game.CloseOverlay,false,true,true);
        }
        private void CoinPile(float center,float baseline,int pack)
        {
            int tiers=pack+2;
            Box(center-96,baseline-4,192,5,new Color(0,0,0,.18f));
            for(int row=0;row<tiers;row++)
                for(int coin=0;coin<tiers-row;coin++)
                {
                    float x=center-(tiers-row)*22+coin*44;
                    SpriteImage("coin",x,baseline-52-row*24,37,52);
                }
        }
        private void Recharge()
        {
            CommerceHeader("COIN TOP-UP");
            Gradient(176,190,1238,126,new Color(.11f,.32f,.52f),new Color(.065f,.15f,.26f));
            Box(176,190,1238,3,cyan);
            Text(204,211,1010,49,"FUND YOUR INEVITABLE GREATNESS.",35,gold,true);
            Text(206,266,980,30,"Convert ordinary ad cash into destiny. Bigger fortunes. Smaller laws of physics.",18,paper);
            SpriteImage("small-jump",1263,211,76,76,true);
            SpriteImage("coin",1360,215,26,37);
            string[] badges={"A TASTE OF DESTINY","PATRON OF GREATNESS / SAVE 2%","ROYAL TREASURY / SAVE 6%"};
            string[] savings={"A royal beginning. An unfair tomorrow.","Save $0.20. Invest in your legend.","Save $1.20. Finance your coronation."};
            for(int pack=0;pack<3;pack++)
            {
                int index=pack;float x=176+pack*420,y=334;
                Color accent=pack==2?gold:cyan;
                Box(x+4,y+6,396,352,new Color(0,0,0,.25f));Box(x,y,396,352,accent);
                Gradient(x+1,y+1,394,350,new Color(.10f,.28f,.46f),new Color(.04f,.10f,.18f));
                Box(x+1,y+1,394,34,pack==2?gold:new Color(.08f,.20f,.34f));
                Text(x+14,y+5,368,26,badges[pack],15,pack==2?ink:gold,true,TextAnchor.MiddleCenter);
                string amount=RunModel.PackCoins(pack).ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
                Text(x+18,y+48,360,56,amount+" COINS",34,paper,true,TextAnchor.MiddleCenter);
                Text(x+18,y+105,360,28,savings[pack],17,cyan,false,TextAnchor.MiddleCenter);
                CoinPile(x+198,y+259,pack);
                bool canBuy=game.Run.wallet>=RunModel.PackCost(pack);
                Button(x+22,y+277,352,54,Money(RunModel.PackCost(pack))+" AD CASH",()=>game.ExchangeCash(index),canBuy,canBuy);
            }
            bool canEarn=game.Run.wallet<RunModel.WalletLimit;
            Button(176,725,390,48,canEarn?"WATCH & EARN $1 / SEC":"CASH BALANCE FULL",()=>game.StartAd(AdKind.Cash),true,canEarn);
            Text(590,734,390,42,"Cash limit $99. Coins arrive instantly.",17,muted,false,TextAnchor.MiddleLeft);
            Button(1014,725,400,48,"BACK TO UPGRADES",game.CloseOverlay);
            Text(181,778,1020,26,"Ad cash only. No real payments. Every map coin also adds 1 spendable coin.",12,muted);
        }
        private void Advertisement()
        {
            bool revive=game.adKind==AdKind.Revive;
            int campaign=(int)game.adCampaign;
            Veil();Window(180,105,1240,690,AdBrands[campaign]+(revive?" / 2-SECOND REVIVE":" / WATCH & EARN"),AdHeadlines[campaign]);
            Text(214,240,1140,30,AdCopy[campaign],17,muted);
            if(game.adCampaign==AdCampaign.Mario)HeroArt(215,276,600,473,!revive);
            else TechnologyAdArt(game.adCampaign,215,276,600,473);
            Text(855,286,494,31,revive?"DEATH IS A NEGOTIABLE INCONVENIENCE":"YOUR ATTENTION MINTS DESTINY.",17,gold,true);
            Text(852,330,497,77,revive?"BACK TO YOUR\nCHECKPOINT":"+"+Money(game.adEarned),revive?29:52,paper,true);
            Text(855,419,494,51,revive?"Your legend refuses to end here.\nResurrection only. No cash reward.":"BALANCE  "+Money(game.Run.wallet)+" / $99.00\n+$1 EVERY FULL SECOND",17,muted);
            if(revive)
            {
                RewardTile(855,488,152,"small-idle","WATCH 2 SECONDS","REVIVE");
                Text(1030,496,319,58,"RECLAIM YOUR\nRIGHTFUL GLORY",21,gold,true);
                Text(1030,561,319,39,"Returning in "+Mathf.Max(1,Mathf.CeilToInt(SceneRoot.ReviveAdDuration-game.adTime))+"...",23,paper);
            }
            else
            {
                int seconds=Mathf.FloorToInt(game.adTime),first=seconds/5*5;
                for(int i=0;i<5;i++)RewardTile(855+i*100,488,94,"coin","SEC "+(first+i+1),seconds>first+i?"CLAIMED":"+$1.00",seconds>first+i);
            }
            float progress=revive?game.adTime/SceneRoot.ReviveAdDuration:game.adTime-Mathf.Floor(game.adTime);
            Box(855,620,494,5,line);Box(855,620,494*Mathf.Clamp01(progress),5,gold);
            Text(855,640,494,40,revive?"Your game resumes when the ad finishes.":"Next $1 in "+(1-progress).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"s. Stop any time; full seconds count.",16,muted);
            if(revive)Text(855,695,494,49,"PREPARING YOUR COMEBACK...",18,gold,true,TextAnchor.MiddleCenter);
            else
            {
                Button(855,695,494,49,"STOP & KEEP CASH",game.FinishAd,true);
                Button(1351,130,42,42,"X",game.FinishAd,false,true,true);
            }
        }
        private void Death()
        {
            Veil();Window(180,105,1240,690,"A SPECIAL OFFER FOR A VERY SPECIAL DEFEAT",game.Run.Owns(Product.Jump)?"You fell. Your benefits didn't.":"That jump would have cost 199 coins.");
            HeroArt(215,276,360,473);
            Text(624,283,720,31,"AN EXCLUSIVE INVITATION TO TRANSCEND MORTALITY",17,gold,true);
            Text(624,332,720,58,game.deathReason,22,paper);
            RewardTile(624,409,166,"small-jump","JUMP DLC","199 COINS");
            RewardTile(806,409,166,"flower","FIRE FLOWER","299 COINS");
            Text(1007,414,333,30,"THE NEXT STAGE OF EVOLUTION",16,gold,true);
            Text(1007,455,333,67,"Transcend limits.\nEmbrace greatness.",25,paper,true);
            Button(624,554,350,62,"REVIVE - 2 SECOND AD",()=>game.StartAd(AdKind.Revive),true);
            Button(994,554,354,62,game.Run.wallet<RunModel.WalletLimit?"EARN CASH - $1 / SEC":"BALANCE FULL - $99",()=>game.StartAd(AdKind.Cash),false,game.Run.wallet<RunModel.WalletLimit);
            Button(624,634,350,51,"FREE CHECKPOINT RETRY",game.Retry,false,true,true);
            Button(994,634,354,51,"VIEW ALL UPGRADES",game.OpenShop,false,true,true);
            Box(624,704,724,1,line);
            Text(624,721,300,27,"SCORE "+game.Run.score.ToString("000000")+"   DEATHS "+game.Run.deaths,15,muted);
            Button(972,712,190,42,"RESTART RUN",game.NewGame,false,true,true);
            Button(1170,712,178,42,"MAIN MENU",game.ToMenu,false,true,true);
            Button(1351,130,42,42,"X",game.Retry,false,true,true);
            Text(430,823,740,28,"In-game offers only. No real payments. Free retry is always available.",15,muted,false,TextAnchor.MiddleCenter);
        }
        private void Pause()
        {
            Veil();Box(460,168,680,590,Color.black);
            Text(500,212,600,60,"PAUSE",40,paper,true,TextAnchor.MiddleCenter,true);
            Button(530,323,540,53,"RESUME",game.CloseOverlay);
            Button(530,394,540,53,"MUSIC: "+(game.audioDirector.MusicEnabled?"ON":"OFF"),game.audioDirector.ToggleMusic);
            Button(530,465,540,53,"SOUND: "+(game.audioDirector.EffectsEnabled?"ON":"OFF"),game.audioDirector.ToggleEffects);
            Button(530,536,540,53,"RESTART RUN",game.NewGame);
            Button(530,607,540,53,"MAIN MENU",game.ToMenu);
            Text(485,705,630,32,"A/D MOVE   SPACE JUMP   J FIRE   ESC PAUSE",18,paper,false,TextAnchor.MiddleCenter);
        }
        private void ReceiptMetric(float x,string label,string value,string detail,Color accent)
        {
            Box(x,296,368,206,line);Gradient(x+1,297,366,204,new Color(.10f,.21f,.33f),panel);
            Box(x+1,297,366,3,accent);
            Text(x+18,315,332,30,label,18,accent,true,TextAnchor.MiddleCenter);
            Text(x+18,351,332,76,value,value.Length>9?43:55,paper,true,TextAnchor.MiddleCenter);
            Text(x+18,441,332,50,detail,17,muted,false,TextAnchor.UpperCenter);
        }
        private void Results()
        {
            var run=game.Run;var culture=System.Globalization.CultureInfo.InvariantCulture;
            string Count(int n)=>n.ToString("N0",culture);
            string Seconds(float n)=>n.ToString("0.0",culture)+" s";
            int spent=run.PaidTotal(),refunded=run.RefundedTotal();
            Veil();Window(180,80,1240,744,game.world==1?"WORLD 1-1 / YOUR RUN SO FAR":"WORLD 1-2 / FINAL RECEIPT",
                game.world==1?"Level cleared. Receipt enclosed.":"VICTORY, ITEMIZED.");
            Text(216,245,1168,36,spent>0?"You bought the advantage. The receipt remembers.":"No upgrades purchased. The receipt has nothing to hide.",23,gold);
            ReceiptMetric(216,"ADS WATCHED",Seconds(run.adWatchTime),run.ads+" rewarded ads.\nThank you for your attention.",gold);
            ReceiptMetric(616,"COINS SPENT",Count(spent),"Refunded: "+Count(refunded)+"  |  Net: "+Count(spent-refunded)+"\nReal money paid: $0.00",gold);
            ReceiptMetric(1016,"YOU ACTUALLY PLAYED",Seconds(run.activeInputTime),"Move / jump / fire held.\nIdle, menus and ads excluded.",cyan);
            float tracked=run.adWatchTime+run.playTime,denominator=Mathf.Max(.001f,tracked);
            float adShare=run.adWatchTime/denominator,inputShare=Mathf.Min(run.activeInputTime,run.playTime)/denominator;
            float idleShare=Mathf.Max(0,run.playTime-run.activeInputTime)/denominator;
            Text(216,518,1168,26,"WHERE YOUR TRACKED TIME WENT",15,muted,true);
            Box(216,551,1168,12,line);Box(216,551,1168*adShare,12,gold);
            Box(216+1168*adShare,551,1168*inputShare,12,cyan);
            Text(216,577,368,29,(adShare*100).ToString("0",culture)+"% WATCHING ADS",18,gold,true,TextAnchor.MiddleCenter);
            Text(616,577,368,29,(inputShare*100).ToString("0",culture)+"% USING CONTROLS",18,cyan,true,TextAnchor.MiddleCenter);
            Text(1016,577,368,29,(idleShare*100).ToString("0",culture)+"% IDLE IN LEVEL",18,muted,true,TextAnchor.MiddleCenter);
            Text(216,626,1168,35,"SCORE  "+Count(run.score)+"     BEST  "+Count(game.HighScore)+"     DEATHS  "+run.deaths,23,paper,true,TextAnchor.MiddleCenter);
            Text(216,668,1168,30,game.world==1?"Next: World 1-2. Clear bonus: up to $4.00. Your purchases follow you.":
                spent>0?"Congratulations. Your purchasing power has defeated the game.":"No purchases. No premium rescue. This victory belongs to you.",20,gold,false,TextAnchor.MiddleCenter);
            if(game.world==1)Button(216,718,1168,55,"NEXT WORLD",game.NextWorld,true);
            else
            {
                Button(216,718,568,55,"PLAY AGAIN",game.NewGame,true);
                Button(816,718,568,55,"MAIN MENU",game.ToMenu);
            }
            Text(216,782,1168,25,"This entire run, including failed attempts. Tracked time = ads + in-level time; shopping and menus excluded.",12,muted,false,TextAnchor.MiddleCenter);
        }
    }
}
