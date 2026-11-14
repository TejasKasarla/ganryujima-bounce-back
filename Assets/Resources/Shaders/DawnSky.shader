Shader "Ganryujima/DawnSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION) { v2f o; o.pos=UnityObjectToClipPos(vertex); o.direction=vertex.xyz; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            half4 frag(v2f i) : SV_Target
            {
                float3 d=normalize(i.direction);
                float elevation=saturate(d.y*2);
                float3 color=lerp(float3(0.89,0.81,0.65),float3(0.36,0.47,0.50),pow(elevation,0.6));
                float haze=exp(-abs(d.y)*10);
                color=lerp(color,float3(0.94,0.86,0.71),haze*0.5);
                float2 cloudUV=d.xz/(max(0.08,d.y+0.3))*float2(2,11);
                float clouds=noise(cloudUV)*0.65+noise(cloudUV*2.7)*0.35;
                float wisps=smoothstep(0.48,0.8,clouds)*smoothstep(0.02,0.2,d.y);
                color=lerp(color,float3(0.53,0.53,0.47),wisps*0.25);
                float sun=dot(d,normalize(float3(0.34,0.30,0.94)));
                color+=float3(0.4,0.13,0.015)*pow(saturate(sun),90)*0.3;
                float disk=smoothstep(0.9990,0.99925,sun);
                color=lerp(color,float3(1.35,0.39,0.16),disk);
                return half4(color,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
