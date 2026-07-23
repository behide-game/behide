shader_type spatial;
render_mode unshaded, cull_disabled;

uniform vec3 primary_color : source_color;
uniform float emission = 3.0;

uniform sampler2D noise_texture;
uniform vec2 noise_scale = vec2(1.0);
uniform vec2 noise_scroll = vec2(0.0, 0.2);
uniform float noise_shape = 1.0;

void vertex() {
	float noise = texture(noise_texture, UV * 2.0).r;
	
	VERTEX += NORMAL * noise * 0.4 * (1.0 - COLOR.a);
}

void fragment() {
	float noise = texture(noise_texture, UV * 2.0 * noise_scale + TIME * noise_scroll).r;
	noise = pow(noise, noise_shape);
	
	
	float alpha = clamp((noise + 1.0) - ((1.0 - COLOR.a) * 2.0), 0.0, 1.0);
	
	ALBEDO = primary_color * 20.0;
	
	ALPHA = alpha;
	ALPHA_SCISSOR_THRESHOLD = 0.1;
}