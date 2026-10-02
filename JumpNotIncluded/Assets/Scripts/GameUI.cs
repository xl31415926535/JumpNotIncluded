using System;
using UnityEngine;

namespace JumpNotIncluded
{
    public partial class GameUI : MonoBehaviour
    {
        private SceneRoot game;
        private Font font,pixelFont;
        private int focus,buttonIndex,lastCount,displayedScore;
        private bool submit;
        private float navCooldown;
        private GUIStyle textStyle;
#if UNITY_EDITOR
        // Opt-in observer for the isolated presentation capture, stripped from players.
        public static event Action<Rect,string,GUIStyle> ObserveText;
#endif
        private GameEvents channel;
        private readonly Color ink=new Color(.025f,.055f,.10f),panel=new Color(.055f,.105f,.18f),
            muted=new Color(.72f,.78f,.86f),paper=new Color(.98f,.97f,.93f),gold=new Color(1f,.79f,.25f),
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
        {if(channel!=null)channel.ScoreChanged-=OnScoreChanged;if(font!=null)Destroy(font);if(pixelFont!=null)Destroy(pixelFont);if(titleBurst!=null)Destroy(titleBurst);}
        public void ResetFocus(){focus=0;submit=false;paymentFocusPending=true;}
        private void Update()
        {
            if(game.Deploying){submit=false;return;}
            navCooldown-=Time.unscaledDeltaTime;
            float n=game.input.navigate.ReadValue<float>();
            if(Mathf.Abs(n)>.5f&&navCooldown<=0)
            {focus=(focus+(n>0?1:-1)+Mathf.Max(lastCount,1))%Mathf.Max(lastCount,1);navCooldown=.18f;}
            if(game.input.submit.WasPressedThisFrame())submit=true;
        }
        private void OnGUI()
        {
            if(game==null||font==null||game.Deploying)return;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            Vector2 offset=new Vector2((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f);
            GUI.matrix=Matrix4x4.TRS(offset,Quaternion.identity,new Vector3(scale,scale,1));
            textStyle=new GUIStyle(GUI.skin.label){font=font,wordWrap=true,padding=new RectOffset(0,0,0,0)};buttonIndex=0;
            if(game.mode==ScreenMode.Menu)Menu();
            else if(game.mode==ScreenMode.Loading)Loading();
            else
            {
                Hud();
                if(game.mode==ScreenMode.Shop){if(game.rechargeOpen)Recharge();else if(game.mechShowcaseOpen)MechShowcase();else Shop();}
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
        private void Text(float x,float y,float w,float h,string value,int size,Color color,bool bold=false,TextAnchor anchor=TextAnchor.UpperLeft,bool retro=false,bool wrap=true)
        {
            textStyle.font=retro?pixelFont:font;textStyle.fontSize=size;textStyle.normal.textColor=color;
            textStyle.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;textStyle.alignment=anchor;textStyle.wordWrap=wrap;
            GUI.Label(new Rect(x,y,w,h),value,textStyle);
#if UNITY_EDITOR
            ObserveText?.Invoke(new Rect(x,y,w,h),value,textStyle);
#endif
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
                    Gradient(x+1,y+1,w-2,h-2,primary&&enabled?new Color(1,.88f,.43f):fill,
                        primary&&enabled?new Color(1,.64f,.14f):Color.Lerp(fill,ink,.15f));
                    if(primary&&enabled)Box(x+2,y+2,w-4,2,new Color(1,.96f,.71f));
                }
                else if(active)Box(x+14,y+h-3,w-28,1,gold);
                if(focus==index&&enabled){Box(x-5,y+5,3,h-10,cyan);Box(x+5,y-4,w-10,2,cyan);}
            }
            else if(focus==index||hover)Text(x-30,y,w,h,">",28,paper,true,TextAnchor.MiddleLeft,true);
            Text(x+14,y+2,w-28,h-4,label,Classic?25:quiet?17:h<38?18:20,enabled?(!Classic&&primary&&!quiet?ink:paper):muted,true,TextAnchor.MiddleCenter,Classic&&game.mode!=ScreenMode.Pause);
            bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);
            bool selected=submit&&focus==index&&Event.current.type==EventType.Repaint;
            if(enabled&&(clicked||selected)){submit=false;action?.Invoke();}
        }
        private string Money(long cents)=>"S$"+(cents/100m).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
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
        private void Menu()
        {
            Hud();
            Box(473,168,666,263,new Color(.27f,.08f,.035f));Box(461,156,666,263,new Color(.69f,.2f,.07f));
            Text(495,174,595,195,"SUPER\nMARIO BROS.",72,paper,true,TextAnchor.MiddleCenter,true);
            Text(495,370,595,36,"Jump Not Included",24,new Color(1,.83f,.58f),true,TextAnchor.MiddleCenter,true);
            FreeToPlayBadge();
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
                "","WORLD\n1-"+Mathf.Max(game.world,1),
                "TIME\n"+(game.world>0?Mathf.Max(0,400-Mathf.FloorToInt(game.Run.playTime)).ToString("000"):"400")};
            for(int i=0;i<values.Length;i++)Text(120+i*360,32,300,90,values[i],29,paper,true,TextAnchor.UpperLeft,true);
            CoinAmount(480,72,300,42,"x"+(game.world>0?game.Run.coins:0).ToString("00"),29,paper,retro:true);
        }
        private void Shop()
        {
            CommerceHeader("UPGRADE SHOP");
            Gradient(176,184,1238,63,promoRed,promoWine);
            Text(202,194,730,46,"WHY PLAY FAIR? PLAY PREMIUM.",28,gold,true);
            Button(960,194,432,43,"SL CHEATER MECH / S$79.99",game.OpenMechShowcase,true);
            float footer=178;
            if(game.world==2&&game.Run.Owns(Product.FireFlower))
            {Button(footer,721,257,42,"REFUND FIRE FLOWER",game.Refund,false,true,true);footer+=263;}
            if(game.world==2&&(!game.Run.trialClaimed||game.Run.trialPending))
            {Button(footer,721,279,42,"FREE 8-SECOND VIP TRIAL",game.StartTrial,false,true,true);}
            string[] icons={"small-jump","flower","mushroom","question","wings","solid"};
            string[] badges={"BREAK THE FLAT-EARTH MONOPOLY","BECOME THE SUN","NATURE NOW WORKS FOR YOU","OMNISCIENCE, NOW ON SALE","THE PALE KING'S INHERITANCE","TURN RESISTANCE INTO CONFETTI"};
            int card=0;
            for(int i=0;i<game.assets.products.Length;i++)
            {
                var product=game.assets.products[i];if(product.product==Product.Mech)continue;
                float x=176+card%3*420,y=262+card/3*226;card++;
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
                if(owns&&product.product==Product.FireFlower&&game.world==3&&game.player.forms.Value!=Form.Fire)
                    Button(x+16,y+173,364,33,"RESTORE FIRE FORM / FREE",()=>game.RestorePurchasedFireForm(),true);
                else if(owns)Button(x+16,y+173,364,33,"OWNED",null,false,false);
                else CoinPriceButton(x+16,y+173,364,33,product.price,()=>game.Buy(product.product),canBuy);
            }
            Text(181,774,805,25,"Collect 1 coin per pickup. Upgrades last this run. All payments are simulated.",14,muted);
            Button(1014,725,400,56,game.ReviveRequired?"REVIVE - 2 SECOND AD":"BACK TO THE GAME",()=>
            {if(game.ReviveRequired)game.StartAd(AdKind.Revive);else game.CloseOverlay();},true);
        }
        private void CommerceHeader(string title)
        {
            Veil();Box(150,92,1320,740,new Color(0,0,0,.4f));Box(139,79,1322,742,gold);
            Gradient(140,80,1320,740,new Color(.09f,.18f,.29f),ink);
            Box(160,100,1280,1,line);Box(160,799,1280,1,line);
            Box(176,109,306,58,line);Gradient(177,110,304,56,new Color(.09f,.2f,.31f),panel);
            CoinAmount(194,113,218,49,Coins(game.Run.coins),game.Run.coins<1000000?29:22,gold);
            Button(428,109,54,58,"+",game.OpenRecharge,true,!game.rechargeOpen);
            Text(526,111,488,50,title,29,paper,true,TextAnchor.MiddleCenter);
            if(game.rechargeOpen||game.mechShowcaseOpen)
            {
                Text(1030,110,270,24,"SGD WALLET",13,muted,true,TextAnchor.MiddleRight);
                Text(1030,134,270,38,Money(game.Run.wallet),26,gold,true,TextAnchor.MiddleRight);
            }
            Button(1323,106,110,42,"BACK",game.CloseOverlay,false,true,true);
        }
        private void CoinPile(float center,float baseline,int pack,float scale=1)
        {
            int tiers=pack+2;
            Box(center-96*scale,baseline-4*scale,192*scale,5*scale,new Color(0,0,0,.18f));
            for(int row=0;row<tiers;row++)
                for(int coin=0;coin<tiers-row;coin++)
                {
                    float x=center-((tiers-row)*22-coin*44)*scale;
                    SpriteImage("coin",x,baseline-(52+row*24)*scale,37*scale,52*scale);
                }
        }
        private void Recharge(){PaymentCheckout();}
        private void Advertisement()=>PromotionAd();
        private void Death()=>PromotionDeath();
        private void Pause()
        {
            Veil();Box(460,168,680,590,Color.black);
            Text(500,212,600,60,"PAUSE",40,paper,true,TextAnchor.MiddleCenter,true);
            Button(530,323,540,53,"RESUME",game.CloseOverlay);
            Button(530,394,540,53,"MUSIC: "+(game.audioDirector.MusicEnabled?"ON":"OFF"),game.audioDirector.ToggleMusic);
            Button(530,465,540,53,"SOUND: "+(game.audioDirector.EffectsEnabled?"ON":"OFF"),game.audioDirector.ToggleEffects);
            Button(530,536,540,53,"RESTART RUN",game.NewGame);
            Button(530,607,540,53,"MAIN MENU",game.ToMenu);
            Text(485,697,630,52,game.Run.Owns(Product.Mech)?"A/D MOVE   HOLD SPACE ASCEND   RELEASE HOVER\nS / DOWN DESCEND   J LOCK-ON LASER   ESC PAUSE":"A/D MOVE   SPACE JUMP   J FIRE   ESC PAUSE",18,paper,false,TextAnchor.MiddleCenter);
        }
        private void ReceiptMetric(float x,string label,string value,string detail,Color accent,bool coins=false)
        {
            Box(x,296,368,206,line);Gradient(x+1,297,366,204,new Color(.10f,.21f,.33f),panel);
            Box(x+1,297,366,3,accent);
            Text(x+18,315,332,30,label,18,accent,true,TextAnchor.MiddleCenter);
            if(coins)CoinAmount(x+18,351,332,76,value,value.Length>7?32:49,paper,TextAnchor.MiddleCenter);
            else Text(x+18,351,332,76,value,value.Length>9?43:55,paper,true,TextAnchor.MiddleCenter);
            Text(x+18,441,332,50,detail,17,muted,false,TextAnchor.UpperCenter);
        }
        private void Results()
        {
            var run=game.Run;var culture=System.Globalization.CultureInfo.InvariantCulture;
            string Count(int n)=>n.ToString("N0",culture);
            string Seconds(float n)=>n.ToString("0.0",culture)+" s";
            int spent=run.PaidTotal(),refunded=run.RefundedTotal();bool boughtPower=spent>0||run.WalletPaidTotal()>0;
            bool final=game.world==SceneRoot.LastWorld;
            Veil();Window(180,80,1240,744,"WORLD 1-"+game.world+(final?" / FINAL RECEIPT":" / YOUR RUN SO FAR"),
                final?"VICTORY, ITEMIZED.":"Level cleared. Receipt enclosed.");
            Text(216,245,1168,36,boughtPower?"You bought the advantage. The receipt remembers.":"No upgrades purchased. The receipt has nothing to hide.",23,gold);
            ReceiptMetric(216,"ADS WATCHED",Seconds(run.adWatchTime),run.ads+" rewarded ads.\nThank you for your attention.",gold);
            ReceiptMetric(616,"COINS SPENT",Count(spent),"Refunded: "+Count(refunded)+"  |  Net: "+Count(spent-refunded)+"\nCard: "+Money(run.Payments.Total(PaymentMethod.VirtualCard))+"  |  Mech: "+Money(run.WalletPaidTotal()),gold,true);
            ReceiptMetric(1016,"YOU ACTUALLY PLAYED",Seconds(run.activeInputTime),"Move / jump / fly / fire held.\nIdle, menus and ads excluded.",cyan);
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
            Text(216,668,1168,30,game.world==1?"Next: World 1-2. Clear bonus: up to S$4.00. Your purchases follow you.":game.world==2?"Next: THE FORECLOSURE FURNACE. Your purchases follow you into the fire.":
                boughtPower?"Congratulations. Your purchasing power has defeated the game.":"No purchases. No premium rescue. This victory belongs to you.",20,gold,false,TextAnchor.MiddleCenter);
            if(!final)Button(216,718,1168,55,"NEXT WORLD",game.NextWorld,true);
            else
            {
                Button(216,718,568,55,"PLAY AGAIN",game.NewGame,true);
                Button(816,718,568,55,"MAIN MENU",game.ToMenu);
            }
            Text(216,782,1168,25,"This entire run, including failed attempts. Tracked time = ads + in-level time; shopping and menus excluded.",12,muted,false,TextAnchor.MiddleCenter);
        }
    }
}
