using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace JumpNotIncluded.EditorTools
{
    // Segment evidence, deliberately not a claim of a complete expert/TAS clear.
    // Only loadout, map reload and a grounded pre-flight launch placement are staged.
    // Live PlayerMotor, collisions, Input System and all authored enemies/hazards remain active.
    [InitializeOnLoad]
    public static class WorldThreeRouteProof
    {
        private const string Pending="jni.world3.routeproof";
        private static IEnumerator sequence;
        private static Keyboard keyboard;
        private static InputSettings originalInput,testInput;
        private static double deadline;
        private static int lastFrame,lowerPassed,upperPassed,failed;
        private static bool trialPassed;
        private static string trialDetails;
        private static string trialFailureSignature;
        private static string traceFolder;
        private static float queuedMoveInput;
        private static bool queuedJumpInput;
        private static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
        private static string Output=>Path.GetFullPath("../work/world-three-route-proof.txt");
        private struct Ledge
        {
            public string name;public float left,right,top;
            public Ledge(string name,float left,float right,float top=0)
            {this.name=name;this.left=left;this.right=right;this.top=top;}
            public static Ledge Pillar(string name,float center,float top,float width)=>new Ledge(name,center-width/2,center+width/2,top);
        }
        private struct Segment
        {
            public Ledge from,to;
            public Segment(Ledge from,Ledge to){this.from=from;this.to=to;}
            public override string ToString()=>from.name+" -> "+to.name;
        }
        static WorldThreeRouteProof()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))Begin();
                if(state==PlayModeStateChange.ExitingPlayMode&&sequence!=null)Finish("Play Mode stopped before completion.");
            };
        }
        [MenuItem("Tools/Jump Not Included/Prove World Three route segments")]
        public static void Start()
        {
            if(sequence!=null)return;
            ProjectSetup.Setup();ProjectSetup.Validate();
            SessionState.SetBool(Pending+".background",Application.runInBackground);
            SessionState.SetBool(Pending+".hadHigh",PlayerPrefs.HasKey("jni.highscore"));
            SessionState.SetInt(Pending+".high",PlayerPrefs.GetInt("jni.highscore",0));
            SessionState.SetString(Pending+".run",JsonUtility.ToJson(Resources.Load<RunState>("RunState")));
            SessionState.SetString(Pending+".traces",Path.GetFullPath("../work/world-three-route-proofs/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
            SessionState.SetBool(Pending,true);Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output,"WORLD THREE / live authored-map segment route proofs\n"+
                "LIMIT: These are independent segments with staged grounded launch positions and natural phase retries. They do not prove a single continuous human or TAS clear.\n"+
                "Every attempt reloads the intact authored map. No hazards/enemies are disabled, no invulnerability is added, and PlayerMotor remains enabled. Natural spawn grace expires before launch. Fire form must remain unchanged throughout each successful flight.\n");
            if(EditorApplication.isPlaying)Begin();
            else{EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
        }
        private static void Begin()
        {
            if(sequence!=null)return;
            lowerPassed=upperPassed=failed=0;queuedMoveInput=0;queuedJumpInput=false;Application.runInBackground=true;
            traceFolder=SessionState.GetString(Pending+".traces",Path.GetFullPath("../work/world-three-route-proofs"));
            Directory.CreateDirectory(traceFolder);Log("Successful actual-input traces: "+traceFolder);
            originalInput=InputSystem.settings;testInput=UnityEngine.Object.Instantiate(originalInput);
            testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=testInput;keyboard=InputSystem.AddDevice<Keyboard>("JNI route proof keyboard");
            sequence=Run();lastFrame=-1;deadline=EditorApplication.timeSinceStartup+1800;EditorApplication.update+=Tick;
        }
        private static void Tick()
        {
            if(sequence==null)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Route proof timed out.");
                if(lastFrame==Time.frameCount){EditorApplication.QueuePlayerLoopUpdate();return;}
                lastFrame=Time.frameCount;
                if(sequence.Current is double until&&Time.realtimeSinceStartupAsDouble<until)return;
                if(!sequence.MoveNext())Finish(failed==0?null:failed+" route segment/gate proofs remain unproven; see attempt traces.");
            }
            catch(Exception error){Finish(error.ToString());}
        }
        private static double Wait(float seconds)=>Time.realtimeSinceStartupAsDouble+seconds;
        private static void Log(string text)=>File.AppendAllText(Output,text+"\n");
        private static void Keys(params Key[] values)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(values));
            bool left=false,right=false,jump=false;
            foreach(Key key in values)
            {
                left|=key==Key.A||key==Key.LeftArrow;
                right|=key==Key.D||key==Key.RightArrow;
                jump|=key==Key.Space||key==Key.W;
            }
            queuedMoveInput=(right?1f:0f)-(left?1f:0f);queuedJumpInput=jump;
        }
        private static void Finish(string error)
        {
            if(sequence==null)return;sequence=null;EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            if(originalInput!=null)InputSystem.settings=originalInput;
            if(testInput!=null)UnityEngine.Object.DestroyImmediate(testInput);
            Application.runInBackground=SessionState.GetBool(Pending+".background",false);
            if(SessionState.GetBool(Pending+".hadHigh",false))PlayerPrefs.SetInt("jni.highscore",SessionState.GetInt(Pending+".high",0));
            else PlayerPrefs.DeleteKey("jni.highscore");PlayerPrefs.Save();
            string run=SessionState.GetString(Pending+".run","");
            if(run.Length>0)JsonUtility.FromJsonOverwrite(run,Resources.Load<RunState>("RunState"));
            Time.timeScale=1;
            Log("SUMMARY: lower single-jump segments "+lowerPassed+"/14; upper Wings segments "+upperPassed+"/11. Continuous whole-map non-mech clear: NOT TESTED.");
            Log(error==null?"ALL ROUTE SEGMENT PROOFS PASSED":"INCOMPLETE: "+error);
            if(error==null)Debug.Log("WORLD THREE ROUTE SEGMENT PROOFS PASSED");else Debug.LogError(error);
            if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);else EditorApplication.isPlaying=false;
        }
        private static IEnumerator Fresh(bool wings,float phase)
        {
            Keys();var run=Resources.Load<RunState>("RunState");run.NewRun();
            run.data.owned.Add(Product.Jump);run.data.owned.Add(Product.FireFlower);
            if(wings)run.data.owned.Add(Product.DoubleJump);run.carryForm=Form.Fire;
            SceneManager.LoadScene("World03");yield return Wait(.15f);
            if(Game==null||Game.world!=3)throw new Exception("World03 did not load.");
            Game.hasFocus=true;
            // Do not inherit spawn grace as a way of surviving the hazards under test.
            double ready=Time.timeAsDouble+1.15+phase;
            while(Time.timeAsDouble<ready)yield return null;
            if(Game.player.Protected)throw new Exception("Natural spawn protection has not expired.");
            if(UnityEngine.Object.FindObjectsByType<CastleHazard>(FindObjectsSortMode.None).Length<18)
                throw new Exception("Authored hazards are not intact at trial start.");
        }
        private static List<Segment> Routes(bool wings)
        {
            var result=new List<Segment>();
            void Add(params Ledge[] route){for(int i=0;i+1<route.Length;i++)result.Add(new Segment(route[i],route[i+1]));}
            Ledge Deck(float a,float b)=>new Ledge("deck "+a+".."+b,a,b);
            Ledge Upper(float x)=>Ledge.Pillar("upper "+x,x,WorldBuilder.UpperRouteHeight,1.8f);
            Ledge Lower(int index)
            {
                Vector3 authored=WorldBuilder.PrecisionPlatforms[index];
                return Ledge.Pillar("lower "+authored.x,authored.x,authored.y,authored.z);
            }
            if(wings)
            {
                Add(Deck(0,23),Upper(32),Upper(40),Deck(47,58));
                Add(Deck(47,58),Upper(65),Upper(73),Deck(76,86));
                Add(Deck(76,86),Upper(93),Upper(102),Deck(112,124));
                Add(Deck(112,124),Upper(132.5f),Deck(139,164));
            }
            else
            {
                Add(Deck(0,23),Lower(0),Lower(1),Lower(2),Deck(47,58));
                Add(Deck(47,58),Lower(3),Lower(4),Deck(76,86));
                Add(Deck(76,86),Lower(5),Lower(6),Lower(7),Deck(112,124));
                Add(Deck(112,124),Lower(8),Lower(9),Deck(139,164));
            }
            return result;
        }
        private static IEnumerator Run()
        {
            var gate=Gate(false);while(gate.MoveNext())yield return gate.Current;
            gate=Gate(true);while(gate.MoveNext())yield return gate.Current;
            foreach(bool wings in new[]{false,true})foreach(var segment in Routes(wings))
            {
                bool success=false;string previousFailure="";int repeatedFailure=0;
                // 0..10.81 seconds spans multiple firebar and crusher periods. These are
                // natural waits on the safe starting deck, never edits of hazard clocks.
                for(int attempt=0;attempt<24;attempt++)
                {
                    float phase=attempt*.47f;
                    var fresh=Fresh(wings,phase);while(fresh.MoveNext())yield return fresh.Current;
                    var trial=Fly(segment,wings);while(trial.MoveNext())yield return trial.Current;
                    Log((trialPassed?"PASS: ":"RETRY: ")+(wings?"WINGS ":"FLOWER SINGLE ")+segment+" | natural phase wait="+phase.ToString("F2")+" | "+trialDetails);
                    if(trialPassed){success=true;if(wings)upperPassed++;else lowerPassed++;break;}
                    if(!string.IsNullOrEmpty(trialFailureSignature))
                    {
                        repeatedFailure=trialFailureSignature==previousFailure?repeatedFailure+1:1;
                        previousFailure=trialFailureSignature;
                        if(repeatedFailure>=8)
                        {
                            Log("SEARCH STOP: eight consecutive natural phases produced the same failure position/form within0.1world units. A wider phase search is not claimed; this segment remains unproven and needs a different input route.");
                            break;
                        }
                    }
                    else {previousFailure="";repeatedFailure=0;}
                }
                if(!success){failed++;Log("UNPROVEN: "+(wings?"WINGS ":"FLOWER SINGLE ")+segment);}
            }
        }
        private static IEnumerator Fly(Segment segment,bool wings)
        {
            trialPassed=false;trialDetails="";trialFailureSignature="";var g=Game;var p=g.player;
            // Place a grounded launch only. Everything after this setup uses gameplay input.
            float half=p.box.size.y*.5f;
            p.body.position=new Vector2(segment.from.right+.11f,segment.from.top+half+.035f);
            p.body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Keys();
            double settle=Time.timeAsDouble+.07;
            while(Time.timeAsDouble<settle&&g.Playing)yield return null;
            if(!g.Playing||p.forms.Value!=Form.Fire||!p.grounded)
            {trialDetails="Launch unavailable: mode="+g.mode+", form="+p.forms.Value+", grounded="+p.grounded;yield break;}
            float startX=p.body.position.x,peak=p.body.position.y,minX=startX,maxX=startX;
            float aim=segment.to.left+Mathf.Min(.35f,(segment.to.right-segment.to.left)*.25f);
            bool longDeckDescent=wings&&segment.to.top<segment.from.top-3&&segment.to.left-segment.from.right>8;
            // Do not brake at the very lip of this broad deck: that drops Mario straight
            // into its approaching Goomba. Continue real D input to overfly the enemy.
            if(longDeckDescent)aim=segment.to.left+Mathf.Min(2.2f,(segment.to.right-segment.to.left)*.25f);
            double began=Time.timeAsDouble,end=began+(wings?3.8:2.1);
            int wingStage=0,releasedFrame=-1;bool leftGround=false,moving=true;
            var trace=new List<string>();
            var clocks=new List<string>();
            foreach(var hazard in UnityEngine.Object.FindObjectsByType<CastleHazard>(FindObjectsSortMode.None))
                clocks.Add(hazard.id+"="+hazard.CycleTime.ToString("F4",CultureInfo.InvariantCulture));
            clocks.Sort();
            trace.Add("# Independent segment: "+(wings?"Wings / ":"Fire Flower single jump / ")+segment);
            trace.Add("# Fresh intact authored map, staged grounded launch, no in-flight edits, no damage permitted.");
            trace.Add("# Hazard clocks at actual launch: "+string.Join("; ",clocks));
            trace.Add("# move_input/jump_input are raw InputAction samples in EditorApplication.update; they may differ from inputs consumed by the physics tick.");
            trace.Add("# queued_move_input/queued_jump_input describe the last keyboard state actually queued by Keys; the event may not yet be applied at this callback. Position and velocity are observed physics state.");
            trace.Add("time,x,y,vx,vy,grounded,move_input,jump_input,air_jump_used,form,queued_move_input,queued_jump_input");
            Keys(Key.D,Key.Space);
            while(Time.timeAsDouble<end&&g.Playing&&p.forms.Value==Form.Fire)
            {
                peak=Mathf.Max(peak,p.body.position.y);minX=Mathf.Min(minX,p.body.position.x);maxX=Mathf.Max(maxX,p.body.position.x);
                trace.Add(string.Format(CultureInfo.InvariantCulture,"{0:F4},{1:F4},{2:F4},{3:F4},{4:F4},{5},{6:F1},{7},{8},{9},{10:F1},{11}",
                    Time.timeAsDouble-began,p.body.position.x,p.body.position.y,p.body.linearVelocity.x,p.body.linearVelocity.y,
                    p.grounded,g.input.move.ReadValue<float>(),g.input.jump.IsPressed(),p.airJumpUsed,p.forms.Value,queuedMoveInput,queuedJumpInput));
                if(p.body.linearVelocity.y>3)leftGround=true;
                if(moving&&p.body.position.x>=aim-.035f)
                {moving=false;if(wingStage!=1)Keys(Key.Space);else Keys();}
                float feet=p.body.position.y-p.box.size.y*.5f;
                // The upper route sits below a solid roof. An immediate second jump into
                // that roof wastes the Wings; a skilled player delays the flap on descent.
                // A long descent to the deck benefits from falling farther before flapping:
                // the roof would otherwise waste the second arc and force a low side-on
                // collision with the deck's Goomba instead of a high, stompable approach.
                float delayedFlapHeight=segment.from.top+(longDeckDescent?-1f:.3f);
                bool flapWindow=segment.from.top>5?feet<=delayedFlapHeight:p.body.linearVelocity.y<1.1f;
                if(wings&&leftGround&&wingStage==0&&flapWindow&&p.body.linearVelocity.y<1.1f&&Time.timeAsDouble>began+.3)
                {wingStage=1;releasedFrame=Time.frameCount;if(moving)Keys(Key.D);else Keys();}
                else if(wings&&wingStage==1&&Time.frameCount-releasedFrame>=2)
                {wingStage=2;if(moving)Keys(Key.D,Key.Space);else Keys(Key.Space);}
                if(leftGround&&Time.timeAsDouble>began+.24&&p.grounded&&p.body.linearVelocity.y<=.1f&&Mathf.Abs(feet-segment.to.top)<.18f&&
                    p.body.position.x>segment.to.left-.28f&&p.body.position.x<segment.to.right+.28f)
                {trialPassed=true;break;}
                if(feet<-.3f)break;
                yield return null;
            }
            Keys();
            if(trialPassed)
            {
                string file=(wings?"wings-":"single-")+segment.from.right.ToString("F3",CultureInfo.InvariantCulture)+"-to-"+segment.to.left.ToString("F3",CultureInfo.InvariantCulture)+".csv";
                File.WriteAllLines(Path.Combine(traceFolder,file),trace);Log("TRACE: "+file+" | launch clocks: "+string.Join("; ",clocks));
            }
            trialDetails="start="+startX.ToString("F3")+", landed/last=("+p.body.position.x.ToString("F3")+","+p.body.position.y.ToString("F3")+")"+
                ", x-range="+minX.ToString("F2")+".."+maxX.ToString("F2")+", peak feet="+(peak-half).ToString("F2")+
                ", time="+(Time.timeAsDouble-began).ToString("F3")+", wingPress="+(wingStage==2)+", form="+p.forms.Value+", mode="+g.mode;
            if(!trialPassed)
            {
                trialFailureSignature=p.forms.Value+"|"+g.mode+"|"+Mathf.RoundToInt(p.body.position.x*10)+"|"+Mathf.RoundToInt(p.body.position.y*10);
                foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
                    if(!enemy.dead&&Vector2.Distance(enemy.transform.position,p.body.position)<2)
                        trialDetails+=", nearby enemy="+enemy.id+"@"+enemy.transform.position.ToString("F3");
            }
        }
        private static IEnumerator Gate(bool wings)
        {
            bool success=false;
            var fresh=Fresh(wings,0);while(fresh.MoveNext())yield return fresh.Current;
            var g=Game;var p=g.player;CastleBarrier seal=null;
            foreach(var barrier in UnityEngine.Object.FindObjectsByType<CastleBarrier>(FindObjectsSortMode.None))if(barrier.id=="w3.seal.entry")seal=barrier;
            if(seal==null)throw new Exception("Authored entrance seal missing.");
            p.body.position=new Vector2(13.7f,p.box.size.y*.5f+.035f);p.body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return Wait(.08f);
            if(!wings)
            {
                for(int shot=0;shot<3;shot++)
                {Keys(Key.J);yield return Wait(.08f);Keys();yield return Wait(.34f);}
                success=seal==null||seal.Destroyed;
                Log((success?"PASS: ":"UNPROVEN: ")+"Real J presses fire three projectiles and open the intact authored entry seal without terrain edits.");
            }
            else
            {
                int stage=0,release=-1;double began=Time.timeAsDouble,until=began+3.5;float maxFeet=0;bool approach=false;
                // Gain height before approaching the white-hot wall. Leaning against it
                // throughout both jumps would legitimately trigger its contact heat.
                Keys(Key.Space);
                while(Time.timeAsDouble<until&&g.Playing&&p.forms.Value==Form.Fire)
                {
                    float feet=p.body.position.y-p.box.size.y*.5f;maxFeet=Mathf.Max(maxFeet,feet);
                    if(stage==0&&Time.timeAsDouble>began+.3&&p.body.linearVelocity.y<1.1f){Keys();release=Time.frameCount;stage=1;}
                    else if(stage==1&&Time.frameCount-release>=2){Keys(Key.Space);stage=2;}
                    if(stage==2&&!approach&&feet>5.2f){approach=true;Keys(Key.D,Key.Space);}
                    if(p.body.position.x>17.2f){success=true;break;}
                    yield return null;
                }
                Keys();success&=seal!=null&&!seal.Destroyed;
                Log((success?"PASS: ":"UNPROVEN: ")+"Real two-press Space/D crest of intact entry seal; max feet="+maxFeet.ToString("F3")+", x="+p.body.position.x.ToString("F3")+", seal retained="+(seal!=null&&!seal.Destroyed));
            }
            if(!success)failed++;
        }
    }
}
