shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec4 shallow_color : source_color = vec4(0.14, 0.46, 0.56, 1.0);
uniform vec4 deep_color : source_color = vec4(0.03, 0.12, 0.22, 1.0);
uniform vec4 highlight_color : source_color = vec4(0.82, 0.96, 1.0, 1.0);
uniform float wave_scale = 0.18;
uniform float wave_speed = 0.35;
uniform float wave_height = 0.22;
uniform float fresnel_power = 4.0;
uniform float base_alpha = 0.4;
uniform float smoothness = 0.9;

varying vec3 world_pos_v;

void vertex() {
    vec3 world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float wave_a = sin((world_pos.x * wave_scale) + (TIME * wave_speed));
    float wave_b = cos((world_pos.z * wave_scale * 1.2) - (TIME * wave_speed * 0.8));
    float wave = (wave_a + wave_b) * 0.5;

    VERTEX.y += wave * wave_height;
    world_pos_v = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    float wave_a = sin((world_pos_v.x * wave_scale) + (TIME * wave_speed));
    float wave_b = cos((world_pos_v.z * wave_scale * 1.2) - (TIME * wave_speed * 0.8));
    float wave_c = sin((world_pos_v.x + world_pos_v.z) * wave_scale * 0.65 + (TIME * wave_speed * 1.4));

    NORMAL = normalize(vec3(
        wave_a * 0.3 + wave_c * 0.15,
        1.0,
        wave_b * 0.3 - wave_c * 0.15
    ));

    float fresnel = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), fresnel_power);
    float color_mix = clamp(0.55 + (wave_c * 0.15), 0.0, 1.0);

    ALBEDO = mix(deep_color.rgb, shallow_color.rgb, color_mix);
    ALBEDO += highlight_color.rgb * fresnel * 0.08;
    EMISSION = highlight_color.rgb * fresnel * 0.18;
    ROUGHNESS = 1.0 - smoothness;
    SPECULAR = 0.8;
    ALPHA = mix(base_alpha, min(0.85, base_alpha + 0.22), fresnel);
}
