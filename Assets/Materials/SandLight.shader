Shader "Custom/SandLight"
{
    Properties
    {
        _Color ("Color", Color) = (0.8, 0.7, 0.5, 1)
        _NoiseScale ("Noise Scale", Range(0.1, 50)) = 10.0
        _NoiseDetail ("Noise Detail", Range(0, 5)) = 2.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
        _Metallic ("Metallic", Range(0, 1)) = 0.1
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
            "Queue"="Geometry"
        }
        
        // Основной проход с освещением
        Pass
        {
            Tags { 
                "LightMode" = "ForwardBase"
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            float4 _Color;
            float _NoiseScale;
            float _NoiseDetail;
            float _Smoothness;
            float _Metallic;
            float _ShadowStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                SHADOW_COORDS(3) // Для теней
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                
                TRANSFER_SHADOW(o); // Передача теневых координат
                
                return o;
            }

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float3 p)
            {
                float total = 0.0;
                float frequency = 1.0;
                float amplitude = 1.0;
                float maxAmplitude = 0.0;
                
                for (int i = 0; i < 3; i++)
                {
                    total += sin(p.x * frequency * 0.7 + 
                        p.y * frequency * 1.3 + 
                        p.z * frequency * 0.9) * amplitude;
                    maxAmplitude += amplitude;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return total / maxAmplitude;
            }

            // Функция для вычисления отраженного света (specular)
            float3 calculateSpecular(float3 normal, float3 viewDir, float3 lightDir, float smoothness)
            {
                float3 halfwayDir = normalize(lightDir + viewDir);
                float spec = pow(max(dot(normal, halfwayDir), 0.0), 32.0 * smoothness);
                return _LightColor0.rgb * spec * smoothness;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Вычисляем шум текстуры
                float3 pos = i.worldPos * _NoiseScale * 0.01;
                float var = noise(pos);
                var = var * 0.5 + 0.5; 
                
                if (_NoiseDetail > 0)
                {
                    float detail = sin(i.worldPos.x * _NoiseScale * 2.3 + i.worldPos.y * _NoiseScale * 1.7 + i.worldPos.z * _NoiseScale * 3.1) * 0.5 + 0.5;
                    var = lerp(var, detail, 0.3 * _NoiseDetail);
                }
                
                float4 color = _Color;
                color.rgb *= (0.8 + var * 0.4);
                
                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(i.viewDir);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                
                // Диффузное освещение
                float diff = max(dot(normal, lightDir), 0.0);
                float3 diffuse = _LightColor0.rgb * diff;
                
                // Отраженный свет (specular)
                float3 specular = calculateSpecular(normal, viewDir, lightDir, _Smoothness);
                
                // Тени
                float shadow = SHADOW_ATTENUATION(i);
                shadow = lerp(1.0, shadow, _ShadowStrength); // Контролируем силу теней
                
                // Комбинируем освещение
                float3 lighting = diffuse + specular;
                
                // Учет металличности
                color.rgb = lerp(color.rgb * lighting, color.rgb * diffuse + specular, _Metallic);
                
                // Применяем тени
                color.rgb *= shadow;
                
                // Добавляем ambient освещение
                color.rgb += unity_AmbientSky.rgb * 0.2;
                
                return color;
            }
            ENDCG
        }
        
        // Проход для отбрасывания теней
        Pass
        {
            Name "ShadowCaster"
            Tags { 
                "LightMode" = "ShadowCaster"
            }
            
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            
            struct appdata_shadow {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f_shadow {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            v2f_shadow vertShadow(appdata_shadow v)
            {
                v2f_shadow o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 fragShadow(v2f_shadow i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}  