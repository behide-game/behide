shader_type spatial;
render_mode unshaded;

uniform float effect_decay : hint_range(0.0, 1.0, 0.01) = 0.0;

group_uniforms Color;
uniform vec3 primary_color : source_color;
uniform vec3 secondary_color : source_color;
uniform float emission = 2.0;

group_uniforms Noise;
uniform sampler2D noise_texture;
uniform vec2 noise_scale = vec2(1.0);
uniform vec2 noise_scroll = vec2(-0.2, 0.2);
uniform float noise_shape = 1.0;
uniform float noise_twist = 0.5;

float sample_noise(vec2 uv){
	vec2 noise_uv = uv * noise_scale + noise_scroll * TIME;
	noise_uv.x += noise_uv.y * noise_twist;
	float noise = texture(noise_texture, noise_uv).r;
	return noise;
}

void vertex() {
	float value = sample_noise(UV);
	float mask = sin(UV.y * PI);
	
	VERTEX += NORMAL * value * 0.4 * mask + (NORMAL * effect_decay);
}

void fragment() {
	float value = sample_noise(UV);
	
	float alpha = clamp((value) - effect_decay, 0.0, 1.0);
	
	float color_value = max(value - effect_decay, 0.0);
	vec3 color = mix(secondary_color, primary_color, color_value);
	
	ALBEDO = color * emission;
	ALPHA = alpha;
	ALPHA_SCISSOR_THRESHOLD = 0.001;
}