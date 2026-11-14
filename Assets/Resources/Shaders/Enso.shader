Shader "Ganryujima/Enso"
{
    Properties
    {
        _Progress("Ink progress",Range(0,1))=0
        _Broken("Broken stroke",Range(0,1))=0
        _Complete("Complete",Range(0,1))=0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Progress,_Broken,_Complete;
            float hash(float2 p) { return frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453); }
            fixed4 frag(v2f_img i):SV_Target
            {
                float2 p=(i.uv-.5)*2;
                float r=length(p);
                float angle=frac((atan2(p.y,p.x)+3.14159265)/6.2831853+.125);
                float a=angle*6.2831853;
                float radius=.70+sin(a*3)*.018+sin(a*7)*.009;
                float width=.055+.032*(.5+.5*sin(a+1.2));
                float rough=(hash(floor(i.uv*640))-.5)*.02;
                float brush=1-smoothstep(width-.012,width+.009,abs(r-radius)+rough);
                float bristle=.70+.30*hash(float2(floor(r*430),floor(angle*55)));
                float stroke=brush*bristle*smoothstep(-.003,.008,_Progress-angle);
                float ghost=brush*.07;
                float broken=brush*step(.40,hash(float2(floor(angle*23),11)))*_Broken*.75;
                float paper=1-smoothstep(.91,1,r);
                float grain=(hash(floor(i.uv*640))-.5)*.026;
                float3 base=float3(.94,.90,.79)+grain;
                float3 ink=lerp(float3(.07,.10,.10),float3(.43,.29,.12),_Complete*.65);
                float pigment=saturate(max(stroke,ghost)+broken);
                if(_Broken>.01) pigment=lerp(pigment,broken+ghost,_Broken);
                float3 color=lerp(base,ink,pigment);
                return fixed4(color,paper*.94);
            }
            ENDCG
        }
    }
    Fallback Off
}
