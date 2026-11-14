Shader "Ganryujima/PineNeedles"
{
    Properties { _MainTex("Needle spray",2D)="white"{} _Color("Tint",Color)=(0.3,0.4,0.3,1) _Cutoff("Cutoff",Range(0,1))=0.35 }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff addshadow fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            fixed4 needle=tex2D(_MainTex,IN.uv_MainTex);
            o.Albedo=needle.rgb*_Color.rgb;
            o.Alpha=needle.a;
            o.Smoothness=0.05;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
