using UnityEngine;

namespace JumpNotIncluded
{
    public class BossFlame : MonoBehaviour
    {
        private SceneRoot game;private Rigidbody2D body;private float age;
        public void Init(SceneRoot root,Vector2 position,Vector2 direction)
        {
            game=root;transform.position=position;
            var sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=game.assets.Sprite("bowser-flame");sprite.sharedMaterial=game.assets.blueKey;sprite.sortingOrder=8;
            sprite.flipX=direction.x>0;transform.localScale=Vector3.one*.8f;
            var box=gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(1.2f,.38f);
            body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            body.linearVelocity=direction*5.5f;
        }
        private void Update(){if(game.Playing){age+=Time.deltaTime;if(age>4)Destroy(gameObject);}}
        private void OnTriggerEnter2D(Collider2D other)
        {
            if(!game.Playing)return;
            var player=other.GetComponent<PlayerMotor>();
            if(player!=null){player.Hit(false,"Bowser brought the heat.");Destroy(gameObject);}
            else if(other.gameObject.layer==8)Destroy(gameObject);
        }
    }
}
