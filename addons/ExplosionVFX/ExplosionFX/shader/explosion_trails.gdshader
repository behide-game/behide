shader_type spatial;
render_mode particle_trails, blend_add, unshaded;

group_uniforms Color;
uniform vec3 primary_color : source_color;
uniform vec3 secondary_color : source_color;
uniform float emission = 2.0;

group_uniforms Noise;
uniform sampler2D noise_texture;
uniform vec2 noise_scale = vec2(1.0);
uniform vec2 noise_scroll = vec2(0.0, 0.2);

float overlay(float base, float blend){
	float limit = step(0.5, base);
	return mix(2.0 * base * blend, 1.0 - 2.0 * (1.0 - base) * (1.0 - blend), limit);
}

void vertex() {
	float wave = sin((UV.x + UV.y - TIME) * TAU * 2.0) * 0.5 + 0.5;
	
	VERTEX += NORMAL * wave * 0.2 * (UV.y * 2.0);
}

void fragment() {
	float decay = 1.0 - COLOR.a;
	
	float noise = texture(noise_texture, UV).r;
	
	float y = UV.y * 2.0;
	
	float value = overlay(1.0 - y, noise);
	
	float alpha = max(value - decay, 0.0);
	
	ALPHA = alpha;
	ALPHA_SCISSOR_THRESHOLD = 0.2;
	
	float glow_value = smoothstep(0.0, 1.0, value);
	vec3 color = mix(secondary_color, primary_color, glow_value);
	
	ALBEDO = color * emission;
}