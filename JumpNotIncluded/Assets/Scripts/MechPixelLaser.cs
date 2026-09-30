using System.Collections.Generic;
using UnityEngine;

namespace JumpNotIncluded
{
    // Rasterized on the same 16-pixel world grid as Mario and the terrain.
    // Each square has one opaque palette color: no smooth line edges or gradients.
    public sealed class MechPixelLaser : MonoBehaviour
    {
        public const float PixelSize=1f/16;
        public const float Lifetime=.22f;
        private static readonly Color32 White=new Color32(255,250,226,255);
        private static readonly Color32 Cyan=new Color32(85,221,255,255);
        private static readonly Color32 Blue=new Color32(36,97,201,255);
        private static readonly Color32 Navy=new Color32(21,63,130,255);
        private SceneRoot game;
        private MeshFilter filter;
        private Mesh[] frames;
        private Material material;
        private float age;
        private int displayedFrame;

        public void Init(SceneRoot root,Vector3 from,Vector3 to)
        {
            if(game!=null)return;
            game=root;transform.SetParent(root.transform,false);
            transform.position=Vector3.zero;transform.rotation=Quaternion.identity;
            var path=RasterLine(Cell(from),Cell(to));
            material=new Material(Shader.Find("Sprites/Default"))
            {name="Moon Killer / pixel palette",mainTexture=Texture2D.whiteTexture};
            filter=gameObject.AddComponent<MeshFilter>();
            var renderer=gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.sortingOrder=21;
            frames=new Mesh[3];
            for(int i=0;i<frames.Length;i++)frames[i]=BuildFrame(path,i);
            filter.sharedMesh=frames[0];
        }

        private static Vector2Int Cell(Vector3 position)=>
            new Vector2Int(Mathf.FloorToInt(position.x/PixelSize),Mathf.FloorToInt(position.y/PixelSize));

        private static List<Vector2Int> RasterLine(Vector2Int start,Vector2Int end)
        {
            var pixels=new List<Vector2Int>();
            int x=start.x,y=start.y,dx=Mathf.Abs(end.x-x),dy=-Mathf.Abs(end.y-y);
            int stepX=x<end.x?1:-1,stepY=y<end.y?1:-1,error=dx+dy;
            while(true)
            {
                pixels.Add(new Vector2Int(x,y));
                if(x==end.x&&y==end.y)break;
                int doubled=2*error;
                if(doubled>=dy){error+=dy;x+=stepX;}
                if(doubled<=dx){error+=dx;y+=stepY;}
            }
            return pixels;
        }

        private Mesh BuildFrame(List<Vector2Int> path,int frame)
        {
            var pixels=new Dictionary<Vector2Int,Color32>();
            // Dilating the one-pixel center by one cell yields a three-pixel blue edge.
            // Its brightness changes in steps before the final one-pixel afterglow.
            if(frame<2)
                foreach(var point in path)
                    for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                        pixels[point+new Vector2Int(x,y)]=frame==0?Blue:Navy;
            foreach(var point in path)pixels[point]=frame==0?White:frame==1?Cyan:Blue;

            // Five-pixel flash, three-pixel flash, then off. The target stays grid aligned.
            if(frame<2)
            {
                var target=path[path.Count-1];int radius=frame==0?2:1;
                for(int offset=-radius;offset<=radius;offset++)
                {
                    pixels[target+new Vector2Int(offset,0)]=Cyan;
                    pixels[target+new Vector2Int(0,offset)]=Cyan;
                }
                pixels[target]=White;
                if(frame==0)
                {
                    pixels[target+Vector2Int.left]=White;pixels[target+Vector2Int.right]=White;
                    pixels[target+Vector2Int.up]=White;pixels[target+Vector2Int.down]=White;
                }
            }

            var vertices=new Vector3[pixels.Count*4];
            var colors=new Color32[vertices.Length];var uv=new Vector2[vertices.Length];
            var triangles=new int[pixels.Count*6];int index=0;
            foreach(var pixel in pixels)
            {
                int vertex=index*4,triangle=index*6;
                float x=pixel.Key.x*PixelSize,y=pixel.Key.y*PixelSize;
                vertices[vertex]=transform.InverseTransformPoint(new Vector3(x,y,0));
                vertices[vertex+1]=transform.InverseTransformPoint(new Vector3(x+PixelSize,y,0));
                vertices[vertex+2]=transform.InverseTransformPoint(new Vector3(x+PixelSize,y+PixelSize,0));
                vertices[vertex+3]=transform.InverseTransformPoint(new Vector3(x,y+PixelSize,0));
                for(int corner=0;corner<4;corner++)
                {colors[vertex+corner]=pixel.Value;uv[vertex+corner]=Vector2.one*.5f;}
                triangles[triangle]=vertex;triangles[triangle+1]=vertex+1;triangles[triangle+2]=vertex+2;
                triangles[triangle+3]=vertex;triangles[triangle+4]=vertex+2;triangles[triangle+5]=vertex+3;
                index++;
            }
            var mesh=new Mesh{name="Moon Killer / pixel pulse "+frame};
            mesh.vertices=vertices;mesh.colors32=colors;mesh.uv=uv;mesh.triangles=triangles;
            mesh.RecalculateBounds();return mesh;
        }

        private void Update()
        {
            if(game==null||!game.Playing)return;
            age+=Time.deltaTime;
            if(age>=Lifetime){Destroy(gameObject);return;}
            int frame=age<.075f?0:age<.15f?1:2;
            if(frame!=displayedFrame){displayedFrame=frame;filter.sharedMesh=frames[frame];}
        }

        private void OnDestroy()
        {
            if(frames!=null)foreach(var frame in frames)if(frame!=null)Destroy(frame);
            if(material!=null)Destroy(material);
        }
    }
}
