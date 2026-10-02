using System.Collections.Generic;
using UnityEngine;

namespace JumpNotIncluded
{
    // Original procedural castle art: the same logical 16 pixels per world unit as Mario.
    // Textures have no filtering, gradients, lighting dependency or imported asset lifetime.
    public static class CastleArt
    {
        public const int PixelsPerUnit=16;
        private static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        private static readonly Color32 Ink=new Color32(22,18,29,255),Mortar=new Color32(37,31,45,255),
            Stone=new Color32(84,74,97,255),Edge=new Color32(139,127,150,255),
            Red=new Color32(179,28,26,255),Orange=new Color32(247,78,25,255),
            Gold=new Color32(255,179,39,255),White=new Color32(255,235,148,255);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {Release();Application.quitting-=Release;Application.quitting+=Release;}

        public static Sprite Sprite(string key,int frame=0)
        {
            frame=((frame%4)+4)%4;
            string name=key+"."+frame;
            if(cache.TryGetValue(name,out var existing)&&existing!=null)return existing;
            int w=key=="crusher"?32:key=="fire"?12:key=="chain"?8:key=="shard"||key=="crack"?4:16;
            int h=key=="crusher"||key=="window"||key=="banner"||key=="jet"?32:key=="fire"?12:key=="shard"?4:key=="crack"?8:16;
            var pixels=new Color32[w*h];
            void P(int x,int y,Color32 c){if(x>=0&&x<w&&y>=0&&y<h)pixels[(h-1-y)*w+x]=c;}
            void R(int x,int y,int width,int height,Color32 c)
            {for(int yy=y;yy<y+height;yy++)for(int xx=x;xx<x+width;xx++)P(xx,yy,c);}
            if(key=="brick")
            {
                R(0,0,w,h,Mortar);
                for(int row=0;row<2;row++)
                {
                    int shift=row==0?0:8;
                    for(int col=-1;col<2;col++)
                    {
                        int x=col*16+shift,y=row*8;
                        R(x+1,y+1,14,6,Stone);R(x+1,y+1,14,1,Edge);
                        R(x+1,y+2,1,4,Edge);R(x+2,y+6,13,1,Ink);
                        P(x+6,y+3,Mortar);P(x+12,y+5,Mortar);
                    }
                }
            }
            else if(key=="gate")
            {
                R(0,0,w,h,Ink);R(1,1,14,14,Mortar);R(2,2,12,12,Stone);
                R(2,2,12,2,Edge);R(2,4,2,10,Edge);R(12,4,2,10,Ink);R(4,12,8,2,Ink);
                R(6,4,4,8,Ink);R(7,5,2,6,Red);P(7,6,Orange);P(8,9,Gold);
                P(3,3,White);P(12,3,Edge);P(3,12,Edge);P(12,12,Edge);
            }
            else if(key=="lava")
            {
                R(0,0,w,h,Red);
                for(int x=0;x<w;x++)
                {
                    int wave=((x+frame*4)%16)/4;
                    int top=wave==0?2:wave==2?0:1;
                    R(x,top,1,2,White);R(x,top+2,1,3,Gold);R(x,top+5,1,3,Orange);
                    if((x+frame*3)%9<3)R(x,11,1,2,Orange);
                    for(int y=0;y<top;y++)P(x,y,new Color32(0,0,0,0));
                }
            }
            else if(key=="fire")
            {
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    int d=(x-5)*(x-5)+(y-5)*(y-5);
                    if(d<31)P(x,y,d<6?White:d<15?Gold:Orange);
                }
                R(frame%2==0?7:2,1,3,2,Red);R(frame%2==0?1:8,8,2,3,Gold);
            }
            else if(key=="crusher")
            {
                R(2,2,28,28,Ink);R(4,4,24,24,Stone);R(4,4,24,2,Edge);R(4,6,2,20,Edge);
                for(int i=0;i<4;i++)
                {
                    int p=3+i*7;R(p,0,5,3,Edge);R(p+1,30,3,2,Edge);
                    R(0,p,3,5,Edge);R(30,p+1,2,3,Stone);
                }
                R(7,9,7,7,Ink);R(18,9,7,7,Ink);R(9,12,3,3,Orange);R(20,12,3,3,Orange);
                for(int i=0;i<7;i++){P(7+i,8+i/2,Ink);P(18+i,11-i/2,Ink);}
                R(10,20,12,6,Ink);R(11,20,2,2,White);R(15,20,2,2,White);R(19,20,2,2,White);
                R(12,24,2,2,Edge);R(18,24,2,2,Edge);R(8,17,3,1,Mortar);R(21,17,3,1,Mortar);
            }
            else if(key=="chain")
            {
                for(int y=0;y<h;y+=8){R(2,y,4,7,Ink);R(3,y+1,2,5,Edge);R(3,y+2,1,3,Mortar);}
            }
            else if(key=="window")
            {
                R(2,6,12,26,Mortar);R(4,3,8,29,Mortar);R(6,1,4,31,Mortar);
                R(4,7,8,25,Ink);R(6,4,4,28,Ink);R(6,9,4,21,Red);R(7,12,2,16,Orange);
                R(7,5,2,27,Stone);R(4,17,8,2,Stone);R(2,30,12,2,Edge);
            }
            else if(key=="banner")
            {
                R(1,1,14,2,Edge);R(3,3,10,25,Red);R(3,3,1,25,Orange);R(12,3,1,25,Ink);
                R(4,28,8,1,Red);R(5,29,6,1,Red);R(6,30,4,1,Red);
                R(6,10,4,6,Gold);R(5,12,6,2,Gold);R(7,9,2,9,White);
            }
            else if(key=="jet")
            {
                for(int y=1;y<h;y++)
                {
                    int half=y<8?1+y/3:4;
                    int mid=7+((y/4+frame)%3-1);
                    R(mid-half,y,half*2+1,1,Red);R(mid-half+1,y,Mathf.Max(1,half*2-1),1,Orange);
                    if(y>8)R(mid-1,y,3,1,Gold);if(y>16)P(mid,y,White);
                }
            }
            else if(key=="shard")
            {R(0,0,4,4,Stone);R(0,0,4,1,Edge);R(0,0,1,3,Edge);P(3,3,Ink);}
            else if(key=="crack")
            {
                for(int y=0;y<8;y++){int x=y/2%2;R(x,y,3,1,Orange);P(x+1,y,White);}
            }
            else
            {R(0,0,w,h,Stone);}
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false)
            {name="Castle / "+name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            var result=UnityEngine.Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),PixelsPerUnit,0,SpriteMeshType.FullRect);
            result.name=texture.name;result.hideFlags=HideFlags.HideAndDontSave;cache[name]=result;return result;
        }

        public static void Release()
        {
            foreach(var value in cache.Values)if(value!=null)
            {
                var texture=value.texture;
                if(Application.isPlaying){Object.Destroy(value);if(texture!=null)Object.Destroy(texture);}
                else {Object.DestroyImmediate(value);if(texture!=null)Object.DestroyImmediate(texture);}
            }
            cache.Clear();
        }
    }
}
