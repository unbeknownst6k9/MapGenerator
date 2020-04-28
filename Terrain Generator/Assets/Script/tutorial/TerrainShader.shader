/*reminder: the function has to be defined before it is being called*/
Shader "Custom/TerrainShader"
{
	Properties
	{
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
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

	const static int maxLayerNum = 8;
	const static float epsilon = 0.0001;
	int layerCount;//this is for knowing how many things there actually is
	float3 baseColor[maxLayerNum];
	float baseStartHeight[maxLayerNum];
	float baseBlend[maxLayerNum];
	float baseColorStrength[maxLayerNum];
	float baseTextureScale[maxLayerNum];

	UNITY_DECLARE_TEX2DARRAY(baseTextureArray);//this is to declare the texture 2D array

	float minHeight;
	float maxHeight;

	sampler2D testTexture;
	float testScale;

        struct Input
        {
            //float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
        };

		float inverseLerp(float min, float max, float current) {
			//saturate means clamp it to 1 and 0
			return saturate((current - min) / (max - min));
		}

		float3 triplanar(float3 worldPos, float3 scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;

			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextureArray, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextureArray, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextureArray, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;

			return (xProjection + yProjection + zProjection);
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
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
			
			for (int i = 0; i < layerCount; i++) {
				float drawStrenght = inverseLerp(-baseBlend[i] / 2 - epsilon, baseBlend[i] / 2, heightPercent - baseStartHeight[i]);//draw the color base on the height for each color
				float3 baseColorRef = baseColor[i] * baseColorStrength[i];
				float3 textureColor = triplanar(IN.worldPos, baseTextureScale[i], blendAxes, i) * (1- baseColorStrength[i]);
				o.Albedo = (o.Albedo * (1 - drawStrenght) + (baseColorRef + textureColor) * drawStrenght);
			}

        }
        ENDCG
    }
    FallBack "Diffuse"
}

