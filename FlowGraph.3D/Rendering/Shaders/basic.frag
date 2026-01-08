#version 100
precision mediump float;

varying vec3 vFragPos;
varying vec3 vNormal;
varying vec2 vTexCoord;

uniform vec4 uColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform vec3 uLightColor;
uniform float uAmbientStrength;
uniform float uSpecularStrength;
uniform float uShininess;

void main()
{
    // Ambient
    vec3 ambient = uAmbientStrength * uLightColor;
    
    // Diffuse
    vec3 norm = normalize(vNormal);
    vec3 lightDir = normalize(uLightPos - vFragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * uLightColor;
    
    // Specular (Blinn-Phong)
    vec3 viewDir = normalize(uViewPos - vFragPos);
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(norm, halfwayDir), 0.0), uShininess);
    vec3 specular = uSpecularStrength * spec * uLightColor;
    
    // Combine
    vec3 result = (ambient + diffuse + specular) * uColor.rgb;
    gl_FragColor = vec4(result, uColor.a);
}
