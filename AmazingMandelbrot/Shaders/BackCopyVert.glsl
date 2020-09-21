#version 400
in vec3 vPosition;
uniform mat4 projectionMatrix;
out vec2 fPosition;
uniform ivec2 resolution;
void main()
{
	float FullHeight = -2/projectionMatrix[1].y;
	vec2 Offset = (vec2(-1,1)-projectionMatrix[3].xy)/vec2(projectionMatrix[0].x,projectionMatrix[1].y);
	vec4 P = projectionMatrix*vec4(vPosition.x+Offset.x,vPosition.y+Offset.y,vPosition.z,1);
	P.y =-P.y;
	gl_Position = P;
	fPosition=vec2(vPosition.x,vPosition.y);
}