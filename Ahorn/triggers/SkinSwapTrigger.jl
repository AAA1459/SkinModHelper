module SkinModHelperSkinSwapTrigger
using ..Ahorn, Maple

@mapdef Trigger "SkinModHelper/SkinSwapTrigger" SkinSwapTrigger(x::Integer, y::Integer, width::Integer=32, height::Integer=40,
	skinId::String="Default", revertOnLeave::Bool=false)

const placements = Ahorn.PlacementDict(
	"Skin Swap Trigger (Skin Mod Helper)" => Ahorn.EntityPlacement(
		SkinSwapTrigger,
		"rectangle"
	)
)

end