shader_type canvas_item;

uniform int outline_width = 3;
uniform sampler2D highlighted_depth_tex : repeat_disable, filter_nearest;

void fragment() {
	vec2 suv = SCREEN_UV;
	float d_h = texture(highlighted_depth_tex, suv).a;
    ivec2 size = textureSize(highlighted_depth_tex,0);
    ivec2 iuv = ivec2(suv*vec2(size));
    vec4 temp_color = texelFetch(highlighted_depth_tex, iuv, 0);

	bool is_inner = d_h > 0.0;
	bool is_outline = false;

	if (!is_inner) {
		for (int x = -outline_width; x <= outline_width && !is_outline; ++x) {
			for (int y = -outline_width; y <= outline_width && !is_outline; ++y) {
				if (y == 0 && x == 0) { continue; }
				ivec2 n_iuv = clamp(iuv + ivec2(x, y), ivec2(0), size-ivec2(1));
                vec4 currentPixel = texelFetch(highlighted_depth_tex, n_iuv, 0);
                float n_d_h = currentPixel.a;
                is_outline = n_d_h > 0.01;
                if(is_outline) {
                    temp_color = currentPixel;
                }
			}
		}
	}

    if(is_inner) COLOR = vec4(temp_color.rgb/temp_color.a, temp_color.a);
    else if(is_outline) COLOR = vec4(temp_color.rgb/temp_color.a, 1.);
    else discard;
}
