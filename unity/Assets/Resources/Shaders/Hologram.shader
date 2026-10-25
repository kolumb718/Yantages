Shader "Orbita/Hologram"
{
    Properties { _Tint ("Tint", Color) = (0.18,0.73,1,0.25) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Tint;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float rim=pow(1-saturate(dot(normalize(i.normal),view)),2.0);
                float scan=0.55+0.45*sin(i.world.y*100-_Time.y*3);
                return fixed4(_Tint.rgb,_Tint.a*(0.18+rim*1.6)*scan);
            }
            ENDCG
        }
    }
}
