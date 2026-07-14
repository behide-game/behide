shader_type spatial;
render_mode unshaded;

uniform sampler2D albedo_tex : source_color;

void fragment() {
    vec4 c = texture(albedo_tex, UV);
    ALBEDO = c.rgb;
    ALPHA = c.a;
}