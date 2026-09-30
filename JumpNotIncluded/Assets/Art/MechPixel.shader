Shader "JumpNotIncluded/MechPixel"
{
    Properties {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            float4 _MainTex_TexelSize;
            v2f vert(appdata_t v) {
                v2f o=SpriteVert(v);
                // Snap the complete sprite quad without stretching its logical pixels.
                float4 local=UnityFlipSprite(v.vertex,_Flip);
                float4 world=mul(unity_ObjectToWorld,local);
                world.xy=round(world.xy*16)/16;
                o.vertex=mul(UNITY_MATRIX_VP,world);
                return o;
            }
            fixed3 Palette(fixed3 c) {
                if(c.r>c.b*1.25 && c.g>c.b*1.1 && c.r>.35)return fixed3(1,.72,.15);
                if(c.b>c.r*1.35 && c.g>c.r*1.3 && c.b>.38)
                    return c.g>.5?fixed3(.30,.80,1):fixed3(.15,.36,.82);
                float l=dot(c,fixed3(.30,.59,.11));
                if(l<.14)return fixed3(.035,.055,.095);
                if(l<.40)return fixed3(.22,.30,.42);
                if(l<.65)return fixed3(.47,.56,.67);
                if(l<.86)return fixed3(.75,.82,.89);
                return fixed3(.96,.98,1);
            }
            fixed4 frag(v2f i):SV_Target {
                // Three by two cells, 52 logical pixels per cell. No soft edge pixels or glow.
                float2 cells=float2(3,2),logical=52;
                float2 cell=floor(i.texcoord*cells);
                float2 inside=frac(i.texcoord*cells);
                float2 uv=(cell+(floor(inside*logical)+.5)/logical)/cells;
                fixed4 c=SampleSpriteTexture(uv);
                clip(c.a-.88);
                return fixed4(Palette(c.rgb)*i.color.rgb,1);
            }
            ENDCG
        }
    }
}
