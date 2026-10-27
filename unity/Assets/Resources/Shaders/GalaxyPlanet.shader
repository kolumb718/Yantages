Shader "Orbita/GalaxyPlanet"
{
    Properties
    {
        _Ocean ("Ocean", Color) = (.2,.4,.5,1)
        _Land ("Land", Color) = (.7,.7,.4,1)
        _Style ("Biome", Float) = 0
        _Seed ("World seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Ocean, _Land;
            float _Style, _Seed;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float3 p:TEXCOORD0; float3 n:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.p=v.vertex.xyz*2; o.n=normalize(UnityObjectToWorldNormal(v.normal)); return o;
            }
            float terrain(float3 p)
            {
                return sin(p.x*7+_Seed)*cos(p.y*6+sin(p.z*5))*.5
                    +sin(p.z*13+p.x*3)*cos(p.y*11)*.25+sin(p.x*23-p.z*17)*.08;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 p=normalize(i.p);
                float n=terrain(p), land=smoothstep(-.04,.08,n);
                if(_Style>3.5 && _Style<4.5) land=smoothstep(.34,.42,n);
                if(_Style>1.5 && _Style<2.5) land=smoothstep(-.25,-.05,n);
                fixed3 color=lerp(_Ocean.rgb,_Land.rgb,land);
                if(_Style>.5 && _Style<1.5)
                {
                    float bands=sin(p.y*27+sin(p.x*5+p.z*9)*.6)*.5+.5;
                    color=lerp(_Ocean.rgb,_Land.rgb,bands*.85);
                }
                if(_Style>2.5 && _Style<3.5)
                {
                    color=lerp(_Ocean.rgb,_Land.rgb,saturate(n*.65+.6));
                    color+=smoothstep(.94,.99,sin(p.y*35+n*2))*.09;
                }
                if(_Style>4.5) color=lerp(_Ocean.rgb,_Land.rgb,smoothstep(-.2,.25,n));
                float clouds=smoothstep(.65,.89,sin(p.x*14+p.z*5)*cos(p.y*17+sin(p.z*7)));
                color=lerp(color,fixed3(.95,.88,.78),clouds*(_Style>4.5?.65:.22));
                float diffuse=max(0,dot(normalize(i.n),normalize(float3(-.5,.65,-1))));
                float rim=pow(1-saturate(dot(normalize(i.n),float3(0,0,-1))),3);
                return fixed4(color*(.3+diffuse*.8)+_Land.rgb*rim*.13,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
