Imports System

Namespace JWT.PetaJson
	<AttributeUsage(AttributeTargets.[Property] Or AttributeTargets.Field)>
	Public Class JsonExcludeAttribute
		Inherits Attribute

	End Class
End Namespace
