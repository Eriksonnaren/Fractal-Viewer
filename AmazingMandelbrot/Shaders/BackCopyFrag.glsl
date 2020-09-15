#version 400
uniform sampler2D sourceTex;
in vec2 fPosition;
out vec4 fragColor;
uniform vec2 resolution;
uniform mat4 projectionMatrix;
void main()
{
	
	vec4 Col = texelFetch(sourceTex,ivec2(fPosition.x,fPosition.y),0);
	fragColor = vec4(Col.xyz,1);
	//fragColor = texelFetch(sourceTex,ivec2(fPosition.x,fPosition.y),0);
}