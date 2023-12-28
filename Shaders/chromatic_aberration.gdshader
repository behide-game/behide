shader_type canvas_item;

uniform sampler2D screenTexture : hint_screen_texture, filter_linear_mipmap;
uniform float offsetR = 3;
uniform float offsetG = 6;
uniform float offsetB = 9;

void fragment(){
	// Coordinates of the pixel. (0; 0) is the center of the window.
	vec2 uv = UV * 2. - 1.;

	// Distance from the center of the screen
	// It's a gradient: 1 in the window corner, 0 at the center
	float d = smoothstep(1, 1.3, length(uv));

	if (d == 0.) COLOR = textureLod(screenTexture, SCREEN_UV, 0);
	else {
		// The direction of the vector that will shift the channels
		vec2 direction = normalize(uv);
		// Calculating offsets
		vec2 redOffset   = d * (offsetR * 0.001) * direction;
		vec2 greenOffset = d * (offsetG * 0.001) * direction;
		vec2 blueOffset  = d * (offsetB * 0.001) * direction;
		// Calculating lod (equivalent to the blurriness)
		float lod = d * .5;

		// Applying offsets
		COLOR.r = textureLod(screenTexture, SCREEN_UV - redOffset,   lod).r;
		COLOR.g = textureLod(screenTexture, SCREEN_UV - greenOffset, lod).g;
		COLOR.b = textureLod(screenTexture, SCREEN_UV - blueOffset,  lod).b;
	}
}
