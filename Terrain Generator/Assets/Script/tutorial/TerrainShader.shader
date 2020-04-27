/*reminder: the function has to be defined before it is being called*/
Shader "Custom/TerrainShader"
{
	Properties
	{

	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

	const static int maxColorNum = 8;
	int baseColorCount;//this is for knowing how many things there actually is
	float3 baseColor[maxColorNum];
	float3 baseStartHeight[maxColorNum];

	float minHeight;
	float maxHeight;

        struct Input
        {
            //float2 uv_MainTex;
			float3 worldPos;
        };

		float inverseLerp(float min, float max, float current) {
			//saturate means clamp it to 1 and 0
			return saturate((current - min) / (max - min));
		}

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
			/*this is going to be called for every pixel that our mesh is visible*/
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
			//o.Albedo = heightPercent;
			
			for (int i = 0; i < baseColorCount; i++) {
				float drawStrenght = saturate(sign(heightPercent - baseStartHeight[i]));//draw the color base on the height for each color

				o.Albedo = (o.Albedo * (1 - drawStrenght) + baseColor[i] * drawStrenght) * heightPercent;
			}
        }
        ENDCG
    }
    FallBack "Diffuse"
}

/*// Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;*/