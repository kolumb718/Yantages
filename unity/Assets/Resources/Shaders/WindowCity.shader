Shader "Orbita/WindowCity"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float4x4 _WorldToWindow;
            float4 _WindowAperture;
            struct v2f { float4 vertex : SV_POSITION; float2 window : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.window = mul(_WorldToWindow, mul(unity_ObjectToWorld, v.vertex)).xy;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Fixed aperture prevents moving buildings from spilling onto the walls.
                clip(i.window - _WindowAperture.xy);
                clip(_WindowAperture.zw - i.window);
                return _Color;
            }
            ENDCG
        }
    }
}
