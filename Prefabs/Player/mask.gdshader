shader_type spatial;

render_mode unshaded;

uniform vec3 colorID : source_color = vec3(1., 0.8, 0.);

void fragment()
{
    ALBEDO = colorID;
    ALPHA = 1.;
}
