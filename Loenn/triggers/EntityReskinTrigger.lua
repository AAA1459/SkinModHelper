local entityReskinTrigger = {}

entityReskinTrigger.name = "SkinModHelper/EntityReskinTrigger"
entityReskinTrigger.placements = {
    {
        name = "normal",
        data = {
            width = 32,
            height = 40,
			entityIndex = -1,
            entityFullName = "Celeste.Strawberry",
			
			oneUse = true,
			newSpriteID = "strawberry_",
        }
    }
}

entityReskinTrigger.fieldInformation = {
    entityIndex = {
        fieldType = "integer",
        minimumValue = -1,
    }
}

return entityReskinTrigger