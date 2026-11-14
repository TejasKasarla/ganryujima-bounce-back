Shader "Ganryujima/CoastalWater"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(float4 vertex:POSITION) { v2f o; o.pos=UnityObjectToClipPos(vertex); o.world=mul(unity_ObjectToWorld,vertex).xyz; UNITY_TRANSFER_FOG(o,o.pos); return o; }
            half4 frag(v2f i):SV_Target
            {
                float2 p=i.world.xz;
                float wave=sin(p.x*1.7+p.y*0.35+_Time.y*0.6)*sin(p.y*0.8-_Time.y*0.3);
                float3 n=normalize(float3(cos(p.x*1.7+p.y*0.35+_Time.y*0.6)*0.035,1,cos(p.y*0.8-_Time.y*0.3)*0.045));
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float fresnel=pow(1-saturate(dot(view,n)),3);
                float3 color=lerp(float3(0.21,0.31,0.31),float3(0.69,0.71,0.64),fresnel);
                color+=wave*0.025;
                float reflection=pow(saturate(dot(reflect(-view,n),normalize(float3(0.34,0.30,0.94)))),100);
                color+=float3(0.85,0.45,0.15)*reflection;
                half4 output=half4(color,1);
                UNITY_APPLY_FOG(i.fogCoord,output);
                return output;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
