using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace JumpNotIncluded.EditorTools
{
    // One integration check, using the real scenes, physics, Input System and audio sources.
    [InitializeOnLoad]
    public static class RuntimeChecks
    {
        private static IEnumerator sequence;
        private static Keyboard keyboard;
        private static int lastFrame,oldHigh;
        private static bool hadHigh,oldBackground;
        private static InputSettings testInputSettings,originalInputSettings;
        private static readonly List<string> passed=new List<string>();
        private static double deadline;
        private static string Output=>Path.GetFullPath("../work/unity-runtime-checks.txt");
        private static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
        static RuntimeChecks()
        {
            EditorApplication.playModeStateChanged+=OnPlayMode;
        }
        [MenuItem("Tools/Jump Not Included/Run runtime checks")]
        public static void Start()
        {
            if(sequence!=null)return;
            ProjectSetup.Setup();ProjectSetup.Validate();
            hadHigh=PlayerPrefs.HasKey("jni.highscore");oldHigh=PlayerPrefs.GetInt("jni.highscore");
            oldBackground=Application.runInBackground;Application.runInBackground=true;
            SessionState.SetBool("jni.checks.pending",true);SessionState.SetBool("jni.checks.hadHigh",hadHigh);
            SessionState.SetInt("jni.checks.high",oldHigh);SessionState.SetBool("jni.checks.background",oldBackground);
            passed.Clear();Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,"RUNNING\n");
            if(EditorApplication.isPlaying)Begin();
            else
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
                EditorApplication.isPlaying=true;
            }
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("jni.checks.pending",false))Begin();
            if(state==PlayModeStateChange.ExitingPlayMode&&sequence!=null)Finish("Play mode stopped before completion.");
        }
        private static void Begin()
        {
            if(sequence!=null)return;
            hadHigh=SessionState.GetBool("jni.checks.hadHigh",false);oldHigh=SessionState.GetInt("jni.checks.high",0);
            oldBackground=SessionState.GetBool("jni.checks.background",false);Application.runInBackground=true;
            originalInputSettings=InputSystem.settings;
            testInputSettings=UnityEngine.Object.Instantiate(originalInputSettings);
            testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=testInputSettings;
            keyboard=InputSystem.AddDevice<Keyboard>("JNI regression keyboard");
            sequence=CheckGame();deadline=EditorApplication.timeSinceStartup+180;lastFrame=-1;
            EditorApplication.update+=Tick;
        }
        private static void Tick()
        {
            if(sequence==null)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Runtime check timed out.");
                if(lastFrame==Time.frameCount){EditorApplication.QueuePlayerLoopUpdate();return;}
                lastFrame=Time.frameCount;
                if(sequence.Current is double until&&Time.realtimeSinceStartupAsDouble<until)return;
                if(!sequence.MoveNext())Finish(null);
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private static void Check(bool condition,string name)
        {if(!condition)throw new Exception(name);passed.Add("PASS: "+name);File.AppendAllText(Output,passed[passed.Count-1]+"\n");}
        private static double Wait(float seconds)=>Time.realtimeSinceStartupAsDouble+seconds;
        private static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        private static void Place(PlayerMotor p,float x,float y,Vector2 velocity,float gravity=0)
        {p.body.position=new Vector2(x,y);p.body.linearVelocity=velocity;p.body.gravityScale=gravity;Physics2D.SyncTransforms();}
        private static void Capture(string file)
        {if(Application.isBatchMode)return;Directory.CreateDirectory("../artifacts");ScreenCapture.CaptureScreenshot(Path.GetFullPath("../artifacts/"+file));}
        private static IEnumerator CheckGame()
        {
            Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");yield return Wait(.5f);
            var g=Game;var p=g.player;
            Check(g.Playing&&g.Run.deaths==0,"fresh world starts without commerce");
            g.OpenShop();g.StartAd(AdKind.Cash);g.StartAd(AdKind.Revive);Check(g.Playing&&!g.Buy(Product.Jump),"commerce cannot open during ordinary play");
            Check(g.assets.revision>=7&&g.assets.Clip("error").length<.3f,"current assets retain the short denied-jump cue");
            Check(g.assets.sutdAIAd!=null&&g.assets.sutdRobotAd!=null&&g.assets.slCheaterAd!=null,"official SUTD and SL Cheater artwork loads from bundled textures");
            Check(Array.TrueForAll(UnityEngine.Object.FindObjectsByType<PickupActor>(FindObjectsSortMode.None),item=>item.kind!=ItemKind.Flower),"World 1 has no map flowers");
            foreach(var product in g.assets.products)
                Check(Array.TrueForAll((product.title+product.description).ToCharArray(),c=>c<128),product.product+" product text is English after asset migration");
            Check(p.forms.Value==Form.Small&&p.buffs.Value==Buff.None,"idle state machines remain stable after asset reload");
            var enemy=UnityEngine.Object.FindFirstObjectByType<EnemyActor>();
            Check(Mathf.Abs(enemy.GetComponent<SpriteRenderer>().bounds.min.y)<.01f&&Mathf.Abs(enemy.GetComponent<Collider2D>().bounds.min.y)<.01f,
                "Goomba sprite and collider feet align with floor");
            Check(g.assets.Sprite("goomba0").rect.y==241&&g.assets.Sprite("goomba1").rect.x==30,"Goomba frames include all 16 pixels, including feet");
            Check(g.Run.activeInputTime==0&&g.Run.adWatchTime==0&&g.Run.playTime>0,"idle gameplay accrues level time without inventing ad or control time");
            float x=p.body.position.x;Keys(Key.D);yield return Wait(.3f);
            Check(p.facing==1&&p.visual.flipX&&p.body.position.x>x+.5f,"right movement faces right (dx="+(p.body.position.x-x)+", input="+g.input.move.ReadValue<float>()+", mode="+g.mode+")");
            Check(g.Run.activeInputTime>.2f&&g.Run.activeInputTime<=g.Run.playTime,"real movement input accrues hands-on time once per frame");
            Keys(Key.A);yield return Wait(.2f);
            Check(p.facing==-1&&!p.visual.flipX,"left movement faces left");Keys();yield return Wait(.1f);
            float hands=g.Run.activeInputTime,levelTime=g.Run.playTime;
            g.SetMode(ScreenMode.Pause);Keys(Key.D,Key.Space,Key.J);yield return Wait(.2f);
            Check(g.Run.activeInputTime==hands&&g.Run.playTime==levelTime&&g.Run.adWatchTime==0,"holding gameplay keys in a pause menu adds no tracked time");
            Keys();g.CloseOverlay();g.hasFocus=false;Keys(Key.D);yield return Wait(.15f);
            Check(g.Run.activeInputTime==hands,"unfocused gameplay input does not count as hands-on time");
            Keys();g.hasFocus=true;yield return Wait(.1f);
            var audio=g.audioDirector;var clip=audio.music.clip;int sample=audio.music.timeSamples;
            float floorY=p.body.position.y;
            Keys(Key.Space);yield return Wait(.12f);Keys();yield return Wait(.15f);
            Keys(Key.Space);yield return Wait(.12f);Keys();yield return Wait(.15f);
            Check(Mathf.Abs(p.body.position.y-floorY)<.05f,"unowned jump remains locked");
            Check(audio.music.clip==clip&&audio.music.isPlaying&&audio.music.timeSamples>sample+clip.frequency*.2f,
                "repeated denied jumps do not restart or replace BGM");
            Check(audio.music!=audio.world&&audio.world!=audio.ui&&audio.music.outputAudioMixerGroup!=audio.world.outputAudioMixerGroup&&audio.ui.outputAudioMixerGroup!=audio.world.outputAudioMixerGroup,
                "music, world effects and UI route through separate mixer groups");
            float effectsVolume=audio.world.volume;audio.ToggleMusic();audio.ToggleMusic();
            Check(audio.world.volume==effectsVolume,"music toggle leaves sound effect volume unchanged");
            Capture("revised-gameplay.png");yield return Wait(.15f);

            var block=new GameObject("Regression question block").AddComponent<BlockActor>();
            block.Init(g,"regression.block",new Vector2(5,3.5f),true,true,ItemKind.Coin);
            p.enabled=false;Place(p,3.5f,3.5f,new Vector2(8,0));yield return Wait(.25f);
            Check(!block.spent&&p.box.bounds.max.x<=4.54f,"block side is solid and cannot award a coin");
            Place(p,5,5.2f,new Vector2(0,-3),3.2f);yield return Wait(.4f);
            Check(!block.spent&&Mathf.Abs(p.box.bounds.min.y-4)<.05f,"player lands on block top without activating it");
            int score=g.Run.score;Place(p,5,2.2f,new Vector2(0,10));yield return Wait(.13f);
            Check(block.spent&&g.Run.score==score+100&&g.Run.coins==1,"underside head hit awards one coin");
            Check(block.transform.position==new Vector3(5,3.5f,0)&&block.GetComponent<BoxCollider2D>().size==Vector2.one,
                "bump animation leaves the one-tile collision body stationary");
            Place(p,5,2.2f,new Vector2(0,10));yield return Wait(.15f);
            Check(g.Run.score==score+100,"spent block cannot award twice");
            var brick=new GameObject("Regression brick").AddComponent<BlockActor>();
            brick.Init(g,"regression.brick",new Vector2(8,3.5f),false,false,ItemKind.Coin);
            Place(p,8,2.2f,new Vector2(0,10));yield return Wait(.15f);
            Check(brick!=null&&!brick.spent,"small Mario bumps an empty brick without consuming it");
            p.forms.Set((int)Form.Super,true);p.enabled=true;Place(p,8,.98f,Vector2.zero);yield return Wait(.1f);
            p.enabled=false;Place(p,8,1.8f,new Vector2(0,10));yield return Wait(.18f);
            Check(brick==null&&g.Run.collected.Contains("regression.brick"),"big Mario breaks an empty brick from underneath (form="+p.forms.Value+", y="+p.body.position.y+", height="+p.box.size.y+")");

            Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");yield return Wait(.4f);
            g=Game;g.KillPlayer("A Goomba has ended your free trial.");yield return Wait(.15f);
            Check(g.mode==ScreenMode.Dead&&Time.timeScale==0,"death freezes gameplay and reveals the offer");Capture("revised-death-offer.png");yield return Wait(.15f);
            g.StartAd(AdKind.Revive);g.FinishAd();
            var reviveCampaign=g.adCampaign;
            Check(Enum.IsDefined(typeof(AdCampaign),reviveCampaign),"revive ad starts with a valid randomized creative");
            Check(g.mode==ScreenMode.Ad&&g.Run.wallet==0,"revive ad cannot finish early or award cash");
            g.hasFocus=false;g.AdvanceAd(5);
            Check(g.adTime==0&&g.mode==ScreenMode.Ad,"ads stop counting while the application is unfocused");
            Check(g.Run.adWatchTime==0,"unfocused ad time is excluded from the receipt");
            g.hasFocus=true;g.AdvanceAd(1.99f);
            Check(g.mode==ScreenMode.Ad&&g.adCampaign==reviveCampaign,"revive ad keeps one creative until two full seconds");
            g.AdvanceAd(.01f);yield return Wait(.3f);g=Game;
            Check(g.Playing&&g.Run.wallet==0&&g.Run.deaths==1&&g.Run.ads==1,"two-second ad revives at the checkpoint without a cash reward");
            Check(Mathf.Abs(g.Run.adWatchTime-2)<.001f,"revive viewing seconds survive the checkpoint scene reload");

            g.KillPlayer("Time for a commercial break.");g.StartAd(AdKind.Cash);g.hasFocus=true;
            var cashCampaign=g.adCampaign;
            Check(cashCampaign!=reviveCampaign,"next ad avoids the revive creative across scene reload");
            g.AdvanceAd(.999f);Check(g.Run.wallet==0,"cash ad pays nothing before one full second");
            g.AdvanceAd(.001f);Check(g.Run.wallet==100,"cash ad awards exactly one dollar at one full second");
            g.AdvanceAd(1.4f);g.FinishAd();yield return Wait(.3f);g=Game;
            Check(g.mode==ScreenMode.Shop&&g.Run.wallet==200&&g.Run.deaths==2&&g.Run.ads==2,"closing a 2.4-second cash ad preserves two dollars and opens the death shop");
            g.FinishAd();g.StartAd(AdKind.Revive);
            Check(g.mode==ScreenMode.Shop&&g.Run.wallet==200,"duplicate completion cannot pay again and revive ads require a death");
            g.StartAd(AdKind.Cash);
            Check(g.adCampaign!=reviveCampaign&&g.adCampaign!=cashCampaign,"first three ad openings cover all three campaigns without repetition");
            g.hasFocus=true;g.AdvanceAd(.5f);g.FinishAd();
            Check(g.Run.wallet==200&&g.Run.ads==2,"closing a short repeat ad does not pay for an unfinished second");
            Check(Mathf.Abs(g.Run.adWatchTime-4.9f)<.001f,"receipt includes fractional cash-ad viewing even when no reward was earned");
            Check(!g.Buy(Product.Jump)&&g.ExchangeCash(0)&&g.ExchangeCash(0)&&g.Buy(Product.Jump)&&g.Run.wallet==0&&g.Run.coins==1,"cash ads fund coin packs, then jump costs 199 coins with one coin left");
            Capture("revised-shop.png");yield return Wait(.15f);
            g.CloseOverlay();Keys(Key.Space);yield return Wait(.2f);Keys();
            Check(g.Playing&&g.player.body.position.y>1,"purchased jump works through the actual Input System");
            hands=g.Run.activeInputTime;
            g.KillPlayer("Checkpoint retry check.");g.Retry();yield return Wait(.3f);g=Game;
            Check(g.Run.Owns(Product.Jump)&&g.Run.wallet==0&&g.Run.coins==1&&g.Run.ads==2,"free retry preserves purchases, both balances and ad history without duplicating rewards");
            Check(Mathf.Abs(g.Run.adWatchTime-4.9f)<.001f&&Mathf.Abs(g.Run.activeInputTime-hands)<.05f&&hands>.1f,"failed attempts retain both viewing and control time after retry");
            g.KillPlayer("Another chance to earn.");g.StartAd(AdKind.Cash);g.hasFocus=true;g.AdvanceAd(3.4f);g.FinishAd();yield return Wait(.3f);g=Game;
            for(int pack=0;pack<3;pack++)g.ExchangeCash(0);
            Check(g.Buy(Product.FireFlower)&&g.Run.wallet==0&&g.Run.coins==2,"repeat cash ad converts into coins for instant Fire Flower");
            Check(g.player.forms.Value==Form.Fire&&g.player.Big&&g.player.box.size.y>1.8f&&g.session.checkpointForm==Form.Fire&&g.session.checkpointPosition.y>.95f,"flower purchase transforms immediately in the paused shop and saves a tall checkpoint");
            g.CloseOverlay();yield return Wait(.1f);
            Keys(Key.J);yield return Wait(.1f);Keys();
            Check(UnityEngine.Object.FindObjectsByType<Fireball>(FindObjectsSortMode.None).Length>0,"fire Mario shoots in play mode");
            g.KillPlayer("Instant flower checkpoint check.");g.Retry();yield return Wait(.3f);g=Game;
            Check(g.player.forms.Value==Form.Fire&&g.player.box.bounds.min.y>=-.01f,"retry restores the purchased fire form above the floor without a pickup");
            g.CompleteWorld();SceneManager.LoadScene("World02");yield return Wait(.3f);g=Game;
            Check(Array.TrueForAll(UnityEngine.Object.FindObjectsByType<PickupActor>(FindObjectsSortMode.None),item=>item.kind!=ItemKind.Flower),"World 2 has no map flowers");
            Check(g.Run.wallet==1000&&g.Run.coins==2&&g.Run.Owns(Product.FireFlower),"second world carries both balances and purchases with its one-time subsidy");
            g.KillPlayer("Refund check.");g.OpenShop();yield return Wait(.3f);g=Game;g.Refund();
            Check(g.Run.wallet==1000&&g.Run.coins==301&&!g.Run.Owns(Product.FireFlower)&&g.player.forms.Value!=Form.Fire,"death shop refund revokes fire upgrade and returns the exact coin price");
            Check(g.ExchangeCash(1)&&g.Buy(Product.Purify)&&g.Run.wallet==20&&g.Run.coins==2,"discount coin pack completes the refund economy route");
            Check(Array.TrueForAll(UnityEngine.Object.FindObjectsByType<PickupActor>(FindObjectsSortMode.None),item=>!item.IsPoison),"Mushroom ID makes every existing poison mushroom safe");

            g.StartAd(AdKind.Cash);g.hasFocus=true;yield return Wait(1.15f);
            Check(g.mode==ScreenMode.Ad&&Time.timeScale==0&&g.Run.wallet==120,"cash ad uses actual unscaled time while gameplay is paused");
            g.FinishAd();g.Run.Credit(9779);g.session.Capture(g.session.checkpointPosition,g.session.checkpointForm);
            float watched=g.Run.adWatchTime;g.StartAd(AdKind.Cash);g.hasFocus=true;g.AdvanceAd(30);
            Check(g.mode==ScreenMode.Shop&&g.Run.wallet==9900&&g.adEarned==1,"cash ad clips the last reward to the cap and stops automatically at 99 dollars");
            Check(Mathf.Abs(g.Run.adWatchTime-watched-1)<.001f,"cash cap counts only the visible second before the ad auto-closes");
            g.StartAd(AdKind.Cash);Check(g.mode==ScreenMode.Shop&&g.Run.wallet==9900,"full wallet cannot start another cash ad");
            for(int pack=0;pack<3;pack++)g.ExchangeCash(0);
            Check(g.Buy(Product.FireFlower)&&g.Run.wallet==9600&&g.Run.coins==3,"coin exchange creates room under the cash cap and funds a repurchase");
            g.StartAd(AdKind.Cash);g.hasFocus=true;g.AdvanceAd(1);g.FinishAd();
            Check(g.Run.wallet==9700&&g.mode==ScreenMode.Shop,"cash ads can be repeated after spending");
            g.CloseOverlay();g.KillPlayer("Capped balance retry check.");g.Retry();yield return Wait(.3f);g=Game;
            Check(g.Run.wallet==9700&&g.Run.coins==3,"checkpoint retry preserves both balances after reaching the cap");
            g.KillPlayer("VIP trial check.");g.OpenShop();yield return Wait(.3f);g=Game;
            g.StartTrial();Check(g.Playing&&g.player.buffs.Value==Buff.VIP,"VIP trial returns safely to play without a terminal");
            g.player.buffs.Tick(8.01f);Check(g.player.buffs.Value==Buff.None,"serialized VIP timer expires after eight seconds");

            g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;
            g.KillPlayer("Ad rotation check.");g.StartAd(AdKind.Cash);g.hasFocus=true;
            var firstCampaign=g.adCampaign;g.AdvanceAd(4.99f);
            Check(g.adCampaign==firstCampaign&&g.Run.wallet==400,"cash creative stays visible for the first five seconds");
            g.AdvanceAd(.01f);var secondCampaign=g.adCampaign;
            Check(secondCampaign!=firstCampaign&&g.adTime==5&&g.Run.wallet==500&&g.adEarned==500,"five-second rotation preserves the ad timer and all cash earned");
            g.hasFocus=false;g.AdvanceAd(5);
            Check(g.adCampaign==secondCampaign&&g.adTime==5&&g.Run.wallet==500,"unfocused ads pause both creative rotation and rewards");
            g.hasFocus=true;g.AdvanceAd(5);var thirdCampaign=g.adCampaign;
            Check(thirdCampaign!=firstCampaign&&thirdCampaign!=secondCampaign&&g.adTime==10&&g.Run.wallet==1000&&g.adEarned==1000,"ten-second rotation completes the shuffled set without resetting rewards");
            g.FinishAd();yield return Wait(.3f);g=Game;
            g.CloseOverlay();g.KillPlayer("Rotation checkpoint check.");g.Retry();yield return Wait(.3f);g=Game;
            g.KillPlayer("Rotation after retry.");g.StartAd(AdKind.Cash);
            Check(g.adCampaign!=thirdCampaign&&g.Run.wallet==1000&&g.Run.ads==1,"checkpoint retry preserves rotation history and earned cash");

            g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;
            g.Run.coins=299;g.SaveCheckpoint(g.session.checkpointPosition);
            g.KillPlayer("Damage ladder check.");g.OpenShop();yield return Wait(.3f);g=Game;p=g.player;
            Check(g.Buy(Product.FireFlower)&&p.forms.Value==Form.Fire&&!g.Buy(Product.FireFlower)&&g.Run.wallet==0,"one flower payment activates fire immediately and duplicate clicks cannot charge again");
            g.CloseOverlay();yield return Wait(1.1f);
            float feet=p.box.bounds.min.y;int deaths=g.Run.deaths;p.Hit();
            Check(g.Playing&&p.forms.Value==Form.Super&&p.Big&&p.Protected&&Mathf.Abs(p.box.bounds.min.y-feet)<.01f,"first hit changes Fire to Super while preserving feet and granting hurt protection");
            for(int i=0;i<8;i++){p.Hit();p.Hit(true);}
            Check(g.Playing&&p.forms.Value==Form.Super&&g.Run.deaths==deaths,"repeated enemy and poison contacts cannot skip the protected Super form");
            yield return Wait(1.65f);
            Check(p.forms.Value==Form.Super&&p.Big&&!p.Protected&&!g.Run.CanFire(p.forms.Value,p.buffs.Value),"hurt timer expires into Super without regaining fire or shrinking again");
            Place(p,2,1.02f,Vector2.zero,3.2f);feet=p.box.bounds.min.y;p.Hit(true);Physics2D.SyncTransforms();
            Check(g.Playing&&p.forms.Value==Form.Small&&!p.Big&&p.Protected&&Mathf.Abs(p.box.bounds.min.y-feet)<.01f,"poison damage changes Super to Small instead of killing and keeps feet in place (mode="+g.mode+", form="+p.forms.Value+", protected="+p.Protected+", feet="+feet+" -> "+p.box.bounds.min.y+", body="+p.body.position+", transform="+p.transform.position+")");
            p.Hit();p.Hit(true);
            Check(g.Playing&&p.forms.Value==Form.Small&&g.Run.deaths==deaths,"Small Mario is protected against repeated contacts during the shrink grace period");
            yield return Wait(1.65f);p.Hit();
            Check(g.mode==ScreenMode.Dead&&p.forms.Value==Form.Dead&&g.Run.deaths==deaths+1,"only the third unprotected hit kills after Fire to Super to Small");

            g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(1.1f);g=Game;p=g.player;
            p.PowerUp("mushroom");p.enabled=false;
            new GameObject("Damage regression Goomba").AddComponent<EnemyActor>().Init(g,"damage.regression",5,5,5);
            Place(p,5,1.02f,Vector2.zero);yield return Wait(.15f);
            Check(g.Playing&&p.forms.Value==Form.Small&&p.Protected&&g.Run.deaths==0,"actual Goomba side contact shrinks Super Mario once without killing");
            p.enabled=true;
            var premiumChecks=CheckPremium();while(premiumChecks.MoveNext())yield return premiumChecks.Current;
            var paidChecks=CheckPaidVictory();while(paidChecks.MoveNext())yield return paidChecks.Current;
        }
        private static IEnumerator CheckPremium()
        {
            var g=Game;g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;var p=g.player;
            p.enabled=false;
            var hidden=new GameObject("Hidden collision check").AddComponent<BlockActor>();
            hidden.Init(g,"check.hidden",new Vector2(5,3.5f),true,true,ItemKind.Coin,true);
            Check(hidden.GetComponent<BoxCollider2D>().isTrigger&&hidden.gameObject.layer==10&&Array.TrueForAll(hidden.GetComponentsInChildren<SpriteRenderer>(),s=>!s.enabled),"unrevealed blocks are invisible triggers outside the ground layer");
            Place(p,3.5f,3.5f,new Vector2(8,0));yield return Wait(.25f);
            Check(!hidden.revealed&&p.body.position.x>5,"side contact passes through a hidden block without triggering it");
            Place(p,5,5,new Vector2(0,-6));yield return Wait(.3f);
            Check(!hidden.revealed&&p.body.position.y<3.5f,"falling through a hidden block cannot reveal it or fake a landing");
            int coins=g.Run.coins;Place(p,5,2.1f,new Vector2(0,12));yield return Wait(.14f);
            Check(hidden.revealed&&hidden.spent&&!hidden.GetComponent<BoxCollider2D>().isTrigger&&hidden.gameObject.layer==8&&p.body.linearVelocity.y<=0&&g.Run.coins==coins+1,"head contact reveals the hidden block, stops ascent and awards exactly one spendable coin");
            Place(p,5,2.1f,new Vector2(0,12));yield return Wait(.15f);
            Check(g.Run.coins==coins+1,"revealed hidden coin cannot be farmed by hitting it twice");
            var star=new GameObject("Hidden star check").AddComponent<BlockActor>();
            star.Init(g,"check.star",new Vector2(6,3.5f),true,false,ItemKind.Star,true);
            Place(p,6,2.1f,new Vector2(0,12));yield return Wait(.15f);
            var reward=Array.Find(UnityEngine.Object.FindObjectsByType<PickupActor>(FindObjectsSortMode.None),item=>item.id=="check.star.item");
            Check(star.spent&&reward!=null&&reward.kind==ItemKind.Star,"hidden star block releases an actual invincibility pickup");
            Place(p,6,4.55f,Vector2.zero);yield return Wait(.4f);
            Check(p.buffs.Value==Buff.Star&&p.Protected&&g.Run.collected.Contains("check.star.item"),"collecting the hidden star enables real invincibility");

            g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;p=g.player;
            var mapBlocks=UnityEngine.Object.FindObjectsByType<BlockActor>(FindObjectsSortMode.None);
            Check(g.level.GapEnd-g.level.GapStart==6&&Array.FindAll(mapBlocks,b=>b.hidden&&b.transform.position.x<44).Length==0&&Array.FindAll(mapBlocks,b=>b.hidden&&b.transform.position.x>=44&&b.transform.position.x<50).Length==8,"opening has no hidden blocks and the six-tile cliff conceals eight traps");
            foreach(var e in UnityEngine.Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(e.gameObject);
            g.Run.owned.Add(Product.Jump);Place(p,43.5f,.53f,Vector2.zero,3.2f);yield return Wait(.12f);
            var trap=Array.Find(UnityEngine.Object.FindObjectsByType<BlockActor>(FindObjectsSortMode.None),b=>Mathf.Abs(b.transform.position.x-44.5f)<.01f&&b.transform.position.y<3);
            Keys(Key.D,Key.Space);yield return Wait(1.05f);Keys();
            Check(trap.revealed&&g.mode==ScreenMode.Dead,"ordinary cliff-edge jump hits the hidden trap and falls (position="+p.body.position+", mode="+g.mode+")");
            g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;p=g.player;
            foreach(var e in UnityEngine.Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(e.gameObject);
            g.Run.owned.Add(Product.Jump);Place(p,42.8f,.53f,Vector2.zero,3.2f);yield return Wait(.12f);
            Keys(Key.D,Key.Space);yield return Wait(1.3f);Keys();
            Check(g.mode==ScreenMode.Dead&&Array.Exists(UnityEngine.Object.FindObjectsByType<BlockActor>(FindObjectsSortMode.None),b=>b.hidden&&b.revealed&&b.transform.position.y>4),"early first-visit jump also hits the upper ambush and falls into the wider gap");

            g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;
            g.Run.coins=20000;g.Run.Credit(1880);g.SaveCheckpoint(g.session.checkpointPosition);
            g.KillPlayer("Premium purchase checks.");g.OpenShop();yield return Wait(.3f);g=Game;p=g.player;
            Check(g.ExchangeCash(2)&&g.Run.wallet==0&&g.Run.coins==22000,"largest cash pack credits 2000 coins inside the shop");
            Check(g.Buy(Product.MasterGuide)&&g.Buy(Product.DoubleJump)&&g.Buy(Product.Gatling)&&g.Run.coins==11003,"all three premium upgrades purchase through the real death shop");
            Check(g.Run.Owns(Product.Jump)&&!g.Buy(Product.Jump),"buying wings includes jumping and blocks a redundant jump purchase");
            g.CloseOverlay();yield return Wait(.1f);
            hidden=Array.Find(UnityEngine.Object.FindObjectsByType<BlockActor>(FindObjectsSortMode.None),b=>b.hidden&&!b.revealed);
            Check(hidden.GuideVisible&&Array.FindAll(hidden.GetComponentsInChildren<SpriteRenderer>(),s=>s.enabled).Length==5&&!hidden.revealed,"Master Guide reveals four outline edges and a reward icon without activating the block");
            Keys(Key.Space);yield return Wait(.13f);Keys();yield return Wait(.08f);
            float firstHeight=p.body.position.y;Keys(Key.Space);yield return Wait(.1f);
            Check(p.airJumpUsed&&p.body.linearVelocity.y>10&&p.body.position.y>firstHeight&&p.wings.enabled&&p.wings.sprite==g.assets.Sprite("wings"),"second jump restores ascent and displays Monarch Wings through actual input");
            Keys();yield return Wait(.08f);Keys(Key.Space);yield return Wait(.08f);Keys();
            Check(p.airJumpUsed&&p.body.linearVelocity.y<10,"a third press cannot grant another midair jump");
            yield return Wait(.2f); // Let the unused jump buffer expire before the landing fixture.
            Place(p,2,.53f,Vector2.zero,3.2f);yield return Wait(.2f);
            Check(p.grounded&&!p.airJumpUsed,"landing recharges the single air jump");
            Keys(Key.J);yield return Wait(.48f);Keys();
            var shots=UnityEngine.Object.FindObjectsByType<Fireball>(FindObjectsSortMode.None);
            Check(shots.Length>=4&&Array.TrueForAll(shots,s=>s.IsBullet&&Mathf.Abs(s.GetComponent<Rigidbody2D>().linearVelocity.y)<.01f),"holding J fires more than three straight Gatling bullets without Fire Mario");
            foreach(var shot in shots)UnityEngine.Object.Destroy(shot.gameObject);
            p.enabled=false;Place(p,2,3.45f,Vector2.zero);p.facing=1;
            var brick=new GameObject("Bullet brick check").AddComponent<BlockActor>();brick.Init(g,"check.bullet.brick",new Vector2(5,3.5f),false,false,ItemKind.Coin);
            var target=new GameObject("Bullet Goomba check").AddComponent<EnemyActor>();target.Init(g,"check.bullet.enemy",7,7,7);target.enabled=false;target.GetComponent<Rigidbody2D>().position=new Vector2(7,3);
            var bullet=new GameObject("Piercing check").AddComponent<Fireball>();bullet.Init(g,p,true);yield return Wait(.3f);
            Check(brick==null&&g.Run.collected.Contains("check.bullet.brick.broken")&&g.Run.collected.Contains("check.bullet.enemy"),"one Gatling bullet destroys a brick and the enemy behind it");
            if(bullet!=null)UnityEngine.Object.Destroy(bullet.gameObject);yield return Wait(.1f);
            var broken=new GameObject("Broken hidden reward check").AddComponent<BlockActor>();broken.Init(g,"check.broken.reward",new Vector2(6,7),true,true,ItemKind.Coin,true);
            coins=g.Run.coins;broken.BreakByBullet();broken.BreakByBullet();yield return Wait(.1f);
            Check(g.Run.coins==coins+1&&g.Run.collected.Contains("check.broken.reward.broken"),"shooting a hidden reward pays its coin once before breaking it");
            Place(p,2,.55f,Vector2.zero);g.SaveCheckpoint(new Vector2(2,.55f));g.KillPlayer("Premium persistence check.");g.Retry();yield return Wait(.3f);g=Game;p=g.player;
            Check(g.Run.Owns(Product.MasterGuide)&&g.Run.Owns(Product.DoubleJump)&&g.Run.Owns(Product.Gatling)&&g.Run.coins==coins+1,"retry retains all premium abilities and exact coin balance");
            broken=new GameObject("Broken block reload check").AddComponent<BlockActor>();broken.Init(g,"check.broken.reward",new Vector2(6,7),true,true,ItemKind.Coin,true);yield return Wait(.1f);
            Check(broken==null&&g.Run.coins==coins+1,"destroyed reward blocks remain destroyed after a checkpoint reload");

            g.session.NewRun();SceneManager.LoadScene("World02");yield return Wait(.3f);g=Game;p=g.player;
            var bosses=UnityEngine.Object.FindObjectsByType<BossActor>(FindObjectsSortMode.None);
            var boss=Array.Find(bosses,b=>b.id==WorldBuilder.BossKey(0));
            var bossIds=new HashSet<string>();foreach(var b in bosses)bossIds.Add(b.id);
            Check(bosses.Length==10&&bossIds.Count==10&&Array.TrueForAll(bosses,b=>b.health==BossActor.MaxHealth)&&boss.transform.position.x<9&&g.assets.Sprite("bowser0")!=g.assets.solid,"World 2 opens with ten independently identified 18-health Bowsers");
            Check(UnityEngine.Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).Length==29&&
                Array.FindAll(UnityEngine.Object.FindObjectsByType<PickupActor>(FindObjectsSortMode.None),item=>item.IsPoison).Length==31,
                "World 2 contains 29 Goombas and 31 poison mushrooms across its hostile corridors");
            p.enabled=false;Place(p,2,.55f,Vector2.zero);yield return Wait(2.1f);
            var flame=UnityEngine.Object.FindFirstObjectByType<BossFlame>();
            Check(flame!=null&&flame.GetComponent<Rigidbody2D>().linearVelocity.x<0,"Bowser patrols and fires toward the player");
            Vector3 bossBefore=boss.transform.position,flameBefore=flame.transform.position;g.SetMode(ScreenMode.Pause);yield return Wait(.25f);
            Check(boss.transform.position==bossBefore&&flame.transform.position==flameBefore,"pause freezes Bowser and his projectiles");g.CloseOverlay();
            foreach(var f in UnityEngine.Object.FindObjectsByType<BossFlame>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(f.gameObject);
            Place(p,boss.transform.position.x-3,1.1f,Vector2.zero);p.facing=1;
            var fire=new GameObject("Boss fireball check").AddComponent<Fireball>();fire.Init(g,p);yield return Wait(.3f);
            Check(boss!=null&&boss.health<BossActor.MaxHealth&&boss.health>0,"ordinary fireball damages Bowser without instantly killing him");
            g.Run.owned.Add(Product.Gatling);p.enabled=true;Place(p,9,.55f,Vector2.zero,3.2f);Keys(Key.J);yield return Wait(1.1f);Keys();
            Check(boss==null&&g.Run.collected.Contains(WorldBuilder.BossKey(0)),"sustained Gatling fire defeats the front rank of Bowsers");
            var defeated=new HashSet<string>();for(int i=0;i<WorldBuilder.OpeningBossCount;i++)if(g.Run.collected.Contains(WorldBuilder.BossKey(i)))defeated.Add(WorldBuilder.BossKey(i));
            Check(defeated.Count>1&&defeated.Count<WorldBuilder.OpeningBossCount,"Gatling defeats each boss in range without erasing the distant survivors");
            g.SaveCheckpoint(new Vector2(2,.55f));g.KillPlayer("Boss persistence check.");g.Retry();yield return Wait(.3f);g=Game;
            bosses=UnityEngine.Object.FindObjectsByType<BossActor>(FindObjectsSortMode.None);
            Check(bosses.Length==WorldBuilder.OpeningBossCount-defeated.Count&&Array.TrueForAll(bosses,b=>!defeated.Contains(b.id)),"checkpoint retry preserves every boss defeat and leaves the surviving Bowsers alive");
        }
        private static IEnumerator CheckPaidVictory()
        {
            var g=Game;g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;var p=g.player;
            g.Run.owned.Add(Product.Gatling);p.enabled=false;Place(p,43,.55f,Vector2.zero);p.facing=-1;
            var shot=new GameObject("Backward terraforming check").AddComponent<Fireball>();shot.Init(g,p,true);yield return Wait(.1f);
            Check(Physics2D.OverlapPoint(new Vector2(45,-.05f),1<<8)==null&&Array.Exists(UnityEngine.Object.FindObjectsByType<BlockActor>(FindObjectsSortMode.None),b=>b.hidden&&b.transform.position.x==44.5f),"firing backward leaves the cliff and forward traps intact");
            p.facing=1;shot=new GameObject("Forward terraforming check").AddComponent<Fireball>();shot.Init(g,p,true);yield return Wait(.1f);
            bool solid=true;for(int x=44;x<50;x++)solid&=Physics2D.OverlapPoint(new Vector2(x+.5f,-.05f),1<<8)!=null;
            Check(solid&&!Array.Exists(UnityEngine.Object.FindObjectsByType<BlockActor>(FindObjectsSortMode.None),b=>b.hidden&&b.transform.position.x>=44&&b.transform.position.x<50),"forward Gatling fire fills all six gap tiles and destroys both tiers of hidden bricks above the muzzle");
            Check(Physics2D.OverlapPoint(new Vector2(56,1),1<<8)==null&&!Array.Exists(UnityEngine.Object.FindObjectsByType<PickupActor>(FindObjectsSortMode.None),item=>item.IsPoison&&item.transform.position.x<57),"Gatling clears the forward pipe and poisonous ground hazards");
            int coins=g.Run.coins,score=g.Run.score;
            shot=new GameObject("Repeated terraforming check").AddComponent<Fireball>();shot.Init(g,p,true);yield return Wait(.1f);
            Check(g.Run.coins==coins&&g.Run.score==score,"repeated paving cannot farm hidden rewards or destruction scores");
            g.SaveCheckpoint(new Vector2(47,.55f));g.KillPlayer("Paved checkpoint check.");g.Retry();yield return Wait(.4f);g=Game;
            Check(g.Playing&&g.player.grounded&&g.player.body.position.y>.4f&&Physics2D.OverlapPoint(new Vector2(47,-.05f),1<<8)!=null&&Physics2D.OverlapPoint(new Vector2(56,1),1<<8)==null,"a checkpoint inside the former abyss restores paved ground and demolished pipes before the player spawns");

            g.session.NewRun();SceneManager.LoadScene("World01");yield return Wait(.3f);g=Game;
            g.KillPlayer("A premium victory awaits.");g.StartAd(AdKind.Cash);g.hasFocus=true;g.AdvanceAd(57);g.FinishAd();yield return Wait(.3f);g=Game;
            Check(g.ExchangeCash(2)&&g.ExchangeCash(2)&&g.ExchangeCash(2)&&g.Buy(Product.Gatling)&&g.Run.coins==1&&g.Run.wallet==60&&!g.Run.Owns(Product.Jump)&&!g.Run.Owns(Product.Purify),"ad cash funds Gatling alone through three real coin-pack purchases");
            g.CloseOverlay();int deaths=g.Run.deaths;Keys(Key.D,Key.J);
            double until=Time.realtimeSinceStartupAsDouble+25;
            while(g.Playing&&Time.realtimeSinceStartupAsDouble<until)yield return Wait(.2f);
            Keys();
            Check(g.mode==ScreenMode.Results&&g.Run.deaths==deaths&&g.player.body.position.x>91,"paid route clears all of World 1 by holding right and fire, with no jumps or extra deaths (position="+g.player.body.position+", mode="+g.mode+")");
            float hands=g.Run.activeInputTime,watched=g.Run.adWatchTime;
            Check(Mathf.Abs(watched-57)<.001f&&hands>15&&hands<=g.Run.playTime&&g.Run.PaidTotal()==5999,
                "first-world receipt contains actual ad seconds, held-control seconds and the Gatling coin receipt");
            yield return Wait(.2f);
            Check(g.Run.activeInputTime==hands&&g.Run.adWatchTime==watched,"result screen freezes both receipt timers");
            g.NextWorld();yield return Wait(.3f);g=Game;g.EnterWorld();
            until=Time.realtimeSinceStartupAsDouble+5;
            while(Game.world!=2&&Time.realtimeSinceStartupAsDouble<until)yield return Wait(.1f);
            g=Game;Check(g.world==2&&g.Playing&&g.Run.Owns(Product.Gatling),"Gatling carries through the actual loading scene into World 2");
            Check(g.Run.activeInputTime==hands&&g.Run.adWatchTime==watched,"ad and hands-on totals carry through the loading scene without adding menu time");
            Keys(Key.D,Key.J);until=Time.realtimeSinceStartupAsDouble+25;
            while(g.Playing&&Time.realtimeSinceStartupAsDouble<until)yield return Wait(.2f);
            Keys();
            bool armyDefeated=true;for(int i=0;i<WorldBuilder.OpeningBossCount;i++)armyDefeated&=g.Run.collected.Contains(WorldBuilder.BossKey(i));
            Check(g.mode==ScreenMode.Results&&g.Run.deaths==deaths&&armyDefeated&&g.player.body.position.x>107,"paid route clears all ten Bowsers and World 2 by holding right and fire, with no jumps or extra deaths (position="+g.player.body.position+", mode="+g.mode+")");
            Check(g.Run.activeInputTime>hands+17&&g.Run.adWatchTime==watched&&g.Run.PaidTotal()==5999,"final receipt totals both worlds while keeping the original viewing and purchase amounts");
            g.session.NewRun();Check(g.Run.adWatchTime==0&&g.Run.activeInputTime==0&&g.Run.playTime==0&&g.Run.ads==0,"a new run clears all receipt telemetry");
        }
        private static void Finish(string error)
        {
            EditorApplication.update-=Tick;SessionState.SetBool("jni.checks.pending",false);sequence=null;
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            if(originalInputSettings!=null)InputSystem.settings=originalInputSettings;
            if(testInputSettings!=null)UnityEngine.Object.DestroyImmediate(testInputSettings);
            if(hadHigh)PlayerPrefs.SetInt("jni.highscore",oldHigh);else PlayerPrefs.DeleteKey("jni.highscore");PlayerPrefs.Save();
            Application.runInBackground=oldBackground;
            string result=error==null?"ALL RUNTIME CHECKS PASSED":"FAILED: "+error;
            File.AppendAllText(Output,result+"\n");
            if(error==null)Debug.Log(result);else Debug.LogError(result);
            if(Application.isBatchMode){EditorApplication.Exit(error==null?0:1);return;}
            if(EditorApplication.isPlaying)
            {Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("MainMenu");}
        }
    }
}
