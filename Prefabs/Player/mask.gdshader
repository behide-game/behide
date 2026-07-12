shader_type spatial;

render_mode unshaded;

uniform vec4 colorID : source_color = vec4(1., 0., 0., 0.5);

void fragment()
{
    ALBEDO = colorID.rgb;
    ALPHA = colorID.a;
}
