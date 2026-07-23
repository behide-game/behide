shader_type spatial;

uniform float effect_decay : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform bool is_particle = true;

group_uniforms Color;
uniform vec3 primary_color : source_color;
uniform vec3 secondary_color : source_color;
uniform vec3 tertiary_color : source_color;
uniform float emission = 2.0;

group_uniforms Noise;
uniform sampler2D smoke_texture;
uniform vec2 noise_scale = vec2(1.0);
uniform vec2 extra_scale = vec2(1.0);
uniform vec2 noise_scroll = vec2(0.0, 0.2);

group_uniforms Shading;
uniform bool use_stepped_shading = true;
uniform float shadow_steps : hint_range(2.0, 12.0, 0.01) = 3.0;

group_uniforms Displacement;
uniform float displacement_amount = 0.2;
uniform float pole_displacement_amount = 0.0;


group_uniforms Normal;
uniform sampler2D normal_texture;

void vertex() {
	float rand = COLOR.r;
	vec2 noise_uv = (UV + rand + noise_scroll * TIME) * noise_scale;
	float noise = texture(smoke_texture, noise_uv).r;
	
	float mask = sin(UV.y * PI);
	
	VERTEX += NORMAL * noise * displacement_amount * mask;
	VERTEX += (NORMAL * pow(mask - 1.0, 3.0)) * pole_displacement_amount;
}

void fragment() {
	float rand = COLOR.r;
	float decay = is_particle ? (1.0 - COLOR.a * (1.0 - effect_decay)) : effect_decay;
	
	vec2 noise_uv = (UV + rand + noise_scroll * TIME) * noise_scale * extra_scale;
	float noise = texture(smoke_texture, noise_uv).r;
	
	float noise_decay = clamp(decay * 2.0, 0.0, 1.0);
	float noise_value = 1.0 - noise;
	//noise_value = (noise_value) * (dot(VIEW, NORMAL));
	noise_value = max(noise_value - noise_decay, 0.0);
	noise_value = smoothstep(0.0, 0.5, noise_value);
	
	vec3 color = mix(tertiary_color, primary_color, noise_value);
	
	ALBEDO = color;
	EMISSION = ALBEDO * noise_value * emission;
	
	float alpha_decay = min(decay, 1.0);
	float alpha = clamp((1.0 + noise) - (alpha_decay * 2.0), 0.0, 1.0);
	
	ALPHA = alpha;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
	
	vec3 normal_map = textureLod(normal_texture, noise_uv, 2.0).rgb;
	NORMAL_MAP = normal_map;
	SPECULAR = 0.0;
}

void light() {
	float ndotl = max(dot(NORMAL, LIGHT), 0.0);
	float light = ndotl * ATTENUATION;
	if(use_stepped_shading){
		light = floor(light * shadow_steps) / (shadow_steps - 1.0);
	}
	
	DIFFUSE_LIGHT += (LIGHT_COLOR) * light;
}