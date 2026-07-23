shader_type spatial;
render_mode unshaded;

uniform vec3 primary_color : source_color;
uniform vec3 secondary_color : source_color;
uniform float emission = 2.0;

void fragment() {
	ALBEDO = mix(secondary_color, primary_color, COLOR.a) * emission;
}