Imports Inventor

''' <summary>
''' The Urgent Care ribbon button: creates the button definition,
''' puts it on a panel, and opens the window when it is clicked.
''' </summary>
Module UrgentCareCommand

    Private Const Title As String = "Urgent Care"
    Private Const IconFileName As String = "ConstraintUrgentCare.ico"

    ' Keep the icons alive for the life of the add-in (the picture objects borrow their handles).
    Private _smallIcon As System.Drawing.Icon
    Private _largeIcon As System.Drawing.Icon

    ' The one window, if it is open.
    Private _form As UrgentCareForm


    Function CreateButton(ribbonPanel As RibbonPanel) As ButtonDefinition

        ' Get the images to use for the button (Resources\ConstraintUrgentCare.ico). The button still works if the icon is missing.
        Dim largeIcon As stdole.IPictureDisp = Nothing
        Dim standardIcon As stdole.IPictureDisp = Nothing
        Try
            _largeIcon = LoadIcon(IconFileName, 32)
            _smallIcon = LoadIcon(IconFileName, 16)
            largeIcon = PictureDispConverter.ToIPictureDisp(_largeIcon)
            standardIcon = PictureDispConverter.ToIPictureDisp(_smallIcon)
        Catch ex As System.Exception
            System.Diagnostics.Debug.WriteLine("*******  Urgent Care icon not loaded: " & ex.Message)
        End Try

        ' This is the text the user sees on the button.
        Dim buttonLabel As String = "Urgent" & vbLf & "Care"

        ' Text that displays when the user hovers over the button.
        Dim toolTipSimple As String = "Lists the broken constraints and joints in the assembly so you can fix them one by one"
        Dim toolTipExpanded As String =
            ChrW(&H2022) & " Shows every broken (sick) constraint and joint in one window." & vbLf &
            ChrW(&H2022) & " Click one to select it, then Edit it." & vbLf &
            ChrW(&H2022) & " Works with both assembly constraints and joints." & vbLf &
            ChrW(&H2022) & " Defer Update is turned on for you when more than one constraint or joint is broken."

        Dim internalName As String = "Assembly_Constraint_Urgent_Care_" & Globals._addInClientId & "_Button_InternalName"
        Dim controlDefs As Inventor.ControlDefinitions = Globals._inventorApplication.CommandManager.ControlDefinitions

        ' Reuse the definition if Inventor already has it.
        Dim buttonDefinition As ButtonDefinition = Nothing
        Try
            buttonDefinition = CType(controlDefs.Item(internalName), ButtonDefinition)
        Catch
            buttonDefinition = Nothing
        End Try

        If buttonDefinition Is Nothing Then
            buttonDefinition = controlDefs.AddButtonDefinition(buttonLabel, internalName, CommandTypesEnum.kNonShapeEditCmdType, Globals._addInClientId)
            System.Diagnostics.Debug.WriteLine("*******  " & buttonDefinition.InternalName & " button created")
        End If

        If largeIcon IsNot Nothing Then buttonDefinition.LargeIcon = largeIcon
        If standardIcon IsNot Nothing Then buttonDefinition.StandardIcon = standardIcon
        buttonDefinition.ToolTipText = toolTipSimple

        ' The richer tooltip: a title and a description (no image).
        Try
            With buttonDefinition.ProgressiveToolTip
                .IsProgressive = True
                .Title = Title
                .Description = toolTipSimple
                .ExpandedDescription = toolTipExpanded
            End With
        Catch
            ' The plain tooltip above is still shown.
        End Try

        ' Add it to the panel once.
        Dim alreadyOnPanel As Boolean = False
        For Each ctl As CommandControl In ribbonPanel.CommandControls
            Try
                If ctl.InternalName = buttonDefinition.InternalName Then
                    alreadyOnPanel = True
                    Exit For
                End If
            Catch
            End Try
        Next

        If Not alreadyOnPanel Then
            ribbonPanel.CommandControls.AddButton(buttonDefinition, True, True)
            System.Diagnostics.Debug.WriteLine("*******  " & buttonDefinition.InternalName & " button added to " & ribbonPanel.DisplayName)
        End If

        Return buttonDefinition

    End Function

    ' This is the code that runs when the button is clicked.
    Sub RunCommandCode()

        Dim app As Inventor.Application = Globals._inventorApplication
        Dim doc As Document = app.ActiveDocument

        If doc Is Nothing OrElse doc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
            System.Windows.Forms.MessageBox.Show("Open an assembly (.iam) first.", Title,
                                                 System.Windows.Forms.MessageBoxButtons.OK,
                                                 System.Windows.Forms.MessageBoxIcon.Information)
            Return
        End If

        ' One window at a time. If it is already open for this assembly, just bring it forward.
        If _form IsNot Nothing AndAlso Not _form.IsDisposed Then
            If _form.DocumentFullName = doc.FullFileName Then
                _form.Activate()
                Return
            End If
            _form.Close()
        End If

        _form = New UrgentCareForm(app, CType(doc, AssemblyDocument))
        _form.Show()

    End Sub

    ' Closes the window when the add-in unloads.
    Sub CloseWindow()
        Try
            If _form IsNot Nothing AndAlso Not _form.IsDisposed Then _form.Close()
        Catch
        End Try
        _form = Nothing
    End Sub

End Module
