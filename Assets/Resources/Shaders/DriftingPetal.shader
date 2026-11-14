Shader "Ganryujima/DriftingPetal"
{
    Properties { _MainTex("Petal", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata_full v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; o.color=v.color; UNITY_TRANSFER_FOG(o,o.pos); return o; }
            fixed4 frag(v2f i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv)*i.color; UNITY_APPLY_FOG(i.fogCoord,c); return c; }
            ENDCG
        }
    }
}
