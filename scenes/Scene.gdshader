shader_type spatial;
// Disable culling so we can see the inside/back of the plants
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec3 albedo : source_color = vec3(0.2, 0.6, 0.3); // Base green color
uniform float sway_speed = 1.5;
uniform float sway_strength = 0.3;

void vertex() {
    // This ensures plants sway out-of-sync with each other, rather than perfectly together.
    float time_offset = NODE_POSITION_WORLD.x * 0.1 + NODE_POSITION_WORLD.z * 0.1;
    float sway_time = TIME * sway_speed + time_offset;
    
    // top to sway the most, and the root (bottom) to not move at all.
	float yy = UV.y * 2.0;
    float sway_factor = 1.0 - yy; 
    
    // Apply sine waves to the X and Z vertices
    VERTEX.x += sin(sway_time) * sway_strength * sway_factor * sway_factor;
    VERTEX.z += cos(sway_time * 0.8) * sway_strength * sway_factor * sway_factor;
}

void fragment() {
    ALBEDO = albedo;
    RIM = 0.2;
    ROUGHNESS = 0.8;
}