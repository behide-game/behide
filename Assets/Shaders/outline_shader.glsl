#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(push_constant) uniform push_constants {
    vec2 raster_size;
} p;

layout(set = 0, binding = 0) uniform sampler2D screen_tex;
layout(rgba16f, set = 0, binding = 1) uniform image2D output_image;

void main() {
    ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
    vec2 size = p.raster_size;

    if (uv.x >= size.x || uv.y >= size.y)
		return;

    vec4 color = texelFetch(screen_tex, uv, 0);
    vec4 res;

    if(color.a>0.) res = vec4(1., 0., 0., 1.);
    else res = vec4(0., 0., 0., 0.);
    imageStore(output_image, uv, res);
}
