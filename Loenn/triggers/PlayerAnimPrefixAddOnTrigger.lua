local playerAnimPrefixAddOnTrigger = {}

playerAnimPrefixAddOnTrigger.name = "SkinModHelper/PlayerAnimPrefixAddOnTrigger"
playerAnimPrefixAddOnTrigger.placements = {
    {
        name = "normal",
        data = {
            width = 32,
            height = 40,
            animPrefixAddOn = "default_",
			revertOnLeave = false,
        }
    }
}

return playerAnimPrefixAddOnTrigger