#version 400
in vec3 vPosition;
uniform mat4 projectionMatrix;
out vec2 fPosition;
uniform vec2 resolution;
void main()
{
	
	vec4 P = projectionMatrix*vec4(vPosition.x,vPosition.y,vPosition.z,1);
	//P.y =-P.y;
	gl_Position = P;
	fPosition=vec2(vPosition.x,vPosition.y);
}