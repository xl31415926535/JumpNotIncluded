using UnityEngine;

namespace JumpNotIncluded
{
    // Three opaque NES-style colors on the same 16 PPU grid as Mario. No gradients or smooth trails.
    public sealed class MechExhaust : MonoBehaviour
    {
        public const float PixelSize=1f/16;
        public bool Emitting=>root!=null&&root.gameObject.activeInHierarchy&&Power>0;
        public Vector2 Direction {get;private set;}=Vector2.down;
        public float Power {get;private set;}
        private const int MaxQuads=192;
        private Transform root;
        private Material material;
        private Mesh[] meshes;
        private Vector3[] vertices;
        private Color32[] colors;
        private Vector2[] uv;
        private int[] triangles;
        private bool visible=true;
        private float effectScale=1;
        private int quadCount;
        private Matrix4x4 worldToLocal;
        // This is deliberately a tiny opaque palette, matching hard-edged sprite pixels.
        private static readonly Color32 White=new Color32(244,255,255,255);
        private static readonly Color32 Cyan=new Color32(76,204,255,255);
        private static readonly Color32 Blue=new Color32(39,91,210,255);

        public void Init(Transform parent,int sortingOrder=20)
        {
            if(root!=null)return;
            // Unity objects are constructed here on the main thread, never in field initializers.
            material=new Material(Shader.Find("Sprites/Default")){name="Reaction drive / three-color pixel exhaust",mainTexture=Texture2D.whiteTexture};
            root=new GameObject("Reaction drive / 16 PPU sole exhaust").transform;root.SetParent(parent,false);
            meshes=new Mesh[2];vertices=new Vector3[MaxQuads*4];colors=new Color32[MaxQuads*4];
            uv=new Vector2[MaxQuads*4];triangles=new int[MaxQuads*6];
            for(int i=0;i<MaxQuads;i++)
            {
                int vertex=i*4,triangle=i*6;
                triangles[triangle]=vertex;triangles[triangle+1]=vertex+1;triangles[triangle+2]=vertex+2;
                triangles[triangle+3]=vertex;triangles[triangle+4]=vertex+2;triangles[triangle+5]=vertex+3;
                for(int p=0;p<4;p++)uv[vertex+p]=Vector2.one*.5f;
            }
            for(int side=0;side<2;side++)
            {
                var go=new GameObject(side==0?"Left sole / pixel reaction jet":"Right sole / pixel reaction jet");
                go.transform.SetParent(root,false);
                meshes[side]=new Mesh{name=go.name};meshes[side].MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh=meshes[side];
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=sortingOrder;
            }
            root.gameObject.SetActive(false);
        }

        /// <param name="time">Owner's clock; six discrete frames at 12 FPS freeze when this stops.</param>
        /// <param name="power">0 = off, .35 = hover, .7 = ascent, 1 = hard braking.</param>
        /// <param name="leftNozzle">Positions are local to the parent passed to Init.</param>
        /// <param name="direction">Direction of expelled material, opposite the thrust on the mech.</param>
        /// <param name="scale">Changes length in whole pixels; individual pixels stay exactly 1/16 world unit.</param>
        public void Render(float time,float power,Vector3 leftNozzle,Vector3 rightNozzle,Vector2 direction,float scale=1f)
        {
            if(root==null)return;
            Power=Finite(power)?Mathf.Clamp01(power):0;
            effectScale=Finite(scale)?Mathf.Max(0,scale):0;
            Direction=Finite(direction.x)&&Finite(direction.y)&&direction.sqrMagnitude>.0001f?direction.normalized:Vector2.down;
            root.gameObject.SetActive(visible&&Power>0&&effectScale>0);
            if(Power<=0||effectScale<=0)return;
            time=Finite(time)?Mathf.Max(0,time):0;
            int frame=Mathf.FloorToInt(Mathf.Repeat(time,.5f)*12)%6;
            int pulse=frame==1||frame==5?1:frame==3?-1:0;
            float length=(.04f+1.36f*Mathf.Pow(Power,1.35f))*effectScale;
            int rows=Mathf.Clamp(Mathf.RoundToInt(length/PixelSize)+pulse,3,48);
            worldToLocal=root.worldToLocalMatrix;
            Vector3 flow=root.TransformDirection(new Vector3(Direction.x,Direction.y,0)).normalized;
            Vector3 across=new Vector3(-flow.y,flow.x,0);
            for(int side=0;side<2;side++)
            {
                quadCount=0;
                Vector3 origin=root.TransformPoint(side==0?leftNozzle:rightNozzle);
                // Start half a pixel out of the sole. Grid coordinates keep it attached to the nozzle.
                origin+=flow*PixelSize*.5f;
                int denseRows=Mathf.Max(2,rows*3/4),whiteRows=Mathf.Max(2,rows/2);
                for(int row=0;row<denseRows;row++)
                {
                    int band=(row-frame+12)%6;
                    Color32 middle=row<2||row<whiteRows&&band<3?White:Cyan;
                    Pixel(origin+flow*(row*PixelSize),middle);
                    // Blocky shoulders and traveling side pixels stay within one pixel of the core.
                    // All motion is along the reaction direction; nothing fans out like sparks or smoke.
                    if(row<denseRows-1&&(row<2||band!=4))
                    {
                        float edge=(side==0?-1:1)*PixelSize;
                        Pixel(origin+flow*(row*PixelSize)+across*edge,band<3?Cyan:Blue);
                    }
                    if(Power>=.6f&&row<denseRows-2&&(row<2||band==1||band==2))
                        Pixel(origin+flow*(row*PixelSize)+across*(side==0?PixelSize:-PixelSize),Blue);
                }
                // One-dimensional ejected square packets, separated by at most a single pixel.
                // Their advancing pattern makes the solid white core visibly expel matter.
                for(int row=denseRows;row<rows;row++)
                {
                    int packet=(row-frame+18)%6;
                    if(packet==0||packet==3)continue;
                    Pixel(origin+flow*(row*PixelSize),packet==1||packet==4?Cyan:Blue);
                }
                var mesh=meshes[side];mesh.Clear(false);
                mesh.SetVertices(vertices,0,quadCount*4);mesh.SetColors(colors,0,quadCount*4);
                mesh.SetUVs(0,uv,0,quadCount*4);mesh.SetTriangles(triangles,0,quadCount*6,0,false);
                mesh.RecalculateBounds();
            }
        }

        private void Pixel(Vector3 center,Color32 color)
        {
            if(quadCount>=MaxQuads)return;
            // World-grid corners keep pixels exactly 1/16 even when a cinematic rig is scaled.
            float x=Mathf.Floor(center.x/PixelSize)*PixelSize,y=Mathf.Floor(center.y/PixelSize)*PixelSize;
            int start=quadCount*4;
            vertices[start]=worldToLocal.MultiplyPoint3x4(new Vector3(x,y,center.z));
            vertices[start+1]=worldToLocal.MultiplyPoint3x4(new Vector3(x+PixelSize,y,center.z));
            vertices[start+2]=worldToLocal.MultiplyPoint3x4(new Vector3(x+PixelSize,y+PixelSize,center.z));
            vertices[start+3]=worldToLocal.MultiplyPoint3x4(new Vector3(x,y+PixelSize,center.z));
            for(int i=0;i<4;i++)colors[start+i]=color;
            quadCount++;
        }
        public void SetVisible(bool value)
        {visible=value;if(root!=null)root.gameObject.SetActive(visible&&Power>0&&effectScale>0);}
        private static bool Finite(float number)=>!float.IsNaN(number)&&!float.IsInfinity(number);
        private void OnDisable(){if(root!=null)root.gameObject.SetActive(false);}
        private void OnDestroy()
        {
            if(root!=null)Destroy(root.gameObject);
            if(meshes!=null)foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            if(material!=null)Destroy(material);
        }
    }
}
