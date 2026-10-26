// A sprite drawn as a flat shape in its vertex colour: the white flash on a hit, tinted afterimages.
Shader "Brawler/Silhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            fixed4 frag(v2f IN) : SV_Target
            {
                return fixed4(IN.color.rgb, SampleSpriteTexture(IN.texcoord).a * IN.color.a);
            }
            ENDCG
        }
    }
}
