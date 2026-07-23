shader_type spatial;

uniform vec3 tertiary_color : source_color;

uniform sampler2D noise_texture;

void fragment() {
	float rand = COLOR.r;
	float radius = length(UV * 2.0 - 1.0);
	float noise = texture(noise_texture, UV * 0.5 + rand).r;
	
	float value = noise - (1.0 - COLOR.a);
	value -= pow(radius, 4.0);
	
	float alpha = clamp(value, 0.0, 1.0);
	
	ALBEDO = tertiary_color;
	
	ALPHA = alpha;
	ALPHA_SCISSOR_THRESHOLD = 0.1;
}