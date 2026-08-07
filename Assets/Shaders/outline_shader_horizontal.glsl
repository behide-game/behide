#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(push_constant) uniform push_constants {
    vec2 raster_size;
    int outline_width;
} p;

layout(set = 0, binding = 0) uniform sampler2D screen_tex;
layout(rgba16f, set = 0, binding = 1) uniform image2D output_image;

void main() {
    ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
    vec2 size = p.raster_size;
    int outline_width = p.outline_width;

    if (uv.x >= size.x || uv.y >= size.y)
		return;

    vec4 temp_color = texelFetch(screen_tex, uv, 0);

    bool is_inner = temp_color.a > 0.;
	bool is_outline = false;
    vec3 mean_color = vec3(0.);
    int weight_count = 0;

	if (!is_inner) {
		for (int x = -outline_width; x <= outline_width; ++x) {
            if (x == 0) { continue; }
            ivec2 n_iuv = clamp(uv + ivec2(x, 0), ivec2(0), ivec2(size)-ivec2(1));
            if (n_iuv.x >= size.x || n_iuv.y >= size.y) continue; // prevent reading outside texture
            vec4 currentPixel = texelFetch(screen_tex, n_iuv, 0);
            float n_d_h = currentPixel.a;
            if(n_d_h > 0.01) {
                int weight = outline_width - abs(x) + 1;
                mean_color += currentPixel.rgb/currentPixel.a*float(weight);
                weight_count += weight;
                is_outline = true;
            }
		}
	}
    vec4 color = mix(vec4(temp_color.rgb/temp_color.a, 0.5), mix(vec4(mean_color/float(weight_count), 1.), vec4(0.), is_outline?0:1), is_inner?0:1);
	imageStore(output_image, uv, color);
}
