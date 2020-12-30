Shader "Ares/Rolling Clouds" {
	Properties {
		_Color ("Diffuse", COLOR) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_Ramp ("Ramp", 2D) = "white" {}
		_DispTex ("Disptex", 2D) = "white" {}
		_DispStrength ("Disp", Range(0,5)) = 1
//		_BumpStrength ("Bump", Range(0,1)) = 1
//		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_Flip ("Flip", Range(0,1)) = 0
		_Falloff ("Falloff", Range(1,20)) = 0
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Ramp vertex:vert
		
		float4 _Color;
		sampler2D _MainTex;
		//		sampler2D _BumpMap;
		sampler2D _Ramp;
		sampler2D _DispTex;
		float4 _DispTex_ST;
		float _DispStrength;
		//		float _BumpStrength;
		float _Flip;
		float _Falloff;

		half4 LightingRamp (SurfaceOutput s, half3 lightDir, half atten) {
			half NdotL = dot(s.Normal, lightDir);
			half diff = NdotL * 0.5 + 0.5;
			half3 ramp = tex2D(_Ramp, float2(diff, diff)).rgb;
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * ramp * atten;
//			c.rgb = ramp;
			c.a = s.Alpha;
			return c;
		}

		struct Input {
		  float2 uv_MainTex : TEXCOORD0;
//		  float2 uv_BumpMap;
//		  float2 uv_DispTex;
		};

		void vert(inout appdata_full v){
			// Normal code from from http://forum.unity3d.com/threads/computing-normals-in-vertex-shader.146254/#post-1001572
			// Obtain tangent space rotation matrix
            float3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
            float3x3 rotation = transpose(float3x3(v.tangent.xyz, binormal, v.normal));
           
            // Create two sample vectors (small +x/+y deflections from +z), put them in tangent space, normalize them, and halve the result.
            // This is equivalent to sampling neighboring vertex data since we're on a unit sphere.
            float3 v1 = normalize(mul(rotation, float3(0.1, 0, 1))) * 0.5;
            float3 v2 = normalize(mul(rotation, float3(0, 0.1, 1))) * 0.5;
            
			float d = pow(tex2Dlod(_DispTex, float4(v.texcoord.xy * _DispTex_ST.xy + _DispTex_ST.zw,0,0)).r, _Falloff);
			v.vertex.xyz += v.normal * lerp(d, 1 - d, _Flip) * _DispStrength;
			v1 += v.normal * lerp(d, 1 - d, _Flip) * _DispStrength;
			v2 += v.normal * lerp(d, 1 - d, _Flip) * _DispStrength;
//			v.normal += v.vertex.xyz;
//			v.vertex.xyz += dot(v.normal, tex2D(_DispTex, float4(v.texcoord.xy * _DispTex_ST.xy + _DispTex_ST.zw,0,0)).r * _DispStrength) + cross(v.normal, tex2D(_DispTex, float4(v.texcoord.xy * _DispTex_ST.xy + _DispTex_ST.zw,0,0)).r * _DispStrength);

			// Take the cross product of the sample-original positions, resulting in a dynamic normal
            float3 vn = cross(v2-v.vertex.xyz, v1-v.vertex.xyz);
           
            // Normalize
            v.normal = normalize(vn);
		}

		void surf(Input IN, inout SurfaceOutput o){
			float t = pow(tex2D(_MainTex, IN.uv_MainTex).r * 0.02, _Falloff+.5);
		
			o.Albedo = lerp(_Color.rgb, _Color.rgb * tex2D(_MainTex, IN.uv_MainTex).rgb, _Color.a);
			o.Emission = lerp(float3(0,0,0), float3(1,1,1), t);
		}
		ENDCG
	} 
	Fallback "Diffuse"
}