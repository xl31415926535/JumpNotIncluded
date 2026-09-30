using UnityEngine;

namespace JumpNotIncluded
{
    // The crossover is an equipment layer. Mario's form and timed buffs survive inside the hull.
    public sealed class MechSuit : MonoBehaviour
    {
        public const float CruiseSpeed=9,AscentSpeed=6,DescentSpeed=4,HullHeight=2.2f;
        public const float MinAltitude=1.35f,MaxAltitude=7.8f,LockRange=14;
        public bool Active=>player!=null&&player.MechActive;
        public bool IsThrusting {get;private set;}
        public bool IsGrounded {get;private set;}
        public bool IsCruising {get;private set;}
        public int FlightFrame {get;private set;}
        public MechExhaust Exhaust {get;private set;}
        public MechExhaust CruiseExhaust {get;private set;}
        public Bounds HullBounds=>MechArt.VisibleBounds(hull,FlightFrame);
        public int ShotsFired {get;private set;}
        public string LastTarget {get;private set;}="";
        public AudioSource ThrusterSource {get;private set;}
        public AudioSource WeaponSource {get;private set;}
        public AudioSource StatusSource {get;private set;}
        public AudioSource IgnitionSource=>ignition;
        public int LandingsPlayed {get;private set;}
        public int IgnitionsPlayed {get;private set;}
        public int BoostsPlayed {get;private set;}
        public int ShieldSoundsPlayed {get;private set;}
        private PlayerMotor player;
        private SceneRoot game;
        private SpriteRenderer hull;
        private Sprite[] flight;
        private LineRenderer[] reticle;
        private AudioSource ignition;
        private Material lightMaterial;
        private GameObject equipment;
        private bool wasEquipped,wasThrusting,wasGrounded,wasCruising,audioStateReady,audioPaused;
        private readonly bool[] resumeAudio=new bool[4];
        private AudioSource[] sources;
        private float animationAge,phaseTime,fireCooldown,impactTime,lockScan,ignitionCooldown,boostCooldown,shieldCooldown;
        private Transform target;
        private Collider2D targetCollider;
        private readonly Color cyan=new Color(.2f,.95f,1);

        public void Init(PlayerMotor owner)
        {
            player=owner;game=owner.game;
            equipment=new GameObject("WAR GOD / Moon Killer replica");equipment.transform.SetParent(transform,false);
            var art=new GameObject("Starfield camouflage chassis");art.transform.SetParent(equipment.transform,false);
            hull=art.AddComponent<SpriteRenderer>();hull.sortingOrder=12;
            hull.transform.localPosition=Vector3.down*1.075f;hull.transform.localScale=Vector3.one;
            hull.sharedMaterial=game.assets.mechPixelMaterial;
            flight=MechArt.CreateFrames(game.assets);
            lightMaterial=new Material(Shader.Find("Sprites/Default"));
            Exhaust=equipment.AddComponent<MechExhaust>();Exhaust.Init(equipment.transform,10);
            CruiseExhaust=equipment.AddComponent<MechExhaust>();CruiseExhaust.Init(equipment.transform,9);
            reticle=new LineRenderer[4];
            for(int i=0;i<4;i++)
            {reticle[i]=Line("Pixel target corner "+i,equipment.transform,1f/16,16,true);reticle[i].positionCount=3;}
            ThrusterSource=Source("Vacuum drive / World SFX",game.assets.mechThrustLoop,true);
            ThrusterSource.volume=0;
            WeaponSource=Source("Moon Killer laser / World SFX",null,false);
            ignition=Source("Drive ignition / World SFX",game.assets.mechIgnition,false);
            StatusSource=Source("Chassis feedback / World SFX",null,false);
            sources=new[]{ThrusterSource,WeaponSource,ignition,StatusSource};
            equipment.SetActive(false);
        }
        private AudioSource Source(string label,AudioClip clip,bool loop)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            source.outputAudioMixerGroup=game.assets.worldGroup;source.clip=clip;source.loop=loop;return source;
        }
        private LineRenderer Line(string label,Transform parent,float width,int order,bool worldSpace)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=lightMaterial;line.useWorldSpace=worldSpace;line.startWidth=line.endWidth=width;
            line.sortingOrder=order;line.numCapVertices=0;line.startColor=line.endColor=cyan;return line;
        }
        public void RefreshEquipment()
        {
            if(wasEquipped==Active)return;
            wasEquipped=Active;equipment.SetActive(Active);player.body.gravityScale=Active?0:3.2f;
            IsThrusting=wasThrusting=IsGrounded=IsCruising=false;phaseTime=animationAge=0;target=null;targetCollider=null;
            audioStateReady=false;ignitionCooldown=boostCooldown=shieldCooldown=0;
            if(Active)player.body.linearVelocity=Vector2.zero;
            else StopAudio();
        }
        private void Update()
        {
            if(player==null)return;
            RefreshEquipment();
            if(!Active)return;
            if(!game.Playing||!game.hasFocus)
            {
                PauseAudio(true);foreach(var corner in reticle)corner.enabled=false;
                Exhaust.SetVisible(false);CruiseExhaust.SetVisible(false);
                return;
            }
            Tick(Time.deltaTime);
        }
        // Fixed-step flight cancels falling momentum as well as gravity, including a saved pit position.
        public void FixedFlight(float movement,bool thrust,bool descend)
        {
            if(!Active||!game.Playing)return;
            player.body.gravityScale=0;
            var position=player.body.position;
            // The abyss floor is a flight safety net, not an invisible platform over real ground.
            // Terrain support lets the boots actually touch floors and pipe tops and shut down.
            bool supported=TrySupport(position,out float surfaceY);
            float lower=supported?surfaceY+player.box.size.y*.5f:MinAltitude;
            position.y=Mathf.Clamp(position.y,lower,MaxAltitude);
            position.x=Mathf.Clamp(position.x,.8f,game.level.length+2);
            player.body.position=position;
            IsThrusting=thrust&&position.y<MaxAltitude-.01f;
            IsGrounded=supported&&position.y<=lower+.04f&&!thrust;
            float vertical=IsThrusting?AscentSpeed:descend&&position.y>lower+.01f?-DescentSpeed:0;
            // Bound the next step too: no off-screen overshoot at the ceiling or abyss floor.
            vertical=Mathf.Clamp(vertical,(lower-position.y)/Time.fixedDeltaTime,(MaxAltitude-position.y)/Time.fixedDeltaTime);
            player.body.linearVelocity=new Vector2(movement*CruiseSpeed,vertical);
            if(Mathf.Abs(movement)>.1f)player.facing=movement>0?1:-1;
        }
        private bool TrySupport(Vector2 position,out float surfaceY)
        {
            surfaceY=float.NegativeInfinity;
            // Ignore side walls and surfaces above the boots, so moving into a pipe cannot
            // teleport the suit onto it. Actual 2D collisions continue to block the chassis.
            float foot=position.y-player.box.size.y*.5f;
            var origin=new Vector2(position.x,Mathf.Max(position.y,MinAltitude)+.12f);
            foreach(var hit in Physics2D.BoxCastAll(origin,new Vector2(.86f,.04f),0,Vector2.down,24,1<<8))
                if(hit.collider!=null&&!hit.collider.isTrigger&&hit.normal.y>.7f&&hit.point.y<=foot+.15f)
                    surfaceY=Mathf.Max(surfaceY,hit.point.y);
            return !float.IsNegativeInfinity(surfaceY);
        }
        public void Tick(float dt)
        {
            if(!Active||!game.Playing)return;
            if(!game.hasFocus){PauseAudio(true);return;}
            PauseAudio(false);
            animationAge+=dt;phaseTime+=dt;fireCooldown-=dt;impactTime-=dt;lockScan-=dt;
            ignitionCooldown-=dt;boostCooldown-=dt;shieldCooldown-=dt;
            Animate();
            float effects=game.audioDirector.world.volume;
            WeaponSource.volume=effects*.52f;ignition.volume=effects*.52f;StatusSource.volume=effects*.5f;
            if(audioStateReady)
            {
                if(!IsGrounded&&(IsThrusting&&!wasThrusting||wasGrounded)&&ignitionCooldown<=0&&game.assets.mechIgnition!=null)
                {ignition.Play();IgnitionsPlayed++;ignitionCooldown=.25f;}
                if(IsGrounded&&!wasGrounded&&game.assets.mechLanding!=null)
                {StatusSource.PlayOneShot(game.assets.mechLanding,.85f);LandingsPlayed++;}
                if(IsCruising&&!wasCruising&&boostCooldown<=0&&game.assets.mechBoost!=null)
                {StatusSource.PlayOneShot(game.assets.mechBoost,.55f);BoostsPlayed++;boostCooldown=.4f;}
            }
            audioStateReady=true;wasGrounded=IsGrounded;wasThrusting=IsThrusting;wasCruising=IsCruising;
            ThrusterSource.volume=Mathf.MoveTowards(ThrusterSource.volume,effects*(IsThrusting?.38f:IsCruising?.27f:.16f),dt*2);
            // Effects mute applies immediately; pitch follows power without restarting the loop.
            if(effects<=0)ThrusterSource.volume=0;
            ThrusterSource.pitch=Mathf.MoveTowards(ThrusterSource.pitch,IsThrusting?1.18f:IsCruising?1.04f:.84f,dt*2);
            if(IsGrounded){ThrusterSource.Stop();ignition.Stop();}
            else if(ThrusterSource.clip!=null&&!ThrusterSource.isPlaying)ThrusterSource.Play();
            // A target can disappear between pulses when another projectile kills it.
            if(lockScan<=0||!TargetAlive()) {FindTarget();lockScan=.08f;}
            bool firing=game.input.fire.IsPressed();
            DrawLock(firing&&TargetAlive());
            if(firing&&fireCooldown<=0)
            {
                FireLaser();fireCooldown=.28f;
            }
        }
        private void Animate()
        {
            IsCruising=!IsGrounded&&Mathf.Abs(player.body.linearVelocity.x)>.25f;
            bool walking=IsGrounded&&Mathf.Abs(player.body.linearVelocity.x)>.1f;
            // Six redrawn poses share Mario's logical pixels; exhaust is always separate art.
            FlightFrame=walking?1+Mathf.FloorToInt(animationAge*8)%2:IsGrounded?0:IsCruising?4+Mathf.FloorToInt(animationAge*6)%2:3;
            hull.sprite=flight[FlightFrame];
            hull.flipX=player.facing<0;
            hull.color=impactTime>0&&Mathf.FloorToInt(impactTime*12)%2==0?new Color(.55f,.85f,1):Color.white;
            hull.transform.localPosition=Vector3.down*1.075f;
            float power=IsGrounded?0:IsThrusting?.72f:IsCruising?.42f:.38f;
            Vector3 Nozzle(Vector3 pixel)
            {
                if(hull.flipX)pixel.x=-pixel.x;
                return equipment.transform.InverseTransformPoint(hull.transform.TransformPoint(pixel));
            }
            Exhaust.SetVisible(true);
            Exhaust.Render(animationAge,power,Nozzle(MechArt.SoleLocal(FlightFrame,false)),Nozzle(MechArt.SoleLocal(FlightFrame,true)),Vector2.down);
            CruiseExhaust.SetVisible(true);
            CruiseExhaust.Render(animationAge,IsCruising?.62f:0,Nozzle(MechArt.RearLocal(FlightFrame,false)),
                Nozzle(MechArt.RearLocal(FlightFrame,true)),Vector2.left*player.facing);
        }
        public void AbsorbHit()
        {
            if(!Active||!game.Playing)return;
            impactTime=.35f;
            if(shieldCooldown<=0&&game.assets.mechShield!=null)
            {
                StatusSource.volume=game.audioDirector.world.volume*.5f;
                StatusSource.PlayOneShot(game.assets.mechShield,.65f);ShieldSoundsPlayed++;shieldCooldown=.2f;
            }
        }
        private bool TargetAlive()
        {
            if(target==null||targetCollider==null||!targetCollider.enabled)return false;
            var enemy=target.GetComponent<EnemyActor>();if(enemy!=null)return !enemy.dead;
            var boss=target.GetComponent<BossActor>();return boss!=null&&boss.health>0;
        }
        private void FindTarget()
        {
            target=null;targetCollider=null;float best=LockRange*LockRange;
            void Consider(Component actor,Collider2D collider)
            {
                if(collider==null||!collider.enabled)return;
                Vector2 point=collider.bounds.center;
                var view=game.cameraView.WorldToViewportPoint(point);
                if(view.z<0||view.x<-.03f||view.x>1.03f||view.y<0||view.y>1)return;
                float distance=(point-player.body.position).sqrMagnitude;
                if(distance>best)return;
                best=distance;target=actor.transform;targetCollider=collider;
            }
            foreach(var boss in FindObjectsByType<BossActor>(FindObjectsSortMode.None))
                if(boss.health>0)Consider(boss,boss.GetComponent<Collider2D>());
            foreach(var enemy in FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
                if(!enemy.dead)Consider(enemy,enemy.GetComponent<Collider2D>());
        }
        private void DrawLock(bool visible)
        {
            foreach(var corner in reticle)corner.enabled=visible;
            if(!visible)return;
            var bounds=targetCollider.bounds;
            Vector3 center=bounds.center;float rx=bounds.extents.x+.25f,ry=bounds.extents.y+.25f;
            for(int i=0;i<4;i++)
            {
                int sx=i%2==0?-1:1,sy=i<2?-1:1;
                Vector3 corner=new Vector3(Mathf.Round((center.x+sx*rx)*16)/16,Mathf.Round((center.y+sy*ry)*16)/16,0);
                reticle[i].SetPosition(0,corner-Vector3.right*sx*.25f);reticle[i].SetPosition(1,corner);
                reticle[i].SetPosition(2,corner-Vector3.up*sy*.25f);
                reticle[i].startColor=reticle[i].endColor=Mathf.FloorToInt(animationAge*6)%2==0?new Color(.30f,.80f,1):Color.white;
            }
        }
        private void FireLaser()
        {
            FindTarget();
            Vector3 origin=(Vector3)player.body.position+new Vector3(player.facing*.35f,.25f,0);
            Vector3 end=TargetAlive()?targetCollider.bounds.center:origin+Vector3.right*player.facing*9;
            new GameObject("Moon Killer / pixel auto-locked laser").AddComponent<MechPixelLaser>().Init(game,origin,end);
            ShotsFired++;LastTarget="";
            if(game.assets.mechLaser!=null)WeaponSource.PlayOneShot(game.assets.mechLaser);
            if(TargetAlive())
            {
                var boss=target.GetComponent<BossActor>();var enemy=target.GetComponent<EnemyActor>();
                if(boss!=null){LastTarget=boss.id;boss.TakeDamage(BossActor.MaxHealth);}
                else if(enemy!=null){LastTarget=enemy.id;enemy.Defeat();}
            }
            target=null;targetCollider=null;lockScan=0;
        }
        private void PauseAudio(bool pause)
        {
            if(sources==null||audioPaused==pause)return;
            for(int i=0;i<sources.Length;i++)
                if(pause){resumeAudio[i]=sources[i].isPlaying;sources[i].Pause();}
                else if(resumeAudio[i])sources[i].UnPause();
            audioPaused=pause;
        }
        private void StopAudio()
        {
            if(sources!=null)foreach(var source in sources)if(source!=null)source.Stop();
            audioPaused=false;
        }
        private void OnDisable(){StopAudio();}
        private void OnDestroy()
        {
            if(flight!=null)foreach(var sprite in flight)if(sprite!=null)Destroy(sprite);
            if(lightMaterial!=null)Destroy(lightMaterial);
        }
    }

}
