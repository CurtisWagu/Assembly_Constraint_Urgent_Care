Imports System.Drawing

Module IconLoader

    ''' <summary>
    ''' Loads one size out of an .ico embedded in the Resources folder.
    ''' Example: LoadIcon("ConstraintUrgentCare.ico", 32)
    ''' </summary>
    Friend Function LoadIcon(fileName As String, size As Integer) As System.Drawing.Icon
        Dim asm = System.Reflection.Assembly.GetExecutingAssembly()
        Dim names = asm.GetManifestResourceNames()
        Dim resourceName = names.FirstOrDefault(
            Function(n) n.EndsWith("." & fileName, StringComparison.OrdinalIgnoreCase))

        If resourceName Is Nothing Then
            Throw New System.IO.FileNotFoundException(
                "Embedded icon not found: " & fileName & ". Embedded resources: " & String.Join(", ", names))
        End If

        Using stream = asm.GetManifestResourceStream(resourceName)
            Return New System.Drawing.Icon(stream, size, size)
        End Using
    End Function

End Module
