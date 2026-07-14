shader_type canvas_item;

uniform int outline_width = 3;
uniform sampler2D input_texture : repeat_disable, filter_nearest;

void fragment() {
	vec2 suv = SCREEN_UV;
    ivec2 size = textureSize(input_texture,0);
    ivec2 iuv = ivec2(suv*vec2(size));
    vec4 temp_color = texelFetch(input_texture, iuv, 0);

	bool is_inner = temp_color.a > 0.0;
	bool is_outline = false;

	if (!is_inner || is_outline) {
		for (int x = -outline_width; x <= outline_width && !is_outline; ++x) {
            if (x == 0) { continue; }
            ivec2 n_iuv = clamp(iuv + ivec2(x, 0), ivec2(0), size-ivec2(1));
            vec4 currentPixel = texelFetch(input_texture, n_iuv, 0);
            float n_d_h = currentPixel.a;
            is_outline = n_d_h > 0.01;
            if(is_outline) {
                temp_color = currentPixel;
            }
		}
	}

    if(is_inner) {
		COLOR = vec4(temp_color.rgb/temp_color.a, 0.5);
	}
    else if(is_outline) {
		COLOR = vec4(temp_color.rgb/temp_color.a, 1.);
	}
    else discard;
}
