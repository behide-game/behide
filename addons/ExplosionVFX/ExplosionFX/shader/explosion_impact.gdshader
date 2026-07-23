shader_type spatial;
render_mode unshaded, blend_add, cull_disabled;

group_uniforms Color;
uniform vec3 primary_color : source_color;
uniform float emission = 2.0;

group_uniforms Noise;
uniform float effect_decay : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform vec2 noise_scale = vec2(1.0);
uniform vec2 noise_scroll = vec2(0.0, 0.2);
uniform float noise_shape = 1.0;

uniform float streak_amount = 7.0;

uniform sampler2D noise_texture;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 noise_uv = UV * vec2(2.0, 2.0) * noise_scale + TIME * noise_scroll;
	float noise = texture(noise_texture, noise_uv).r;
	noise = pow(noise, noise_shape);
	
	
	float wave = sin(UV.x * TAU * streak_amount);
	
	float decay = 1.0 - COLOR.a;
	float mask = sin(UV.y * 2.0 * PI);
	float y = UV.y * 4.0 - 1.0;
	
	float shape = 1.0 - abs((y - ((1.0 - decay) - 0.25)) + wave * 0.25 * decay);
	shape = (shape - 0.75) / 0.25;
	
	float value = shape;
	value -= (2.0 - noise) * (0.2 + pow(decay, 2.0) * 0.8);
	
	ALPHA = max(value, 0.0);
	ALPHA_SCISSOR_THRESHOLD = 0.2;
	
	ALBEDO = primary_color * emission;
}