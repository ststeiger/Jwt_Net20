
Namespace System.Runtime.CompilerServices
    ''' <summary>
    ''' Indicates that a method is an extension method, or that a class or assembly contains extension methods.
    ''' </summary>
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.[Class] Or AttributeTargets.Assembly)> _
    Friend NotInheritable Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace
