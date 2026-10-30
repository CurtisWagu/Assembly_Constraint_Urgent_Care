Imports System.Runtime.InteropServices
Imports Inventor

Namespace Assembly_Constraint_Urgent_Care

    <ProgIdAttribute("Assembly_Constraint_Urgent_Care.StandardAddInServer"), GuidAttribute(Globals._simpleAddInClientId)>
    Public Class StandardAddInServer
        Implements Inventor.ApplicationAddInServer

        Private WithEvents _userInterfaceEvents As UserInterfaceEvents

        ' The command button.
        Private WithEvents _urgentCareButton As ButtonDefinition

        ''' <summary>
        ''' Puts the button on the Relationships panel of the Assemble tab, in the Assembly environment.
        ''' </summary>
        Private Sub AddToUserInterface()

            Dim assemblyRibbon As Ribbon = Globals._inventorApplication.UserInterfaceManager.Ribbons.Item("Assembly")

            Dim relationshipsPanel As RibbonPanel = FindPanel(assemblyRibbon, "Assemble", "Relationships")
            If relationshipsPanel Is Nothing Then
                Throw New System.InvalidOperationException("The Relationships panel was not found on the Assembly ribbon.")
            End If

            _urgentCareButton = UrgentCareCommand.CreateButton(relationshipsPanel)

        End Sub

        ''' <summary>
        ''' Finds a panel by its display name. The preferred tab is searched first, then every other tab.
        ''' </summary>
        Private Function FindPanel(myRibbon As Ribbon, preferredTabName As String, panelName As String) As RibbonPanel

            For Each tabItem As RibbonTab In myRibbon.RibbonTabs
                If String.Equals(tabItem.DisplayName, preferredTabName, System.StringComparison.OrdinalIgnoreCase) Then
                    For Each panelItem As RibbonPanel In tabItem.RibbonPanels
                        If String.Equals(panelItem.DisplayName, panelName, System.StringComparison.OrdinalIgnoreCase) Then Return panelItem
                    Next
                End If
            Next

            For Each tabItem As RibbonTab In myRibbon.RibbonTabs
                For Each panelItem As RibbonPanel In tabItem.RibbonPanels
                    If String.Equals(panelItem.DisplayName, panelName, System.StringComparison.OrdinalIgnoreCase) Then Return panelItem
                Next
            Next

            Return Nothing

        End Function

#Region "Button 'on click' Events"

        Private Sub UrgentCare_Button_OnExecute(context As NameValueMap) Handles _urgentCareButton.OnExecute
            UrgentCareCommand.RunCommandCode()
        End Sub

#End Region

#Region "ApplicationAddInServer Members"

        ''' <summary>
        ''' This method is called by Inventor when it loads the AddIn.
        ''' The AddInSiteObject provides access to the Inventor Application object.
        ''' The FirstTime flag is always true with the ribbon, so the user interface is added every time.
        ''' </summary>
        Public Sub Activate(ByVal addInSiteObject As Inventor.ApplicationAddInSite, ByVal firstTime As Boolean) Implements Inventor.ApplicationAddInServer.Activate

            Globals._inventorApplication = addInSiteObject.Application
            _userInterfaceEvents = Globals._inventorApplication.UserInterfaceManager.UserInterfaceEvents

            Try
                AddToUserInterface()
                Globals._inventorApplication.StatusBarText = "Assembly Constraint Urgent Care loaded."
            Catch ex As System.Exception
                System.Diagnostics.Debug.WriteLine("*******  Error adding user interface: " & ex.Message)
                System.Windows.Forms.MessageBox.Show("Assembly Constraint Urgent Care could not add its button:" & vbCrLf & vbCrLf & ex.Message,
                                                     "Assembly Constraint Urgent Care")
            End Try

        End Sub

        ' This method is called by Inventor when the AddIn is unloaded. The AddIn will be
        ' unloaded either manually by the user or when the Inventor session is terminated.
        Public Sub Deactivate() Implements Inventor.ApplicationAddInServer.Deactivate

            UrgentCareCommand.CloseWindow()

            _urgentCareButton = Nothing
            _userInterfaceEvents = Nothing
            Globals._inventorApplication = Nothing

            System.GC.Collect()
            System.GC.WaitForPendingFinalizers()

        End Sub

        ' This property is provided to allow the AddIn to expose an API of its own to other programs. Not used here.
        Public ReadOnly Property Automation() As Object Implements Inventor.ApplicationAddInServer.Automation
            Get
                Return Nothing
            End Get
        End Property

        ' Note: This method is now obsolete, you should use the ControlDefinition functionality for implementing commands.
        Public Sub ExecuteCommand(ByVal commandId As Integer) Implements Inventor.ApplicationAddInServer.ExecuteCommand
            ' Not used.
        End Sub

        Private Sub OnResetRibbonInterface(Context As NameValueMap) Handles _userInterfaceEvents.OnResetRibbonInterface
            ' The ribbon was reset, so add back the add-ins user-interface.
            Try
                AddToUserInterface()
            Catch ex As System.Exception
                System.Diagnostics.Debug.WriteLine("*******  Error re-adding user interface: " & ex.Message)
            End Try
        End Sub

#End Region

    End Class

End Namespace
