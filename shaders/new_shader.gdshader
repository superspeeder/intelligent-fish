shader_type spatial;
render_mode cull_disabled;

uniform sampler2D albedo_texture : source_color;

uniform float time_scale  = 1.0;
uniform float side_to_side  = 0.045;
uniform float pivot  = 2.135;
uniform float wave  = -0.055;
uniform float twist  = .1;



void vertex() {
	// Called for every vertex the material is visible on.
	float time = (TIME * time_scale) + (6.28318 * INSTANCE_CUSTOM.x);
	VERTEX.z += cos(time)*side_to_side;
	
	float pivot_angle = cos(time) * 0.1 * pivot;
	mat2 rotation_matrix = mat2(vec2(cos(pivot_angle), -sin(pivot_angle)), vec2(sin(pivot_angle), cos(pivot_angle)));
	VERTEX.xz = rotation_matrix * VERTEX.xz;
	
	float body = (VERTEX.x + 1.0) / .2;
	VERTEX.z += cos(time + body) * wave;
	
	float twist_angle = cos(time + body) * 0.3 * twist;
	mat2 twist_matrix = mat2(vec2(cos(twist_angle), -sin(twist_angle)), vec2(sin(twist_angle), cos(twist_angle)));
	VERTEX.zy = twist_matrix * VERTEX.zy;
	

}

void fragment() {
	ALBEDO = texture(albedo_texture, UV).rgb;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
