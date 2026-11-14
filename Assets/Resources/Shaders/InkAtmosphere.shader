Shader "Ganryujima/InkAtmosphere"
{
    Properties
    {
        _MainTex("Source",2D)="white"{}
        _Grain("Paper grain",Float)=0.018
        _Vignette("Vignette",Float)=0.24
        _Bloom("Glow",Float)=0.09
        _Saturation("Saturation",Float)=0.76
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Grain,_Vignette,_Bloom,_Saturation;
            fixed4 frag(v2f_img i):SV_Target
            {
                float3 color=tex2D(_MainTex,i.uv).rgb;
                float2 px=_MainTex_TexelSize.xy;
                float3 glow=0;
                glow+=max(0,tex2D(_MainTex,i.uv+px*float2(4,0)).rgb-0.8);
                glow+=max(0,tex2D(_MainTex,i.uv-px*float2(4,0)).rgb-0.8);
                glow+=max(0,tex2D(_MainTex,i.uv+px*float2(0,4)).rgb-0.8);
                glow+=max(0,tex2D(_MainTex,i.uv-px*float2(0,4)).rgb-0.8);
                color+=glow*_Bloom;
                float luma=dot(color,float3(0.2126,0.7152,0.0722));
                color=lerp(luma.xxx,color,_Saturation);
                color=(color-0.45)*1.06+0.45;
                color*=float3(1.025,1.005,0.965);
                float2 centered=i.uv*2-1;
                color*=1-_Vignette*dot(centered,centered)*0.55;
                float grain=frac(sin(dot(floor(i.uv/px),float2(12.9898,78.233)))*43758.5453)-0.5;
                color+=grain*_Grain;
                return fixed4(saturate(color),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
