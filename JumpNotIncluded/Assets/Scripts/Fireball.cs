using UnityEngine;

namespace JumpNotIncluded
{
    public class Fireball : MonoBehaviour
    {
        public bool IsBullet {get;private set;}
        private SceneRoot game;private float age;private Rigidbody2D body;private int facing;
        public void Init(SceneRoot root,PlayerMotor player,bool bullet=false)
        {
            game=root;IsBullet=bullet;facing=player.facing;transform.position=(Vector3)player.body.position+new Vector3(facing*.9f,.1f,0);
            var sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=game.assets.solid;sprite.color=new Color(1,.45f,.13f);sprite.sortingOrder=9;
            transform.localScale=bullet?new Vector3(.6f,.13f,1):Vector3.one*.28f;
            var c=gameObject.AddComponent<CircleCollider2D>();c.radius=.5f;
            Physics2D.IgnoreCollision(c,player.box);
            body=gameObject.AddComponent<Rigidbody2D>();body.gravityScale=bullet?0:1;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            c.isTrigger=bullet;
            body.linearVelocity=bullet?new Vector2(facing*25,0):new Vector2(facing*11,1);
            if(bullet)game.level.PaveVictoryLane(player.body.position.x,facing);
        }
        private void Update(){if(game.Playing){age+=Time.deltaTime;if(age>2.5f)Destroy(gameObject);}}
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if(collision.GetContact(0).normal.y>.5f)body.linearVelocity=new Vector2(facing*11,5);
            else Destroy(gameObject);
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if(!IsBullet||!game.Playing)return;
            var enemy=other.GetComponent<EnemyActor>();if(enemy!=null){enemy.Defeat();return;}
            var boss=other.GetComponent<BossActor>();if(boss!=null){boss.TakeDamage(3);return;}
            var brick=other.GetComponent<BlockActor>();if(brick!=null){brick.BreakByBullet();return;}
            var flame=other.GetComponent<BossFlame>();if(flame!=null){Destroy(flame.gameObject);return;}
            if(other.gameObject.layer==8)Destroy(gameObject);
        }
    }
}
