using UnityEngine;

namespace JumpNotIncluded
{
    public class EnemyActor : MonoBehaviour
    {
        public SceneRoot game;
        public string id;
        private SpriteRenderer sprite;
        private Rigidbody2D body;
        private BoxCollider2D box;
        private ContactFilter2D terrain;
        private readonly RaycastHit2D[] hits=new RaycastHit2D[4];
        private const float Skin=.005f;
        private float age,deathAge,fallSpeed;
        private bool active;
        private int direction=-1;
        public bool dead;
        public void Init(SceneRoot root,string key,float x)
        {
            game=root;id=key;transform.position=new Vector3(x,0,0);
            sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=game.assets.Sprite("goomba0");sprite.sharedMaterial=game.assets.blueKey;sprite.sortingOrder=5;
            box=gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(.84f,1);box.offset=Vector2.up*.5f;box.isTrigger=true;
            body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            body.interpolation=RigidbodyInterpolation2D.Interpolate;
            terrain.SetLayerMask(1<<8);terrain.useTriggers=false;
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
            // Start when the camera reaches us, then keep walking even off screen.
            if(!active)
            {
                float halfWidth=game.cameraView.orthographicSize*game.cameraView.aspect;
                if(Mathf.Abs(body.position.x-game.cameraView.transform.position.x)>halfWidth+box.size.x*.5f)return;
                active=true;
            }
            Vector2 position=body.position;
            float step=1.15f*Time.fixedDeltaTime;
            var wall=CastTerrain(position,Vector2.right*direction,step);
            position.x+=direction*(wall.collider!=null?Mathf.Max(0,wall.distance-Skin):step);
            if(wall.collider!=null)direction=-direction;
            // Unsupported Goombas fall into gaps; only actual terrain turns them around.
            fallSpeed=Mathf.Min(fallSpeed-Physics2D.gravity.y*3.2f*Time.fixedDeltaTime,24);
            float drop=fallSpeed*Time.fixedDeltaTime;
            var floor=CastTerrain(position,Vector2.down,drop);
            position.y-=floor.collider!=null?Mathf.Max(0,floor.distance-Skin):drop;
            if(floor.collider!=null)fallSpeed=0;
            body.MovePosition(position);
            if(position.y<-5)Destroy(gameObject);
        }
        private RaycastHit2D CastTerrain(Vector2 position,Vector2 movement,float distance)
        {
            int count=Physics2D.BoxCast(position+box.offset,box.size-Vector2.one*(Skin*2),0,movement,terrain,hits,distance+Skin);
            for(int i=0;i<count;i++)
                if(Vector2.Dot(hits[i].normal,movement)<-.5f)return hits[i];
            return default;
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
