shader_type spatial;
// Prevent the quad from being affected by lighting and fog. This also improves performance.
render_mode unshaded, fog_disabled;

uniform sampler2D screen_texture : source_color, hint_screen_texture;
uniform float offsetR = 3;
uniform float offsetG = 6;
uniform float offsetB = 9;

void vertex() {
  POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment(){
	// Coordinates of the pixel. (0; 0) is the center of the window.
	vec2 uv = SCREEN_UV * 2. - 1.;

	// Distance from the center of the screen
	// It's a gradient: 1 in the window corner, 0 at the center
	float d = smoothstep(1, 1.3, length(uv));

	if (d == 0.) ALBEDO.rgb = textureLod(screen_texture, SCREEN_UV, 0).rgb;
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
		ALBEDO.r = textureLod(screen_texture, SCREEN_UV - redOffset,   lod).r;
		ALBEDO.g = textureLod(screen_texture, SCREEN_UV - greenOffset, lod).g;
		ALBEDO.b = textureLod(screen_texture, SCREEN_UV - blueOffset,  lod).b;
	}
}
