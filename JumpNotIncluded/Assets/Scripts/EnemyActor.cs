using UnityEngine;

namespace JumpNotIncluded
{
    public class EnemyActor : MonoBehaviour
    {
        public SceneRoot game;
        public string id;
        private SpriteRenderer sprite;
        private Rigidbody2D body;
        private float left,right,age,deathAge;
        private int direction=-1;
        public bool dead;
        public void Init(SceneRoot root,string key,float x,float min,float max)
        {
            game=root;id=key;left=min;right=max;transform.position=new Vector3(x,0,0);
            sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=game.assets.Sprite("goomba0");sprite.sharedMaterial=game.assets.blueKey;sprite.sortingOrder=5;
            var collider=gameObject.AddComponent<BoxCollider2D>();collider.size=new Vector2(.84f,1);collider.offset=Vector2.up*.5f;collider.isTrigger=true;
            body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            body.interpolation=RigidbodyInterpolation2D.Interpolate;
        }
        private void Update()
        {
            if(!game.Playing)return;
            age+=Time.deltaTime;
            if(dead){deathAge+=Time.deltaTime;if(deathAge>.3f)Destroy(gameObject);return;}
            sprite.sprite=game.assets.Sprite("goomba"+(Mathf.FloorToInt(age*6)%2));
        }
        private void FixedUpdate()
        {
            if(!game.Playing||dead)return;
            float x=body.position.x+direction*1.15f*Time.fixedDeltaTime;
            if(x<left){x=left;direction=1;}if(x>right){x=right;direction=-1;}
            body.MovePosition(new Vector2(x,0));
        }
        private void OnTriggerEnter2D(Collider2D other){Touch(other);}
        private void OnTriggerStay2D(Collider2D other){Touch(other);}
        private void Touch(Collider2D other)
        {
            if(dead||!game.Playing)return;
            var fireball=other.GetComponent<Fireball>();
            if(fireball!=null){if(!fireball.IsBullet){Defeat();Destroy(fireball.gameObject);}return;}
            var player=other.GetComponent<PlayerMotor>();if(player==null)return;
            if(player.buffs.Value!=Buff.None){Defeat();return;}
            if(player.body.linearVelocity.y<-.1f && player.box.bounds.min.y>GetComponent<Collider2D>().bounds.max.y-.22f)
            {Defeat();player.Bounce();}else player.Hit();
        }
        public void Defeat()
        {
            if(dead)return;dead=true;GetComponent<Collider2D>().enabled=false;
            sprite.sprite=game.assets.Sprite("goomba-dead");
            game.events.Defeat(id,200);
        }
    }
}
