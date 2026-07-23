@tool
extends OmniLight3D
class_name VFXOmniLightBB

@export var light_mult : float = 1.0:
	set(v):
		light_mult = v
		light_energy = vfx_light_energy * light_mult

@export var vfx_light_energy : float = 5.0:
	set(v):
		vfx_light_energy = v
		light_energy = vfx_light_energy * light_mult

@export var vfx_indirect_energy : float = 1.0:
	set(v):
		vfx_indirect_energy = v
		light_indirect_energy = vfx_indirect_energy * light_mult

@export var vfx_volumetric_fog_energy : float = 1.0:
	set(v):
		vfx_volumetric_fog_energy = v
		light_volumetric_fog_energy = vfx_volumetric_fog_energy * light_mult
