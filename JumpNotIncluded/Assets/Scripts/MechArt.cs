using UnityEngine;

namespace JumpNotIncluded
{
    // The generated sheet is an enlarged painting of pixel cells. The shader samples
    // a fixed logical grid and a small palette, yielding exactly Mario's 16 world PPU.
    public static class MechArt
    {
        public const int Grid=52,PixelsPerUnit=16,Hover=3;
        private static readonly Vector2[] Feet={new Vector2(28,48),new Vector2(28,48),new Vector2(28,48),
            new Vector2(34,44),new Vector2(32,42),new Vector2(31,42)};
        private static readonly Rect[] Visible={new Rect(16,15,19,33),new Rect(15,15,22,33),new Rect(17,15,22,33),
            new Rect(7,10,35,34),new Rect(8,12,36,30),new Rect(10,10,33,32)};
        public static Sprite[] CreateFrames(GameAssets assets)
        {
            var texture=assets.mechPixelAtlas;float cell=texture.width/3f;
            var result=new Sprite[6];
            for(int i=0;i<6;i++)
            {
                result[i]=Sprite.Create(texture,new Rect(i%3*cell,texture.height-(i/3+1)*cell,cell,cell),
                    new Vector2(Feet[i].x/Grid,1-Feet[i].y/Grid),cell/Grid*PixelsPerUnit,0,SpriteMeshType.FullRect);
                result[i].name="War God / 8-bit pose "+i;
            }
            return result;
        }
        private static Vector3 Point(int frame,float x,float y)=>
            new Vector3((x-Feet[frame].x)/PixelsPerUnit,(Feet[frame].y-y)/PixelsPerUnit,0);
        public static Vector3 SoleLocal(int frame,bool second)
        {
            if(frame==4)return second?Point(frame,30,38):Point(frame,27,42);
            if(frame==5)return second?Point(frame,29,38):Point(frame,24.5f,42);
            return second?Point(frame,36,44):Point(frame,32.5f,44);
        }
        public static Vector3 RearLocal(int frame,bool second)
        {
            // Left boundary of each rear wingtip, sampled on the shader's 52-cell grid.
            // The half-cell Y anchors the jet to that edge pixel's center; emitting left
            // starts outside the opaque armor instead of hiding the plume behind it.
            if(frame==5)return second?Point(frame,11,24.5f):Point(frame,10,15.5f);
            return second?Point(frame,8,27.5f):Point(frame,10,19.5f);
        }
        public static Vector3 CoreLocal(int frame)=>frame==3?Point(frame,38,24.5f):
            frame>=4?Point(frame,frame==4?40.5f:39,26.5f):Point(frame,31.5f,29.5f);
        public static Bounds VisibleBounds(SpriteRenderer hull,int frame)
        {
            if(hull==null)return new Bounds();
            Rect rect=Visible[Mathf.Clamp(frame,0,5)];
            var a=Point(frame,rect.xMin,rect.yMax);var b=Point(frame,rect.xMax,rect.yMin);
            if(hull.flipX){a.x=-a.x;b.x=-b.x;}
            Vector3 pa=hull.transform.TransformPoint(a),pb=hull.transform.TransformPoint(b);
            return new Bounds((pa+pb)*.5f,new Vector3(Mathf.Abs(pa.x-pb.x),Mathf.Abs(pa.y-pb.y),.01f));
        }
    }
}
