shader_type spatial;
render_mode blend_mix, unshaded, cull_back, depth_test_disabled;
uniform vec4 albedo : source_color;
uniform float fresnel_power;

void fragment() {
	vec2 base_uv = UV;
	float fresnel = pow((1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0)), fresnel_power);
	ALBEDO = albedo.rgb;
	ALPHA *= albedo.a * fresnel;
}