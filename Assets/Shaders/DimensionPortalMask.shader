Shader "Custom/DimensionPortalMask"
{
    Properties
    {
        _StencilRef ("Stencil ID", Int) = 1
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry-1"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        ColorMask 0
        ZWrite Off
        Cull Off

        Pass
        {
            Name "PortalMaskPass"

            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
