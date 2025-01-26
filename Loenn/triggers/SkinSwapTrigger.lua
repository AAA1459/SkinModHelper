local skinSwapTrigger = {}

skinSwapTrigger.name = "SkinModHelper/SkinSwapTrigger"
skinSwapTrigger.placements = {
    {
        name = "normal",
        data = {
            width = 32,
            height = 40,
            skinId = "Default",
            revertOnLeave = false,
			playerVariant = true,
			otherselfVariant = true,
			silhouetteVariant = false,
        }
    }
}

return skinSwapTrigger