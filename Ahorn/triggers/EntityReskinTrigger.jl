module SkinModHelperEntityReskinTrigger
using ..Ahorn, Maple

@mapdef Trigger "SkinModHelper/EntityReskinTrigger" EntityReskinTrigger(x::Integer, y::Integer, width::Integer=32, height::Integer=40,
	entityIndex::Integer=-1, entityFullName::String="Celeste.Strawberry", 
	oneUse::Bool=true, newSpriteID::String="strawberry_")

const placements = Ahorn.PlacementDict(
	"Entity Reskin Trigger (Skin Mod Helper)" => Ahorn.EntityPlacement(
		EntityReskinTrigger,
		"rectangle"
	)
)

end