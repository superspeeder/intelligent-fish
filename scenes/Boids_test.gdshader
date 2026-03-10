shader_type spatial;

uniform vec3 sand_color : source_color = vec3(0.15, 0.2, 0.25);
uniform sampler2D caustic_noise;
uniform float caustic_speed = 0.03;
uniform float caustic_strength = 0.8;
uniform float caustic_scale = 5.0;

void fragment() {
    // Base color of the ocean floor
    ALBEDO = sand_color;

    // 2. Animate the UV coordinates
    vec2 animated_uv = (UV * caustic_scale) + vec2(TIME * caustic_speed, TIME * (caustic_speed * 0.8));

    // Sample the noise texture
    float noise_val = texture(caustic_noise, animated_uv).r;

    // 4. Sharpen the noise to look like light bands
    float caustic = pow(noise_val, 4.0) * caustic_strength;

    // Add a bright teal/cyan tint to the light bands
    vec3 caustic_color = vec3(caustic) * vec3(0.6, 1.0, 1.0);
    ALBEDO += caustic_color;
    
    // Add a slight emission so the light bands glow in dark areas
    EMISSION = caustic_color * 0.5;
}