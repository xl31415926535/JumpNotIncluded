using UnityEngine;

namespace JumpNotIncluded
{
    public class BossActor : MonoBehaviour
    {
        public const int MaxHealth=18;
        public int health=MaxHealth;
        public SceneRoot game;
        public string id {get;private set;}
        private Rigidbody2D body;private BoxCollider2D box;private SpriteRenderer sprite,healthFill;
        private float age,fireTimer=2.2f,hitFlash;private int direction=-1;
        private float left,right;
        public void Init(SceneRoot root,string key,float x,float min,float max,float firstShot=2.2f)
        {
            game=root;id=key;left=min;right=max;fireTimer=firstShot;transform.position=new Vector3(x,0,0);
            sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=game.assets.Sprite("bowser0");sprite.sharedMaterial=game.assets.blueKey;sprite.sortingOrder=6;
            transform.localScale=Vector3.one*1.35f;
            box=gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(1.75f,2);box.offset=Vector2.up;
            box.isTrigger=true;body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            var bar=new GameObject("Boss health");bar.transform.SetParent(transform,false);bar.transform.localPosition=Vector3.up*2.25f;
            healthFill=bar.AddComponent<SpriteRenderer>();healthFill.sprite=game.assets.solid;healthFill.color=new Color(1,.2f,.08f);healthFill.sortingOrder=10;
            bar.transform.localScale=new Vector3(1.8f,.08f,1);
        }
        private void Update()
        {
            if(!game.Playing||health<=0)return;
            age+=Time.deltaTime;hitFlash-=Time.deltaTime;fireTimer-=Time.deltaTime;
            sprite.sprite=game.assets.Sprite("bowser"+(Mathf.FloorToInt(age*4)%2));
            bool right=game.player.body.position.x>body.position.x;sprite.flipX=right;
            sprite.color=hitFlash>0?new Color(1,.45f,.4f):fireTimer<.45f?new Color(1,.7f,.4f):Color.white;
            if(fireTimer<=0&&Mathf.Abs(game.player.body.position.x-body.position.x)<20)
            {
                fireTimer=1.8f;var mouth=body.position+new Vector2(right?1.3f:-1.3f,1.25f);
                Vector2 aim=(game.player.body.position-mouth).normalized;
                new GameObject("Bowser flame").AddComponent<BossFlame>().Init(game,mouth,aim);
                game.events.Sound("bowser-fire");
            }
            healthFill.enabled=health<MaxHealth;
            healthFill.transform.localScale=new Vector3(1.8f*health/MaxHealth,.08f,1);
        }
        private void FixedUpdate()
        {
            if(!game.Playing||health<=0)return;
            float x=body.position.x+direction*1.25f*Time.fixedDeltaTime;
            if(x<left){x=left;direction=1;}if(x>right){x=right;direction=-1;}
            body.MovePosition(new Vector2(x,0));
        }
        private void OnTriggerEnter2D(Collider2D other){Touch(other);}
        private void OnTriggerStay2D(Collider2D other){Touch(other);}
        private void Touch(Collider2D other)
        {
            if(!game.Playing||health<=0)return;
            var shot=other.GetComponent<Fireball>();
            if(shot!=null){if(!shot.IsBullet){TakeDamage(1);Destroy(shot.gameObject);}return;}
            var player=other.GetComponent<PlayerMotor>();if(player==null)return;
            if(player.buffs.Value!=Buff.None){TakeDamage(MaxHealth);return;}
            if(player.body.linearVelocity.y<-.1f&&player.box.bounds.min.y>box.bounds.max.y-.25f)
            {TakeDamage(1);player.Bounce();}
            else player.Hit(false,"Bowser has rejected your entry request.");
        }
        public void TakeDamage(int amount)
        {
            if(!game.Playing||health<=0||amount<=0)return;
            health=Mathf.Max(0,health-amount);hitFlash=.12f;
            if(health>0){game.events.Sound("bump");return;}
            box.enabled=false;game.events.Defeat(id,1500);game.events.Sound("bowser-fall");
            game.level.Pickup(id+".coin",ItemKind.Coin,new Vector2(body.position.x,.7f));Destroy(gameObject);
        }
    }
}
