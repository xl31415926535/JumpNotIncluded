using UnityEngine;

namespace JumpNotIncluded
{
    public class PlayerMotor : MonoBehaviour
    {
        public SceneRoot game;
        public Rigidbody2D body;
        public BoxCollider2D box;
        public SpriteRenderer visual;
        public SpriteRenderer wings,gun;
        public PlayerFormController forms;
        public BuffStateController buffs;
        public bool grounded;
        public int facing=1;
        public bool Big=>forms.Value==Form.Super||forms.Value==Form.Fire;
        public const float HurtDuration=1.5f;
        public bool Protected=>buffs.Value!=Buff.None||forms.IsHurt||grace>0;
        public bool airJumpUsed;
        private float coyote,jumpBuffer,fireCooldown,stepTime,height=1,grace=1,errorCooldown,wingTime;
        public void Init(SceneRoot root,Vector2 position,Form form)
        {
            game=root;transform.position=position;gameObject.layer=9;
            body=gameObject.AddComponent<Rigidbody2D>();body.gravityScale=3.2f;
            body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            body.interpolation=RigidbodyInterpolation2D.Interpolate;
            box=gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(.7f,.95f);
            var mat=new PhysicsMaterial2D("Player friction"){friction=0,bounciness=0};box.sharedMaterial=mat;
            var spriteObject=new GameObject("Sprite");spriteObject.transform.SetParent(transform,false);
            visual=spriteObject.AddComponent<SpriteRenderer>();visual.sharedMaterial=game.assets.blueKey;visual.sortingOrder=10;
            var wingObject=new GameObject("Monarch Wings");wingObject.transform.SetParent(transform,false);
            wings=wingObject.AddComponent<SpriteRenderer>();wings.sprite=game.assets.Sprite("wings");wings.sortingOrder=9;wings.enabled=false;
            var gunObject=new GameObject("Gatling barrels");gunObject.transform.SetParent(transform,false);
            gun=gunObject.AddComponent<SpriteRenderer>();gun.sprite=game.assets.solid;gun.color=new Color(.22f,.26f,.31f);gun.sortingOrder=11;gun.enabled=false;
            for(int i=0;i<3;i++)
            {
                var barrel=new GameObject("Barrel");barrel.transform.SetParent(gunObject.transform,false);barrel.transform.localPosition=new Vector3(.16f,(i-1)*.33f,0);
                var sr=barrel.AddComponent<SpriteRenderer>();sr.sprite=game.assets.solid;sr.color=new Color(.65f,.7f,.75f);sr.sortingOrder=12;
                barrel.transform.localScale=new Vector3(1,.13f,1);
            }
            forms=gameObject.AddComponent<PlayerFormController>();forms.states=game.assets.formStates;forms.events=game.events;forms.Set((int)form,true);
            var buffObject=new GameObject("Buff state machine");buffObject.transform.SetParent(transform,false);
            buffs=buffObject.AddComponent<BuffStateController>();buffs.states=game.assets.buffStates;buffs.events=game.events;buffs.Set(0,true);
            height=Big?1.9f:1;box.size=new Vector2(.7f,height-.05f);
            ApplyHeight(); UpdateSprite();
        }
        private void Update()
        {
            if(game==null||!game.Playing)return;
            float dt=Time.deltaTime;stepTime+=dt;fireCooldown-=dt;grace-=dt;errorCooldown-=dt;wingTime-=dt;
            forms.Tick(dt);buffs.Tick(dt);
            grounded=Physics2D.OverlapBox(body.position+Vector2.down*(height*.5f+.06f),new Vector2(.57f,.14f),0,1<<8)!=null;
            bool landed=grounded&&body.linearVelocity.y<=.1f;
            if(landed)airJumpUsed=false;
            coyote=landed?.12f:coyote-dt;jumpBuffer-=dt;
            if(game.input.jump.WasPressedThisFrame())
            {
                if(game.Run.Owns(Product.Jump))jumpBuffer=.12f;
                else if(errorCooldown<=0){game.events.Sound("error");errorCooldown=.25f;}
            }
            if(game.input.jump.WasReleasedThisFrame()&&body.linearVelocity.y>0)
                body.linearVelocity=new Vector2(body.linearVelocity.x,body.linearVelocity.y*.5f);
            bool gatling=game.Run.Owns(Product.Gatling);
            if((gatling?game.input.fire.IsPressed():game.input.fire.WasPressedThisFrame())&&fireCooldown<=0&&game.Run.CanFire(forms.Value,buffs.Value))
            {
                fireCooldown=gatling?.1f:.3f;
                if(gatling||FindObjectsByType<Fireball>(FindObjectsSortMode.None).Length<3)
                {var go=new GameObject(gatling?"Gatling bullet":"Fireball");go.AddComponent<Fireball>().Init(game,this,gatling);game.events.Sound("fire");}
            }
            if(transform.position.y<-4)game.KillPlayer("Gravity has claimed another non-premium customer.");
            ApplyHeight();UpdateSprite();
        }
        private void FixedUpdate()
        {
            if(game==null||!game.Playing)return;
            float movement=game.input.move.ReadValue<float>();
            if(Mathf.Abs(movement)>.1f)facing=movement>0?1:-1;
            var velocity=body.linearVelocity;
            if(!forms.IsHurt||forms.elapsed>=.18f)velocity.x=movement*5.5f*(buffs.Value==Buff.VIP?1.25f:1);
            if(jumpBuffer>0&&(coyote>0||game.Run.Owns(Product.DoubleJump)&&!airJumpUsed))
            {
                if(coyote<=0){airJumpUsed=true;wingTime=.45f;}
                velocity.y=17f;jumpBuffer=0;coyote=0;game.events.Sound("jump");
            }
            body.linearVelocity=velocity;
        }
        public void Hit(bool poison=false,string reason=null)
        {
            if(!game.Playing||Protected)return;
            if(forms.Value==Form.Small)
                game.KillPlayer(reason??(poison?"Purple mushrooms are poisonous. Mind what you pick up.":"A Goomba has ended your free trial."));
            else
            {
                forms.Signal("damage");grace=HurtDuration;
                body.linearVelocity=new Vector2(-facing*3,4);ApplyHeight();UpdateSprite();
            }
        }
        public void PowerUp(string signal)
        {forms.Signal(signal);ApplyHeight();UpdateSprite();}
        public void Bounce(){body.linearVelocity=new Vector2(body.linearVelocity.x,10.5f);}
        private void ApplyHeight()
        {
            float wanted=Big?1.9f:1;
            if(Mathf.Abs(wanted-height)>.1f)
            {
                // Rigidbody interpolation can leave transform.position one frame behind physics.
                body.position+=Vector2.up*(wanted-height)*.5f;height=wanted;box.size=new Vector2(.7f,height-.05f);
            }
            visual.transform.localPosition=new Vector3(0,-height*.5f,0);
        }
        private void UpdateSprite()
        {
            string prefix=forms.Value==Form.Fire?"fire":Big?"big":"small";
            string pose=!grounded?"jump":Mathf.Abs(body.linearVelocity.x)>.1f?"walk"+(Mathf.FloorToInt(stepTime*10)%3):"idle";
            // The course atlas stores the left-facing frames.
            visual.sprite=game.assets.Sprite(prefix+"-"+pose);visual.flipX=facing>0;
            visual.enabled=!forms.IsHurt||Mathf.FloorToInt(forms.elapsed*14)%2==0;
            visual.color=buffs.Value==Buff.VIP?new Color(1,1,.5f):buffs.Value==Buff.Star?Color.HSVToRGB(Mathf.Repeat(stepTime*.7f,1),.4f,1):Color.white;
            wings.enabled=wingTime>0;
            float width=2.7f/wings.sprite.bounds.size.x,flap=.6f+.4f*Mathf.Abs(Mathf.Sin(wingTime*24));
            wings.transform.localScale=new Vector3(width,width*flap,1);wings.transform.localPosition=new Vector3(0,.25f,0);
            gun.gameObject.SetActive(game.Run.Owns(Product.Gatling));gun.enabled=true;
            gun.transform.localPosition=new Vector3(facing*.65f,.05f,0);gun.transform.localScale=new Vector3(facing*.85f,.3f,1);
        }
    }
}
