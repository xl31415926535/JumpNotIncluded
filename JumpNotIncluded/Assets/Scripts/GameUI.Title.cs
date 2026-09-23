using UnityEngine;

namespace JumpNotIncluded
{
    public partial class GameUI
    {
        private Texture2D titleBurst;

        private void FreeToPlayBadge()
        {
            if(titleBurst==null)titleBurst=CreateTitleBurst();
            var previousMatrix=GUI.matrix;var previousColor=GUI.color;
            var badge=new Rect(1110,233,410,300);
            var pivot=new Vector3(badge.center.x,badge.center.y,0);
            GUI.matrix=previousMatrix*Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,-12),Vector3.one)*Matrix4x4.Translate(-pivot);
            GUI.color=new Color(0,0,0,.4f);
            GUI.DrawTexture(new Rect(badge.x+9,badge.y+13,badge.width,badge.height),titleBurst);
            GUI.color=Color.white;GUI.DrawTexture(badge,titleBurst);
            Text(1160,309,310,86,"FREE",68,new Color(.40f,.075f,.025f),true,TextAnchor.MiddleCenter);
            Text(1160,390,310,59,"TO PLAY",42,new Color(.40f,.075f,.025f),true,TextAnchor.MiddleCenter);
            GUI.color=previousColor;GUI.matrix=previousMatrix;
        }

        // Rasterize a vector burst once; keep the title graphic self-contained in the UI.
        private Texture2D CreateTitleBurst()
        {
            const int size=512,points=28;
            var vertices=new Vector2[points];
            float angleStep=2*Mathf.PI/points;
            for(int i=0;i<points;i++)
            {
                float radius=i%2==0?.97f:.70f;
                vertices[i]=new Vector2(Mathf.Cos(i*angleStep),Mathf.Sin(i*angleStep))*radius;
            }
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var position=new Vector2((x+.5f)*2/size-1,(y+.5f)*2/size-1);
                float angle=Mathf.Atan2(position.y,position.x);if(angle<0)angle+=2*Mathf.PI;
                int edgeIndex=Mathf.Min(points-1,Mathf.FloorToInt(angle/angleStep));
                var a=vertices[edgeIndex];var edge=vertices[(edgeIndex+1)%points]-a;
                var direction=position.normalized;
                float boundary=(a.x*edge.y-a.y*edge.x)/(direction.x*edge.y-direction.y*edge.x);
                float ratio=position.magnitude/boundary;
                if(ratio>1.005f)continue;
                Color color=ratio>.965f?new Color(.23f,.07f,.035f):
                    ratio>.865f?new Color(.83f,.16f,.035f):
                    ratio>.84f?new Color(1,.96f,.65f):
                    Color.Lerp(new Color(1,.72f,.12f),new Color(1,.94f,.36f),(position.y+1)*.5f);
                color.a=Mathf.Clamp01((1-ratio)*size*.5f+.5f);pixels[y*size+x]=color;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Free to play burst",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
    }
}
