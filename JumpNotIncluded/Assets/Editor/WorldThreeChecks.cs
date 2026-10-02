using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace JumpNotIncluded.EditorTools
{
    // Gameplay assertions use live physics and Input System events. Only fixture setup is staged.
    // The final mech run is the entire authored map, with no pre-cleared enemies or hazards.
    [InitializeOnLoad]
    public static class WorldThreeChecks
    {
        const string Pending="jni.world3.checks";
        static IEnumerator steps;
        static Keyboard keyboard;
        static InputSettings originalInput,testInput;
        static double deadline;
        static int lastFrame,assertions;
        static Action<string,int> defeatObserver;
        static Action<int> scoreObserver;
        static GameEvents observedEvents;
        static readonly HashSet<string> renderedText=new HashSet<string>();
        static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
        static string Output=>Path.GetFullPath("../work/world-three-checks.txt");
        static string Previews=>Path.GetFullPath("../artifacts/world-three-previews");

        static WorldThreeChecks()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))Begin();
                if(state==PlayModeStateChange.ExitingPlayMode&&steps!=null)Finish("Play Mode stopped before completion.");
            };
        }
        [MenuItem("Tools/Jump Not Included/Run World Three checks and captures")]
        public static void Start()
        {
            if(steps!=null)return;
            ProjectSetup.Setup();ProjectSetup.Validate();
            SessionState.SetBool(Pending+".background",Application.runInBackground);
            SessionState.SetBool(Pending+".hadHigh",PlayerPrefs.HasKey("jni.highscore"));
            SessionState.SetInt(Pending+".high",PlayerPrefs.GetInt("jni.highscore",0));
            SessionState.SetString(Pending+".run",JsonUtility.ToJson(Resources.Load<RunState>("RunState")));
            SessionState.SetBool(Pending,true);
            Directory.CreateDirectory(Path.GetDirectoryName(Output));Directory.CreateDirectory(Previews);
            File.WriteAllText(Output,"WORLD THREE / live Play Mode physics and Input System\n");
            File.WriteAllText(Path.Combine(Previews,"README.md"),
                "# World Three verification captures\n\nActual Unity Play Mode camera and UI renders at 1280x720 and 960x540. Overview captures use staged camera positions. The final mech traversal uses the intact authored map and real D input only; no J or Space, no deleted hazards. Targeted contact/weapon checks use explicitly named isolated fixtures. This is not proof of a human expert or TAS route.\n");
            if(EditorApplication.isPlaying)Begin();
            else{EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
        }
        static void Begin()
        {
            if(steps!=null)return;
            assertions=0;renderedText.Clear();Application.runInBackground=true;
            originalInput=InputSystem.settings;testInput=UnityEngine.Object.Instantiate(originalInput);
            testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=testInput;keyboard=InputSystem.AddDevice<Keyboard>("JNI World Three validation");
            GameUI.ObserveText+=ObserveText;
            steps=Run();deadline=EditorApplication.timeSinceStartup+240;lastFrame=-1;EditorApplication.update+=Tick;
        }
        static double Wait(float seconds=.15f)=>Time.realtimeSinceStartupAsDouble+seconds;
        static void Tick()
        {
            if(steps==null)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("World Three validation timed out.");
                if(lastFrame==Time.frameCount){EditorApplication.QueuePlayerLoopUpdate();return;}
                lastFrame=Time.frameCount;
                if(steps.Current is double until&&Time.realtimeSinceStartupAsDouble<until)return;
                if(!steps.MoveNext())Finish(null);
            }
            catch(Exception error){Finish(error.ToString());}
        }
        static void Check(bool ok,string label)
        {if(!ok)throw new Exception(label);assertions++;File.AppendAllText(Output,"PASS: "+label+"\n");}
        static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        static T[] All<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        static void Place(PlayerMotor player,Vector2 position,Vector2 velocity,float gravity=0)
        {player.body.position=position;player.body.linearVelocity=velocity;player.body.gravityScale=gravity;Physics2D.SyncTransforms();}
        static void ObserveText(Rect rect,string value,GUIStyle style)
        {if(Event.current.type==EventType.Repaint&&!string.IsNullOrEmpty(value))renderedText.Add(value);}
        static void DetachObservers()
        {
            if(observedEvents!=null)
            {if(defeatObserver!=null)observedEvents.EnemyDefeated-=defeatObserver;if(scoreObserver!=null)observedEvents.ScoreChanged-=scoreObserver;}
            defeatObserver=null;scoreObserver=null;observedEvents=null;
        }
        static void Finish(string error)
        {
            if(steps==null)return;
            steps=null;EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);
            DetachObservers();GameUI.ObserveText-=ObserveText;
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            if(originalInput!=null)InputSystem.settings=originalInput;
            if(testInput!=null)UnityEngine.Object.DestroyImmediate(testInput);
            Application.runInBackground=SessionState.GetBool(Pending+".background",false);
            if(SessionState.GetBool(Pending+".hadHigh",false))PlayerPrefs.SetInt("jni.highscore",SessionState.GetInt(Pending+".high",0));
            else PlayerPrefs.DeleteKey("jni.highscore");
            PlayerPrefs.Save();
            string run=SessionState.GetString(Pending+".run","");
            if(run.Length>0)JsonUtility.FromJsonOverwrite(run,Resources.Load<RunState>("RunState"));
            Time.timeScale=1;
            File.AppendAllText(Output,error==null?"ALL "+assertions+" WORLD THREE CHECKS PASSED\n":"FAIL: "+error+"\n");
            if(error==null)Debug.Log("WORLD THREE CHECKS PASSED: "+assertions);else Debug.LogError(error);
            if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);else EditorApplication.isPlaying=false;
        }
        static IEnumerator FreshThird(params Product[] products)
        {
            DetachObservers();Keys();var run=Resources.Load<RunState>("RunState");run.NewRun();
            foreach(var product in products)run.data.owned.Add(product);
            if(run.data.Owns(Product.Mech))run.data.mechDeployed=true;
            if(run.data.Owns(Product.FireFlower))run.carryForm=Form.Fire;
            SceneManager.LoadScene("World03");yield return Wait(.25f);
        }
        static IEnumerator Run()
        {
            Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World02");yield return Wait(.25f);
            var g=Game;g.Run.owned.Add(Product.Jump);g.Run.coins=123;g.Run.adWatchTime=8.5f;
            g.CompleteWorld();
            Check(g.world==2&&g.mode==ScreenMode.Results&&g.session.nextWorld==3,"World Two completion queues World Three instead of ending the run");
            renderedText.Clear();CapturePair("01-world-two-results");
            Check(renderedText.Contains("NEXT WORLD"),"World Two receipt renders a NEXT WORLD action");
            g.NextWorld();yield return Wait(.2f);g=Game;
            Check(g.mode==ScreenMode.Loading&&g.session.nextWorld==3,"Next World enters the real Loading scene");
            g.EnterWorld();double until=Time.realtimeSinceStartupAsDouble+8;
            while((Game==null||Game.world!=3)&&Time.realtimeSinceStartupAsDouble<until)yield return Wait(.05f);
            yield return Wait(.15f);g=Game;
            Check(g.world==3&&g.Playing&&g.level.IsCastle&&g.level.length>=150,"Loading resolves World03 and builds the full castle");
            Check(g.Run.Owns(Product.Jump)&&g.Run.coins==123&&g.Run.adWatchTime==8.5f,"World Three retains purchases, coins and ad telemetry from World Two");
            Check(Array.Exists(EditorBuildSettings.scenes,s=>s.enabled&&s.path.EndsWith("/World03.unity")),"World03 is present and enabled in the player build");
            Check(g.input.move.enabled&&g.input.jump.enabled&&g.input.fire.enabled&&g.input.actions.FindActionMap("UI").enabled,"World Three uses the real Gameplay and UI Input System maps");
            Check(g.audioDirector.music.outputAudioMixerGroup==g.assets.musicGroup&&g.audioDirector.world.outputAudioMixerGroup==g.assets.worldGroup&&g.audioDirector.ui.outputAudioMixerGroup==g.assets.uiGroup,"Music, world effects and UI use three separate AudioMixer groups");
            g.assets.mixer.GetFloat("ShopLowpass",out float cutoff);
            Check(cutoff>20000,"Active play restores the music low-pass cutoff to 22 kHz");
            var intro=Overview();while(intro.MoveNext())yield return intro.Current;

            // Check the authored entrance before using isolated mechanics fixtures.
            var seal=Array.Find(All<CastleBarrier>(),b=>b.id=="w3.seal.entry");
            Check(seal!=null&&seal.GetComponent<Collider2D>().bounds.max.y>7,"The real entrance furnace seal exceeds a single-jump apex");
            Place(g.player,new Vector2(13,.55f),Vector2.zero,3.2f);Keys();int entryDeaths=g.Run.deaths;
            yield return Wait(.8f);
            Check(g.Playing&&g.Run.deaths==entryDeaths&&g.player.forms.Value==Form.Small,"Waiting before the furnace without contact is safe");
            Keys(Key.D,Key.Space);until=Time.realtimeSinceStartupAsDouble+4;
            while(g.Playing&&Time.realtimeSinceStartupAsDouble<until)yield return Wait(.05f);
            Keys();
            Check(g.mode==ScreenMode.Dead&&g.Run.deaths==entryDeaths+1&&!seal.Destroyed&&seal.Health==3&&g.player.body.position.x<16,
                "Jump-only Mario remains blocked and sustained furnace contact causes a normal death instead of a progression dead end");
            CapturePair("03-jump-only-seal");
            g.OpenShop();yield return Wait(.3f);g=Game;
            Check(g.world==3&&g.mode==ScreenMode.Shop&&g.ReviveRequired&&g.Run.Owns(Product.Jump),
                "Death at the entrance opens the real World Three upgrade shop while preserving Jump and the revival requirement");
            renderedText.Clear();CapturePair("03b-entrance-upgrade-shop");
            Check(renderedText.Contains("UPGRADE SHOP"),"The blocked Jump-only player can see the actual purchase menu");

            var routine=GroundingFixtures();while(routine.MoveNext())yield return routine.Current;
            routine=BarrierFixtures();while(routine.MoveNext())yield return routine.Current;
            routine=ScoreAndWings();while(routine.MoveNext())yield return routine.Current;
            routine=GatlingFixtures();while(routine.MoveNext())yield return routine.Current;
            routine=CheckpointAndAudio();while(routine.MoveNext())yield return routine.Current;
            routine=MechSafety();while(routine.MoveNext())yield return routine.Current;
            routine=MechFullTraversal();while(routine.MoveNext())yield return routine.Current;
        }
        static IEnumerator Overview()
        {
            var g=Game;Time.timeScale=0;
            float[] positions={11,35,69,101,134};
            for(int i=0;i<positions.Length;i++)
            {
                g.cameraView.transform.position=new Vector3(positions[i],4.7f,-10);
                CapturePair("02-castle-overview-"+(i+1));yield return Wait(.03f);
            }
            g.cameraView.transform.position=new Vector3(11,4.7f,-10);Time.timeScale=1;
        }
        static IEnumerator BarrierFixtures()
        {
            var fresh=FreshThird(Product.Jump,Product.FireFlower);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;p.enabled=false;
            // Above the map so enemies, map bounds and pickups cannot influence contact assertions.
            var barrier=new GameObject("CHECK fixture furnace seal").AddComponent<CastleBarrier>();
            barrier.Init(g,"check.castle.furnace",new Vector2(5,20),new Vector2(2,6),true);
            Place(p,new Vector2(5,15.5f),new Vector2(0,12));yield return Wait(.2f);
            Check(barrier!=null&&!barrier.Destroyed&&p.box.bounds.max.y<=17.04f,"A real upward head collision cannot break a furnace seal even in Fire form");
            Place(p,new Vector2(5,25),new Vector2(0,-12));yield return Wait(.2f);
            Check(barrier!=null&&!barrier.Destroyed&&p.box.bounds.min.y>=22.96f,"A real downward stomp cannot break a furnace seal");
            int score=g.Run.score;
            for(int shot=1;shot<=3;shot++)
            {
                Place(p,new Vector2(2,20),Vector2.zero);p.facing=1;
                var projectile=new GameObject("CHECK actual fireball "+shot).AddComponent<Fireball>();projectile.Init(g,p);
                yield return Wait(.24f);
                if(shot<3)Check(barrier!=null&&!barrier.Destroyed&&barrier.Health==3-shot,"Furnace seal loses exactly one health from actual fireball contact "+shot);
            }
            Check(barrier==null||barrier.Destroyed,"Exactly three real projectile collisions open the furnace seal");
            Check(g.Run.score==score+200&&g.Run.collected.Contains("check.castle.furnace.destroyed"),"Broken seal adds exactly 200 points and is persisted in the run state");
            var restored=new GameObject("CHECK already-cleared seal").AddComponent<CastleBarrier>();
            restored.Init(g,"check.castle.furnace",new Vector2(5,20),new Vector2(2,6),true);yield return Wait(.05f);
            Check(restored==null||restored.Destroyed,"A cleared seal cannot reappear after the map is rebuilt");
        }
        static IEnumerator GroundingFixtures()
        {
            var fresh=FreshThird(Product.Jump);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;
            var wall=new GameObject("CHECK unheated wall-climb regression").AddComponent<CastleBarrier>();
            wall.Init(g,"check.castle.wallclimb",new Vector2(8,4),new Vector2(1.5f,10),false);
            Place(p,new Vector2(6,.55f),Vector2.zero,3.2f);yield return Wait(.12f);
            float peakFeet=0;bool elevatedGround=false;
            for(int attempt=0;attempt<3;attempt++)
            {
                Keys(Key.D,Key.Space);double until=Time.realtimeSinceStartupAsDouble+.3f;
                while(Time.realtimeSinceStartupAsDouble<until)
                {
                    peakFeet=Mathf.Max(peakFeet,p.box.bounds.min.y);
                    if(p.box.bounds.min.y>1&&p.grounded)elevatedGround=true;
                    yield return Wait(.01f);
                }
                Keys(Key.D);until=Time.realtimeSinceStartupAsDouble+.06f;
                while(Time.realtimeSinceStartupAsDouble<until)
                {
                    peakFeet=Mathf.Max(peakFeet,p.box.bounds.min.y);
                    if(p.box.bounds.min.y>1&&p.grounded)elevatedGround=true;
                    yield return Wait(.01f);
                }
            }
            Keys();
            Check(g.Playing&&peakFeet>2&&peakFeet<4.8f&&!elevatedGround&&p.body.position.x<7.3f&&!wall.Destroyed,
                "Three real Space presses against an unheated ten-unit wall cannot report side contact as ground or ratchet Jump-only Mario above a single-jump apex (peak feet="+peakFeet+")");

            fresh=FreshThird(Product.DoubleJump);while(fresh.MoveNext())yield return fresh.Current;
            g=Game;p=g.player;
            var ledge=new GameObject("CHECK one-way underside grounding").AddComponent<CastlePlatform>();
            ledge.Init(g,"check.castle.oneway",new Vector2(5,4.6f),2,true);
            Place(p,new Vector2(5,3),new Vector2(0,8));p.airJumpUsed=true;
            yield return Wait(.03f); // Discard the previous floor's cached grounded value after staging the ascent.
            bool falseGround=false,refreshedBelow=false;double passUntil=Time.realtimeSinceStartupAsDouble+1;
            while(p.box.bounds.min.y<4.85f&&Time.realtimeSinceStartupAsDouble<passUntil)
            {
                if(p.box.bounds.min.y<4.55f)
                {falseGround|=p.grounded;refreshedBelow|=!p.airJumpUsed;}
                yield return Wait(.01f);
            }
            Check(p.box.bounds.min.y>=4.85f&&!falseGround&&!refreshedBelow&&p.airJumpUsed,
                "Rising through a real one-way platform never counts its underside or interior as ground and never restores spent Wings");
            Place(p,new Vector2(5,6),new Vector2(0,-4),3.2f);yield return Wait(.35f);
            Check(g.Playing&&p.grounded&&!p.airJumpUsed&&Mathf.Abs(p.box.bounds.min.y-4.6f)<.05f,
                "Landing on the same one-way platform from above still grounds Mario and restores Wings");
        }
        static IEnumerator ScoreAndWings()
        {
            var fresh=FreshThird(Product.DoubleJump);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;
            int defeats=0,scores=0;
            observedEvents=g.events;defeatObserver=(id,points)=>{if(id=="check.castle.stomp")defeats++;};scoreObserver=value=>scores++;
            observedEvents.EnemyDefeated+=defeatObserver;observedEvents.ScoreChanged+=scoreObserver;
            var target=new GameObject("CHECK Lab Three stomp").AddComponent<EnemyActor>();target.Init(g,"check.castle.stomp",5);target.enabled=false;
            p.enabled=false;int before=g.Run.score;
            Place(p,new Vector2(3,4),new Vector2(8,0));yield return Wait(.45f);
            Check(!target.dead&&g.Run.score==before&&defeats==0&&scores==0,"Crossing above an enemy without contact awards no score or enemy event");
            p.airJumpUsed=true;Place(p,new Vector2(5,2.1f),new Vector2(0,-6));yield return Wait(.2f);
            Check(target.dead&&g.Run.score==before+200&&defeats==1&&scores==1,"Actual head stomp emits one EnemyDefeated event and one +200 score update");
            Check(!p.airJumpUsed&&p.body.linearVelocity.y>0,"An actual stomp replenishes spent Monarch Wings and bounces Mario");
            target.Defeat();Check(g.Run.score==before+200&&defeats==1&&scores==1,"Repeated defeat requests cannot duplicate the event or score");
            p.enabled=true;Keys(Key.Space);yield return Wait(.1f);Keys();
            Check(p.airJumpUsed&&p.body.linearVelocity.y>10&&p.wings.enabled,"Real Space input after a stomp uses the restored air jump and Wings animation");
            DetachObservers();
        }
        static IEnumerator GatlingFixtures()
        {
            var fresh=FreshThird(Product.Jump,Product.Gatling);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;p.enabled=false;Place(p,new Vector2(2,20),Vector2.zero);p.facing=1;
            var platform=new GameObject("CHECK permanent terrain");platform.layer=8;platform.transform.position=new Vector2(6,18);
            platform.AddComponent<BoxCollider2D>().size=new Vector2(2,1);
            var brick=new GameObject("CHECK bullet brick").AddComponent<BlockActor>();brick.Init(g,"check.castle.brick",new Vector2(4,20),false,false,ItemKind.Coin);
            var hazard=new GameObject("CHECK bullet crusher").AddComponent<CastleHazard>();
            hazard.Init(g,"check.castle.crusher",CastleHazardKind.Crusher,new Vector2(7,20),new Vector2(1.5f,2),0);
            var enemy=new GameObject("CHECK bullet Goomba").AddComponent<EnemyActor>();enemy.Init(g,"check.castle.bullet.enemy",9);enemy.enabled=false;
            enemy.GetComponent<Rigidbody2D>().position=new Vector2(9,19.5f);
            Physics2D.SyncTransforms();
            Collider2D pipe=Physics2D.OverlapPoint(new Vector2(82,1),1<<8);
            Check(pipe!=null&&Physics2D.OverlapPoint(new Vector2(25,-.1f),1<<8)==null,"Authored World Three pipe is solid and its first lava gap has no floor");
            new GameObject("CHECK actual Gatling bullet").AddComponent<Fireball>().Init(g,p,true);yield return Wait(.25f);
            Check(brick==null&&enemy.dead&&(hazard==null||hazard.Destroyed),"A real Gatling shot removes brickwork, an enemy and a crusher in its forward lane");
            Check(platform!=null&&platform.GetComponent<Collider2D>().enabled&&pipe!=null&&pipe.enabled&&Physics2D.OverlapPoint(new Vector2(25,-.1f),1<<8)==null,"Gatling leaves permanent platforms, pipes and the authored gap intact");
            Check(Array.Exists(All<CastleHazard>(),h=>!h.Destructible&&!h.Destroyed),"Gatling leaves the castle's non-destructible lava hazards in place");
        }
        static IEnumerator CheckpointAndAudio()
        {
            var fresh=FreshThird(Product.Jump,Product.FireFlower);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;
            // Reach each actual safe deck; the normal Update checkpoint rule must capture it.
            foreach(float x in new[]{51f,116f})
            {
                Place(p,new Vector2(x,1.02f),Vector2.zero,3.2f);yield return Wait(.2f);
                Check(g.session.checkpointPosition.x==x&&g.session.checkpointPosition.y>.95f,"The castle's safe checkpoint at x="+x+" records the tall form above its deck");
            }
            g.KillPlayer("CHECK third-world checkpoint reload");g.StartAd(AdKind.Revive);g.hasFocus=true;yield return Wait(.08f);
            g.assets.mixer.GetFloat("ShopLowpass",out float lowpass);
            Check(lowpass==850,"Rewarded ad activates the AudioMixer 850 Hz music low-pass effect");
            g.AdvanceAd(2);yield return Wait(.3f);g=Game;p=g.player;
            Check(g.world==3&&g.Playing&&Mathf.Abs(p.body.position.x-116)<.1f&&p.box.bounds.min.y>=-.02f,"Two-second revival reloads World03 at its safe checkpoint rather than World01");
            Check(g.Run.deaths==1&&g.Run.Owns(Product.FireFlower)&&p.forms.Value==Form.Fire,"Third-world retry preserves death telemetry, purchases and the checkpoint form");
            g.assets.mixer.GetFloat("ShopLowpass",out lowpass);Check(lowpass>20000,"Returning from the ad restores the full-bandwidth music mixer cutoff");
            CapturePair("04-third-world-retry");
            p.forms.Set((int)Form.Small,true);p.RefreshEquipment();g.SetMode(ScreenMode.Shop);
            int coins=g.Run.coins,wallet=g.Run.wallet,receipts=g.Run.receipts.Count;
            Check(g.RestorePurchasedFireForm()&&p.forms.Value==Form.Fire&&g.session.checkpointForm==Form.Fire&&g.Run.coins==coins&&g.Run.wallet==wallet&&g.Run.receipts.Count==receipts,
                "Previously purchased Fire Flower can restore lost fire form in the castle shop without charging twice");
            Check(!g.RestorePurchasedFireForm(),"Restoring an already active fire form is an idempotent no-op");g.CloseOverlay();
            var positions=new Dictionary<Transform,Vector3>();
            var cycles=new Dictionary<CastleHazard,float>();
            foreach(var h in All<CastleHazard>())
            {cycles[h]=h.CycleTime;foreach(var t in h.GetComponentsInChildren<Transform>())positions[t]=t.position;}
            g.SetMode(ScreenMode.Pause);Vector2 before=p.body.position;float time=g.Run.playTime;
            Keys(Key.D,Key.Space,Key.J);yield return Wait(.3f);Keys();bool frozen=true;
            foreach(var pair in positions)if(pair.Key!=null&&pair.Key.position!=pair.Value)frozen=false;
            foreach(var pair in cycles)if(pair.Key!=null&&pair.Key.CycleTime!=pair.Value)frozen=false;
            Check(frozen&&p.body.position==before&&g.Run.playTime==time,"Pause freezes castle hazard geometry, player movement and tracked gameplay time");
            CapturePair("05-third-world-pause");g.CloseOverlay();
        }
        static IEnumerator MechSafety()
        {
            var fresh=FreshThird(Product.Mech);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;int deaths=g.Run.deaths;
            Check(p.MechActive&&p.Protected,"Owned deployed mech equips in World Three with immunity");
            foreach(float x in new[]{25f,60f,90f,128f})
            {
                Place(p,new Vector2(x,-7),new Vector2(0,-70));Keys(Key.S);yield return Wait(.12f);Keys();
                Check(g.Playing&&g.Run.deaths==deaths&&p.body.position.y>=MechSuit.MinAltitude-.05f,"Extreme descent at lava gap x="+x+" restores mech hover without a death");
            }
            p.Hit(true,"CHECK poison");g.KillPlayer("CHECK direct lava/kill-plane route");yield return Wait(.08f);
            Check(g.Playing&&g.Run.deaths==deaths&&p.MechActive,"Poison damage and the central kill path cannot kill an equipped mech");
            var hazard=Array.Find(All<CastleHazard>(),h=>h.Kind==CastleHazardKind.FireBar&&!h.Destroyed);
            Check(hazard!=null,"Authored castle contains destructible hazards for mech contact");
            Place(p,hazard.transform.position,new Vector2(0,-60));yield return Wait(.15f);
            Check(g.Playing&&g.Run.deaths==deaths&&(hazard==null||hazard.Destroyed),"Entering a live authored crusher or fire hazard with the mech destroys it without damage");
        }
        static IEnumerator MechFullTraversal()
        {
            var fresh=FreshThird(Product.Mech);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;int deaths=g.Run.deaths;
            string[] seals=Array.ConvertAll(Array.FindAll(All<CastleBarrier>(),b=>b.id.Contains("seal")),b=>b.id);
            int hazards=All<CastleHazard>().Length;int enemies=All<EnemyActor>().Length+All<BossActor>().Length;
            Check(hazards>10&&enemies>=9&&seals.Length>=3,"D-only traversal starts with the authored hazards, enemy army and all three furnace seals intact");
            int shots=p.mech.ShotsFired;Keys(Key.D);double until=Time.realtimeSinceStartupAsDouble+35;
            bool midCaptured=false,endCaptured=false;
            while(g.Playing&&Time.realtimeSinceStartupAsDouble<until)
            {
                if(!midCaptured&&p.body.position.x>65){CapturePair("06-mech-smash-midpoint");midCaptured=true;}
                if(!endCaptured&&p.body.position.x>136){CapturePair("07-mech-smash-final-gauntlet");endCaptured=true;}
                yield return Wait(.1f);
            }
            Keys();
            Check(g.world==3&&g.mode==ScreenMode.Results&&p.body.position.x>=g.level.length-4,"Holding only D traverses the entire intact World Three and reaches its final receipt (x="+p.body.position.x+", mode="+g.mode+")");
            Check(g.Run.deaths==deaths&&p.mech.ShotsFired==shots,"Full-map mech traversal records zero deaths and fires no laser or Gatling shot");
            Check(Array.TrueForAll(seals,id=>g.Run.collected.Contains(id+".destroyed")),"Mech contact physically destroys every blocking furnace seal on the authored route");
            Check(Physics2D.OverlapPoint(new Vector2(82,1),1<<8)==null&&Physics2D.OverlapPoint(new Vector2(120,1),1<<8)==null,"Forward mech impact also smashes both authored pipe obstacles");
            renderedText.Clear();CapturePair("08-final-world-three-receipt");
            Check(renderedText.Contains("PLAY AGAIN")&&!renderedText.Contains("NEXT WORLD"),"World Three final receipt provides replay rather than a nonexistent fourth world");
            float hands=g.Run.activeInputTime,play=g.Run.playTime;yield return Wait(.2f);
            Check(g.Run.activeInputTime==hands&&g.Run.playTime==play&&hands>10,"Final receipt freezes telemetry after actual full-map input");
        }
        static void CapturePair(string name)
        {Capture(name,1280,720);Capture(name+"-small",960,540);}
        static void Capture(string name,int width,int height)
        {
            var flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.FlattenHierarchy;
            var render=typeof(EditorGUIUtility).GetMethod("RenderPlayModeViewCamerasInternal",flags);
            if(render==null)throw new MissingMethodException("Unity offscreen Game View renderer was not found.");
            Type viewType=null;
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {viewType=assembly.GetType("UnityEditor.PlayModeView");if(viewType!=null)break;}
            if(viewType==null)throw new MissingMemberException("Unity PlayModeView type is missing.");
            var view=viewType.GetMethod("GetMainPlayModeView",flags).Invoke(null,null);
            if(view==null)
            {
                view=ScriptableObject.CreateInstance(viewType.Assembly.GetType("UnityEditor.GameView"));
                var parent=typeof(EditorWindow).GetField("m_Parent",BindingFlags.Instance|BindingFlags.NonPublic);
                parent.SetValue(view,ScriptableObject.CreateInstance(parent.FieldType));
            }
            viewType.GetProperty("targetSize",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,new Vector2(width,height));
            Game.cameraView.pixelRect=new Rect(0,0,width,height);Game.cameraView.aspect=(float)width/height;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
            RenderTexture previous=RenderTexture.active,readback=null;Texture2D image=null;
            try
            {
                Event.current=new Event{type=EventType.Repaint};
                render.Invoke(null,new object[]{target,0,new Vector2(-100,-100),false,true});
                readback=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
                Graphics.Blit(target,readback,new Vector2(1,-1),new Vector2(0,1));RenderTexture.active=readback;
                image=new Texture2D(width,height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(Previews,name+".png"),image.EncodeToPNG());
                File.AppendAllText(Output,"CAPTURED: "+name+" ("+width+"x"+height+")\n");
            }
            finally
            {
                RenderTexture.active=previous;if(image!=null)UnityEngine.Object.DestroyImmediate(image);
                if(readback!=null)RenderTexture.ReleaseTemporary(readback);target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
