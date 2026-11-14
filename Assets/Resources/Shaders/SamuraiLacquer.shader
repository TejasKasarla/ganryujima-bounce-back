Shader "Ganryujima/SamuraiLacquer"
{
    Properties
    {
        _MainTex("Albedo",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _ArmorTint("Lacquer dye",Color)=(.8,.8,.7,1)
        _BumpMap("Normal",2D)="bump"{}
        _MetallicGlossMap("Metallic smoothness",2D)="white"{}
        _GlossMapScale("Smoothness",Range(0,1))=.72
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex,_BumpMap,_MetallicGlossMap;
        fixed4 _Color,_ArmorTint; half _GlossMapScale;
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; };
        void surf(Input i,inout SurfaceOutputStandard o)
        {
            fixed3 c=tex2D(_MainTex,i.uv_MainTex).rgb;
            // Restrict dye to saturated crimson; warm skin and brass must keep their original maps.
            float mask=smoothstep(.015,.09,c.r-c.g*2.3)*smoothstep(.015,.09,c.r-c.b*2.7);
            c=lerp(c,_ArmorTint.rgb*(.5+dot(c,float3(.3,.6,.1))*1.7),mask);
            fixed4 packed=tex2D(_MetallicGlossMap,i.uv_MainTex);
            o.Albedo=c*_Color.rgb; o.Normal=UnpackNormal(tex2D(_BumpMap,i.uv_BumpMap));
            o.Metallic=packed.r; o.Smoothness=packed.a*_GlossMapScale; o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
