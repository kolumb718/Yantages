Shader "Orbita/WindowSurface"
{
    Properties { _MainTex ("Composed distant city", 2D) = "white" {} _CityVolume ("City span and depth", Vector) = (160,80,70,-4.26) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4x4 _WorldToWindow;
            float4 _WindowAperture, _CityVolume;
            struct v2f { float4 vertex:SV_POSITION; float3 window:TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);
                o.window=mul(_WorldToWindow,mul(unity_ObjectToWorld,v.vertex)).xyz;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 eye=mul(_WorldToWindow,float4(_WorldSpaceCameraPos,1)).xyz;
                float3 ray=i.window-eye;
                float distance=(_CityVolume.w-_CityVolume.z-eye.z)/min(-.001,ray.z);
                float2 horizon=(eye+ray*distance).xy;
                float2 centre=(_WindowAperture.xy+_WindowAperture.zw)*.5;
                float2 uv=(horizon-centre)/_CityVolume.xy+.5;
                float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
                fixed3 sky=lerp(fixed3(.65,.43,.38),fixed3(.20,.21,.34),saturate(uv.y));
                // The source camera faces -Z, so undo its horizontal image handedness.
                fixed3 city=tex2D(_MainTex,float2(1-saturate(uv.x),saturate(uv.y))).rgb;
                return fixed4(lerp(sky,city,saturate(edge*12)),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
