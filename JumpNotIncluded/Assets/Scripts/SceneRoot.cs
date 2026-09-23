using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JumpNotIncluded
{
    public class SceneRoot : MonoBehaviour
    {
        public GameAssets assets;
        public RunState session;
        public GameEvents events;
        public ScreenMode mode;
        public int world;
        public PlayerMotor player;
        public WorldBuilder level;
        public InputRouter input;
        public AudioDirector audioDirector;
        public GameUI ui;
        public Camera cameraView;
        public string toast,deathReason;
        public float toastUntil,adTime,shopTime;
        public const float ReviveAdDuration=2;
        public const float AdCampaignDuration=5;
        public AdKind adKind;
        public AdCampaign adCampaign;
        public int adEarned;
        public bool adFromDeath,loading,hasFocus=true;
        public bool rechargeOpen {get;private set;}
        public float loadProgress;
        private int adPaidSeconds;
        private int adCampaignSegment;
        private bool skipAdFrame;
        public RunModel Run => session.data;
        public bool Playing => mode==ScreenMode.Playing;
        public int HighScore => PlayerPrefs.GetInt("jni.highscore",0);

        private void Awake()
        {
            Time.timeScale=1;
            if(assets==null) assets=Resources.Load<GameAssets>("GameAssets");
            if(session==null) session=Resources.Load<RunState>("RunState");
            if(events==null) events=Resources.Load<GameEvents>("Events");
            if(assets==null||session==null||events==null)
            { Debug.LogError("Run Tools > Jump Not Included > Setup project once."); enabled=false;return; }
            string scene=SceneManager.GetActiveScene().name;
            world=scene=="World02"?2:scene=="World01"?1:0;
            mode=scene=="Loading"?ScreenMode.Loading:world==0?ScreenMode.Menu:ScreenMode.Playing;
            if(Run==null) session.NewRun();
            if(world>0) Run.world=world;
            if(world==2)Run.Grant("support",600);
            cameraView=GetComponentInChildren<Camera>();
            if(cameraView==null)
            {
                var go=new GameObject("Camera",typeof(Camera),typeof(AudioListener));go.transform.SetParent(transform);
                cameraView=go.GetComponent<Camera>();
            }
            cameraView.orthographic=true; cameraView.orthographicSize=6.75f;
            cameraView.transform.position=new Vector3(11,4.7f,-10);
            cameraView.backgroundColor=world==2?new Color(.065f,.12f,.22f):new Color(.36f,.58f,.98f);
            cameraView.clearFlags=CameraClearFlags.SolidColor;
            input=gameObject.AddComponent<InputRouter>();input.Init();
            events.EnemyDefeated+=OnEnemyDefeated;
            audioDirector=gameObject.AddComponent<AudioDirector>();audioDirector.Init(this);
            level=gameObject.AddComponent<WorldBuilder>();level.Init(this,world==0?1:world);
            if(world>0)
            {
                var go=new GameObject("Player");player=go.AddComponent<PlayerMotor>();
                float spawnY=session.carryForm==Form.Super||session.carryForm==Form.Fire?1.02f:.55f;
                player.Init(this,session.restoreCheckpoint?session.checkpointPosition:new Vector2(2,spawnY),session.carryForm);
                session.restoreCheckpoint=false;
                if(string.IsNullOrEmpty(session.checkpointJson)) SaveCheckpoint(new Vector2(2,spawnY));
                if(session.shopOnLoad){session.shopOnLoad=false;SetMode(ScreenMode.Shop);}
                else if(Run.trialPending){Run.trialPending=false;player.buffs.Signal("vip");}
            }
            ui=gameObject.AddComponent<GameUI>();ui.Init(this);
            SetMode(mode);
        }
        private void Update()
        {
            if(input==null) return;
            if(Playing)
            {
                Run.playTime+=Time.deltaTime;
                if(hasFocus&&(Mathf.Abs(input.move.ReadValue<float>())>.1f||input.jump.IsPressed()||
                    input.jump.WasPressedThisFrame()||input.fire.IsPressed()||input.fire.WasPressedThisFrame()))
                    Run.activeInputTime+=Time.deltaTime;
                if(player!=null)
                {
                    // Invisible, fixed safe checkpoints; no terminal or on-screen prompt.
                    float checkpoint=world==1?(player.transform.position.x>=60?60:26):(player.transform.position.x>=80?80:34);
                    if(session.checkpointPosition.x<checkpoint&&player.transform.position.x>=checkpoint&&player.grounded&&player.transform.position.y<2)
                        SaveCheckpoint(new Vector2(checkpoint,player.Big?1.02f:.55f));
                    float target=Mathf.Clamp(player.transform.position.x+4,11,level.length-10);
                    cameraView.transform.position=Vector3.Lerp(cameraView.transform.position,new Vector3(target,4.7f,-10),Time.deltaTime*7);
                }
            }
            if(mode==ScreenMode.Shop) shopTime+=Time.unscaledDeltaTime;
            if(mode==ScreenMode.Ad&&hasFocus)
            {if(skipAdFrame)skipAdFrame=false;else AdvanceAd(Time.unscaledDeltaTime);}
            if(input.pause.WasPressedThisFrame())
            {
                if(Playing){events.Sound("pause");SetMode(ScreenMode.Pause);}
                else if(mode==ScreenMode.Pause||mode==ScreenMode.Shop) CloseOverlay();
                else if(mode==ScreenMode.Loading&&!loading) ToMenu();
            }
        }
        public void SetMode(ScreenMode value)
        {
            mode=value;Time.timeScale=Playing?1:0;
            if(value!=ScreenMode.Shop&&value!=ScreenMode.Ad)rechargeOpen=false;
            input?.SetPlaying(Playing);
            if(ui!=null) ui.ResetFocus();
        }
        public void Toast(string message,float seconds=3)
        {toast=message;toastUntil=Time.unscaledTime+seconds;}
        public void OpenShop()
        {
            if(mode!=ScreenMode.Dead)return;
            session.Restore();session.shopOnLoad=true;ReloadWorld();
        }
        public void SaveCheckpoint(Vector2 position)
        { session.Capture(position,player!=null?player.forms.Value:Form.Small); }
        public void OpenRecharge()
        {
            if(mode!=ScreenMode.Shop)return;
            rechargeOpen=true;toastUntil=0;ui?.ResetFocus();
        }
        public void CloseOverlay()
        {
            if(mode==ScreenMode.Shop&&rechargeOpen)
            {rechargeOpen=false;toastUntil=0;ui?.ResetFocus();return;}
            if(mode==ScreenMode.Shop||mode==ScreenMode.Pause){toastUntil=0;SetMode(ScreenMode.Playing);}
        }
        public bool Buy(Product product)
        {
            if(mode!=ScreenMode.Shop)return false;
            int price=RunModel.Price(product);
            foreach(var definition in assets.products)if(definition.product==product)price=definition.price;
            if(!Run.Buy(product,price)){events.Sound("error");Toast("Not enough coins, or you already own this upgrade.");return false;}
            if(product==Product.FireFlower)player.PowerUp("flower");else events.Sound("powerup");
            string[] messages={"The ground's monopoly is over. Your Z-axis empire begins.","The sun has accepted your application. Let your enemies admire the heat.","Poison has been abolished. Nature now serves your interests.","The designer's secrets are now your private property.","Hallownest's royal inheritance is yours. Let the void become your stairway.","Reality has accepted your payment. Hold J and march toward your inevitable victory."};
            Toast(messages[(int)product],4);
            events.Score(Run.score);SaveCheckpoint(new Vector2(session.checkpointPosition.x,player.Big?1.02f:.55f));return true;
        }
        public void Refund()
        {
            if(mode!=ScreenMode.Shop)return;
            int before=Run.coins;
            if(!Run.RefundFireFlower()){events.Sound("error");Toast("No refundable Fire Flower order.");return;}
            player.forms.Signal("refund");
            events.Sound("coin");Toast("Refunded "+(Run.coins-before)+" coins. Fire Flower upgrade removed.",4);
            SaveCheckpoint(new Vector2(session.checkpointPosition.x,player.Big?1.02f:.55f));
        }
        public bool ExchangeCash(int pack)
        {
            if(mode!=ScreenMode.Shop)return false;
            if(!Run.ExchangeCash(pack)){events.Sound("error");Toast("Not enough ad cash. Watch & earn first.");return false;}
            events.Sound("coin");Toast("Your royal treasury swells by "+RunModel.PackCoins(pack)+" coins. Destiny applauds.");
            SaveCheckpoint(new Vector2(session.checkpointPosition.x,player.Big?1.02f:.55f));return true;
        }
        public void StartAd(AdKind kind)
        {
            if(mode!=ScreenMode.Dead&&mode!=ScreenMode.Shop)return;
            if(kind!=AdKind.Revive&&kind!=AdKind.Cash)return;
            if(kind==AdKind.Revive&&mode!=ScreenMode.Dead)return;
            if(kind==AdKind.Cash&&Run.wallet>=RunModel.WalletLimit)return;
            adFromDeath=mode==ScreenMode.Dead;
            if(adFromDeath)session.Restore();
            adKind=kind;adTime=0;adPaidSeconds=0;adEarned=0;
            adCampaign=session.NextAdCampaign();adCampaignSegment=0;
            SetMode(ScreenMode.Ad);
        }
        public void AdvanceAd(float seconds)
        {
            if(mode!=ScreenMode.Ad||!hasFocus||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            // Count only the visible portion of the ad, including unrewarded fractions.
            float remaining=adKind==AdKind.Revive?ReviveAdDuration-adTime:
                adPaidSeconds+Mathf.Ceil((RunModel.WalletLimit-Run.wallet)/100f)-adTime;
            float watched=Mathf.Min(seconds,Mathf.Max(0,remaining));
            adTime+=watched;Run.adWatchTime+=watched;
            if(adKind==AdKind.Revive)
            {if(adTime>=ReviveAdDuration)FinishAd();return;}
            int wholeSeconds=Mathf.FloorToInt(adTime);
            int due=wholeSeconds-adPaidSeconds;
            if(due>0)
            {
                int earned=Run.Credit(Mathf.Min(due,99)*100);
                if(adPaidSeconds==0&&earned>0)Run.ads++;
                adPaidSeconds=wholeSeconds;adEarned+=earned;
                session.Capture(session.checkpointPosition,session.checkpointForm);
                if(earned>0)events.Sound("coin");
            }
            if(Run.wallet>=RunModel.WalletLimit){FinishAd();return;}
            int segment=Mathf.FloorToInt(adTime/AdCampaignDuration);
            if(segment>adCampaignSegment)
            {adCampaign=session.NextAdCampaign();adCampaignSegment=segment;}
        }
        public void FinishAd()
        {
            if(mode!=ScreenMode.Ad)return;
            if(adKind==AdKind.Revive)
            {
                if(adTime<ReviveAdDuration)return;
                Run.ads++;session.Capture(session.checkpointPosition,session.checkpointForm);
                session.shopOnLoad=false;ReloadWorld();return;
            }
            // Full seconds were credited as they elapsed; closing the ad adds nothing.
            session.Capture(session.checkpointPosition,session.checkpointForm);
            if(adFromDeath)
            {
                session.shopOnLoad=true;ReloadWorld();return;
            }
            SetMode(ScreenMode.Shop);
            Toast(Run.wallet>=RunModel.WalletLimit?"Balance limit reached: $99.00.":"Your cash is saved. Thanks for your time.",4);
        }
        public void StartTrial()
        {
            if(mode!=ScreenMode.Shop||world!=2 || Run.trialClaimed&&!Run.trialPending)return;
            Run.trialClaimed=true;Run.trialPending=true;
            SaveCheckpoint(session.checkpointPosition);
            Run.trialPending=false;
            player.buffs.Signal("vip");SetMode(ScreenMode.Playing);
        }
        public void KillPlayer(string reason)
        {
            if(!Playing)return;
            deathReason=reason;Run.deaths++;player.forms.Set((int)Form.Dead);
            player.buffs.Set((int)Buff.None,true);player.body.linearVelocity=Vector2.zero;
            SetMode(ScreenMode.Dead);RecordHigh();events.Sound("death");
        }
        public void Retry()
        {session.Restore();ReloadWorld();}
        private void ReloadWorld()
        {Time.timeScale=1;SceneManager.LoadScene(world==2?"World02":"World01");}
        public void NewGame()
        {session.NewRun();Time.timeScale=1;SceneManager.LoadScene("Loading");}
        public void ToMenu()
        {Time.timeScale=1;SceneManager.LoadScene("MainMenu");}
        public void EnterWorld()
        {if(!loading)StartCoroutine(LoadWorld());}
        private IEnumerator LoadWorld()
        {
            loading=true;Time.timeScale=1;
            var operation=SceneManager.LoadSceneAsync(session.nextWorld==2?"World02":"World01");
            while(!operation.isDone){loadProgress=operation.progress;yield return null;}
        }
        public void CompleteWorld()
        {
            if(!Playing)return;
            AddScore("finish"+world,1000);events.Sound("complete");
            if(world==1)
            {
                Run.Grant("world1",400);session.nextWorld=2;
                session.carryForm=player.forms.Value;
                session.checkpointJson="";SetMode(ScreenMode.Results);
            }
            else {RecordHigh();SetMode(ScreenMode.Results);}
        }
        public void NextWorld(){Time.timeScale=1;SceneManager.LoadScene("Loading");}
        public void AddScore(string id,int value)
        {if(Run.ScoreOnce(id,value))events.Score(Run.score);}
        public void AddCoin(string id)
        {if(Run.ScoreOnce(id,100)){Run.coins++;events.Score(Run.score);}}
        private void OnEnemyDefeated(string id,int points)
        {AddScore(id,points);events.Sound("stomp");}
        public void RecordHigh()
        {if(Run.score>HighScore){PlayerPrefs.SetInt("jni.highscore",Run.score);PlayerPrefs.Save();}}
        public void ClearHigh(){PlayerPrefs.DeleteKey("jni.highscore");PlayerPrefs.Save();}
        private void OnApplicationFocus(bool focus)
        {
            hasFocus=focus;
            if(!focus&&Playing&&!Application.runInBackground)SetMode(ScreenMode.Pause);
            // Ignore a possible large delta from time spent outside the window.
            if(focus)skipAdFrame=true;
        }
        private void OnDestroy()
        {if(events!=null)events.EnemyDefeated-=OnEnemyDefeated;Time.timeScale=1;}
    }
}
