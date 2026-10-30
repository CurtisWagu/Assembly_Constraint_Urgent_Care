Imports Inventor

' Suppresses BC35000, which .NET 8 reports for a Module that is referenced from the GuidAttribute on the add-in class.
#Disable Warning BC35000
Public Module Globals

    Public _inventorApplication As Inventor.Application

    ' Unique ID for this add-in. It must match the ClassId/ClientId in Autodesk.Assembly_Constraint_Urgent_Care.Inventor.addin,
    ' the ProductCode in PackageContents.xml, and the clsid in Assembly_Constraint_Urgent_Care.X.manifest.
    Public Const _simpleAddInClientId As String = "365B2F23-E672-443B-96AC-252279713C68"
    Public Const _addInClientId As String = "{" & _simpleAddInClientId & "}"

End Module
#Enable Warning BC35000
