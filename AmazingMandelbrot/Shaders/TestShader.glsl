#version 400
in vec2 fPosition;
out vec4 fragColor;
uniform vec2 resolution;
void main()
{
	fragColor = vec4(10*fPosition.xy/resolution,0,1);
	//fragColor = texelFetch(sourceTex,ivec2(fPosition.x,fPosition.y),0);
}