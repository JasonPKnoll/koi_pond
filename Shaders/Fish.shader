// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Timelincoln/Fish"
{
	Properties
	{
		_Color("Color", Color) = (0,0,0,0)
		_wiggliness("wiggliness", Float) = 0
		_wigglinessbyspeed("wiggliness by speed", Float) = 0
		_movementwigglenoise("movement wiggle noise", 2D) = "white" {}
		_fishwigglenoise("fish wiggle noise", 2D) = "white" {}
		_Angle("Angle", Float) = 15.77
		_Scale(" Scale", Float) = 7.86
		_Color2("Color2", Color) = (0,0,0,0)
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
		};

		uniform sampler2D _movementwigglenoise;
		uniform float _wigglinessbyspeed;
		uniform sampler2D _fishwigglenoise;
		uniform float _wiggliness;
		uniform float4 _Color2;
		uniform float _Scale;
		uniform float _Angle;
		uniform float4 _Color;
		uniform float _Metallic;
		uniform float _Smoothness;


		float2 voronoihash46( float2 p )
		{
			
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi46( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
		{
			float2 n = floor( v );
			float2 f = frac( v );
			float F1 = 8.0;
			float F2 = 8.0; float2 mg = 0;
			for ( int j = -1; j <= 1; j++ )
			{
				for ( int i = -1; i <= 1; i++ )
			 	{
			 		float2 g = float2( i, j );
			 		float2 o = voronoihash46( n + g );
					o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
					float d = 0.5 * dot( r, r );
			 		if( d<F1 ) {
			 			F2 = F1;
			 			F1 = d; mg = g; mr = r; id = o;
			 		} else if( d<F2 ) {
			 			F2 = d;
			
			 		}
			 	}
			}
			return F1;
		}


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float4 appendResult27 = (float4(ase_worldPos.x , ase_worldPos.z , 0.0 , 0.0));
			float2 panner29 = ( _Time.y * float2( 0,0 ) + ( appendResult27 * _wigglinessbyspeed ).xy);
			float3 ase_vertex3Pos = v.vertex.xyz;
			float4 appendResult8 = (float4(ase_vertex3Pos.x , ase_vertex3Pos.z , 0.0 , 0.0));
			float2 panner22 = ( _SinTime.w * float2( 0,0 ) + ( appendResult8 * _wiggliness * _SinTime.w ).xy);
			float4 FishyMovement70 = ( tex2Dlod( _movementwigglenoise, float4( panner29, 0, 0.0) ) * tex2Dlod( _fishwigglenoise, float4( panner22, 0, 0.0) ) );
			v.vertex.xyz += FishyMovement70.rgb;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float time46 = _Angle;
			float2 voronoiSmoothId46 = 0;
			float2 coords46 = i.uv_texcoord * _Scale;
			float2 id46 = 0;
			float2 uv46 = 0;
			float voroi46 = voronoi46( coords46, time46, id46, uv46, 0, voronoiSmoothId46 );
			float simplePerlin2D53 = snoise( i.uv_texcoord );
			simplePerlin2D53 = simplePerlin2D53*0.5 + 0.5;
			float4 FishyColors63 = ( ( _Color2 * _Color2 * ( 1.0 - voroi46 ) ) + ( voroi46 * simplePerlin2D53 * _Color * _Color ) );
			o.Albedo = FishyColors63.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
413;-855;1274;488;498.8872;386.7077;1;True;True
Node;AmplifyShaderEditor.CommentaryNode;69;-1871.043,466.1484;Inherit;False;1340.329;593.5576;Comment;9;18;11;21;8;38;6;22;37;23;Fish Movement;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;65;-1857.078,-247.6504;Inherit;False;1161.564;583.1008;Comment;7;34;27;32;28;31;29;36;World Movement;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;60;-1529.933,-1367.15;Inherit;False;1316.143;656.4312;Comment;11;2;47;59;55;56;58;53;46;49;50;42;Fishy Colors;1,1,1,1;0;0
Node;AmplifyShaderEditor.PosVertexDataNode;18;-1821.043,516.1483;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;34;-1807.079,-197.6504;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;32;-1642.35,44.50811;Inherit;False;Property;_wigglinessbyspeed;wiggliness by speed;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;42;-1459.309,-1310.062;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;49;-1479.933,-1172.545;Inherit;False;Property;_Angle;Angle;5;0;Create;True;0;0;0;False;0;False;15.77;15.77;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;50;-1474.61,-1100.699;Inherit;False;Property;_Scale; Scale;6;0;Create;True;0;0;0;False;0;False;7.86;7.86;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;27;-1569.72,-142.6011;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SinTimeNode;21;-1599.422,739.6125;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;8;-1457.363,563.494;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-1615.993,664.903;Inherit;False;Property;_wiggliness;wiggliness;1;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;31;-1421.142,143.0813;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-1405.3,31.23261;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SinTimeNode;38;-1308.613,876.7059;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-1282.877,646.73;Inherit;False;3;3;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.VoronoiNode;46;-1210.192,-1157.085;Inherit;False;0;0;1;0;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.OneMinusNode;55;-830.7617,-1114.945;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;53;-1180.799,-1276.17;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-1028.383,-922.7178;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;22;-1122.144,633.356;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;29;-1216.514,92.77607;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;58;-837.2306,-1317.15;Inherit;False;Property;_Color2;Color2;7;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;36;-1015.514,105.4504;Inherit;True;Property;_movementwigglenoise;movement wiggle noise;3;0;Create;True;0;0;0;False;0;False;-1;830a59df67f68644fb695f46a791f972;830a59df67f68644fb695f46a791f972;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;37;-850.7141,582.2536;Inherit;True;Property;_fishwigglenoise;fish wiggle noise;4;0;Create;True;0;0;0;False;0;False;-1;830a59df67f68644fb695f46a791f972;830a59df67f68644fb695f46a791f972;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-541.0452,-920.4626;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;-527.3625,-1078.263;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-348.7187,293.6891;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;59;-359.0164,-1000.961;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;63;-104.1089,-1004.289;Inherit;False;FishyColors;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;70;-150.4511,286.263;Inherit;False;FishyMovement;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;71;69.95563,57.24663;Inherit;False;70;FishyMovement;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;68;-77.12894,-84.34072;Inherit;False;Property;_Smoothness;Smoothness;9;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;23;-1338.639,788.634;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;64;70.36829,-233.191;Inherit;False;63;FishyColors;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;67;-166.5717,-161.4755;Inherit;False;Property;_Metallic;Metallic;8;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;310.9482,-226.8892;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Timelincoln/Fish;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;27;0;34;1
WireConnection;27;1;34;3
WireConnection;8;0;18;1
WireConnection;8;1;18;3
WireConnection;28;0;27;0
WireConnection;28;1;32;0
WireConnection;6;0;8;0
WireConnection;6;1;11;0
WireConnection;6;2;21;4
WireConnection;46;0;42;0
WireConnection;46;1;49;0
WireConnection;46;2;50;0
WireConnection;55;0;46;0
WireConnection;53;0;42;0
WireConnection;22;0;6;0
WireConnection;22;1;38;4
WireConnection;29;0;28;0
WireConnection;29;1;31;0
WireConnection;36;1;29;0
WireConnection;37;1;22;0
WireConnection;47;0;46;0
WireConnection;47;1;53;0
WireConnection;47;2;2;0
WireConnection;47;3;2;0
WireConnection;56;0;58;0
WireConnection;56;1;58;0
WireConnection;56;2;55;0
WireConnection;35;0;36;0
WireConnection;35;1;37;0
WireConnection;59;0;56;0
WireConnection;59;1;47;0
WireConnection;63;0;59;0
WireConnection;70;0;35;0
WireConnection;0;0;64;0
WireConnection;0;3;67;0
WireConnection;0;4;68;0
WireConnection;0;11;71;0
ASEEND*/
//CHKSM=CEFE3837E18DA94F18118F219F274C7A5B4F8FB6