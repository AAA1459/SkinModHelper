module PlayerAnimPrefixAddOnTrigger
using ..Ahorn, Maple

@mapdef Trigger "SkinModHelper/PlayerAnimPrefixAddOnTrigger" PlayerAnimPrefixAddOnTrigger(x::Integer, y::Integer, width::Integer=32, height::Integer=40,
	animPrefixAddOn::String="default_", revertOnLeave::Bool=false)

const placements = Ahorn.PlacementDict(
	"Player Anim Prefix AddOn Trigger (Skin Mod Helper)" => Ahorn.EntityPlacement(
		PlayerAnimPrefixAddOnTrigger,
		"rectangle"
	)
)

end