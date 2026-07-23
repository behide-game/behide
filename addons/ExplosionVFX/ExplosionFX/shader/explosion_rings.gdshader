shader_type spatial;
render_mode unshaded, blend_add, cull_disabled;

group_uniforms Color;
uniform vec3 primary_color : source_color;
uniform vec3 secondary_color : source_color;
uniform float emission = 2.0;

group_uniforms Noise;
uniform sampler2D noise_texture;
uniform vec2 noise_scale = vec2(1.0);
uniform vec2 noise_scroll = vec2(0.0, 0.2);
uniform float noise_shape = 1.0;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	float decay = 1.0 - COLOR.a;
	
	float mask = 1.0 - abs(UV.y * 4.0 - 1.0);
	
	vec2 noise_uv = (UV * vec2(2.0, 1.0) * noise_scale) + noise_scroll * TIME;
	float noise = texture(noise_texture, noise_uv).r;
	noise = pow(noise, noise_shape);
	
	float value = mask - (1.0 - noise);
	value = max(value - decay, 0.0);
	
	vec3 color = mix(secondary_color, primary_color, value);
	color = FRONT_FACING ? primary_color : secondary_color;
	
	ALBEDO = color * emission;
	
	ALPHA = value;
	ALPHA_SCISSOR_THRESHOLD = 0.2;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
