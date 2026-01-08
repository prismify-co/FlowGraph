#version 100
precision mediump float;

attribute vec3 aPosition;
attribute vec3 aNormal;
attribute vec2 aTexCoord;

varying vec3 vFragPos;
varying vec3 vNormal;
varying vec2 vTexCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vFragPos = worldPos.xyz;
    vNormal = normalize(mat3(uModel) * aNormal);
    vTexCoord = aTexCoord;
    
    gl_Position = uProjection * uView * worldPos;
}
