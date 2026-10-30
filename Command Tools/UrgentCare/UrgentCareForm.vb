Imports System.Windows.Forms
Imports Inventor

''' <summary>
''' Non-modal window listing every unhealthy assembly constraint and assembly joint.
'''   - Click an item      : updates the assembly,
'''                          rebuilds the list, then selects that constraint or joint.
'''   - Edit               : launches Inventor's Edit Constraint or Edit Joint command.
'''   - Refresh            : one update + re-scan.
'''   - Finish / close (X) : turns Defer Update OFF and runs one update.
''' Defer Update is turned on automatically only when more than one constraint or joint
''' is broken (re-evaluated on every refresh).
''' </summary>
Public Class UrgentCareForm
    Inherits Form

    Private app As Inventor.Application
    Private asm As AssemblyDocument
    Private opts As AssemblyOptions
    Private docName As String = ""
    Private windowIcon As System.Drawing.Icon
    Private restored As Boolean = False
    Private sick As New List(Of SickItem)
    Private lst As New ListBox()
    Private status As New Label()
    Private chkDefer As New CheckBox()
    Private busy As Boolean = False
    Private lastRows As Integer = 0
    Private Const STATUS_H As Integer = 48
    Private Const BAR_H As Integer = 40

    ''' <summary>One broken constraint or joint in the list.</summary>
    Private Class SickItem
        Public Obj As Object
        Public IsJoint As Boolean
        Public Name As String
    End Class

    Private Declare Auto Function SetForegroundWindow Lib "user32.dll" (ByVal hWnd As IntPtr) As Boolean

    ''' <summary>The full file name of the assembly this window was opened for.</summary>
    Public ReadOnly Property DocumentFullName As String
        Get
            Return docName
        End Get
    End Property

    Public Sub New(a As Inventor.Application, d As AssemblyDocument)
        app = a
        asm = d
        opts = app.AssemblyOptions
        Try
            docName = asm.FullFileName
        Catch
            docName = ""
        End Try
        ' Defer Update is decided after the first scan (see Reload)

        Me.Text = "Urgent Care"
        Try
            windowIcon = LoadIcon("ConstraintUrgentCare.ico", 32)
            Me.Icon = windowIcon
        Catch
            ' The window still works with the default icon.
        End Try
        Me.Width = 620
        Me.Height = 380
        Me.TopMost = True
        Me.StartPosition = FormStartPosition.Manual
        Me.Location = New System.Drawing.Point(60, 120)

        status.Dock = DockStyle.Top
        status.Height = STATUS_H
        status.Padding = New System.Windows.Forms.Padding(6, 6, 6, 0)

        lst.Dock = DockStyle.Fill
        lst.HorizontalScrollbar = True

        Dim bar As New FlowLayoutPanel()
        bar.Dock = DockStyle.Bottom
        bar.Height = BAR_H
        bar.Padding = New System.Windows.Forms.Padding(6)

        Dim bEdit As New Button() : bEdit.Text = "Edit" : bEdit.Width = 90
        Dim bRefresh As New Button() : bRefresh.Text = "Refresh" : bRefresh.Width = 90
        Dim bFinish As New Button() : bFinish.Text = "Finish" : bFinish.Width = 90
        bar.Controls.Add(bEdit)
        bar.Controls.Add(bRefresh)
        bar.Controls.Add(bFinish)

        chkDefer.Text = "Defer updates: OFF"
        chkDefer.AutoSize = True
        chkDefer.Margin = New System.Windows.Forms.Padding(16, 6, 0, 0)
        chkDefer.Checked = False
        chkDefer.ForeColor = System.Drawing.Color.Gray
        Dim tip As New ToolTip()
        tip.AutoPopDelay = 10000
        tip.SetToolTip(chkDefer, "Defer Update is normally OFF in Inventor. It is turned ON here automatically only when more than one constraint or joint is broken, so the solver doesn't prompt after every edit. Finish turns it back OFF.")
        bar.Controls.Add(chkDefer)
        AddHandler chkDefer.CheckedChanged, AddressOf OnDeferToggled


        Me.Controls.Add(lst)
        Me.Controls.Add(status)
        Me.Controls.Add(bar)
        lst.BringToFront()

        AddHandler lst.SelectedIndexChanged, AddressOf OnPick
        AddHandler bEdit.Click, AddressOf OnEdit
        AddHandler bRefresh.Click, AddressOf OnRefresh
        AddHandler bFinish.Click, AddressOf OnFinish
        AddHandler Me.FormClosed, AddressOf OnClosed

        Reload()
    End Sub

    Private Sub Reload()
        sick.Clear()
        lst.Items.Clear()
        Dim constraintCount As Integer = 0
        Dim jointCount As Integer = 0

        For Each c As AssemblyConstraint In asm.ComponentDefinition.Constraints
            Dim h As String = c.HealthStatus.ToString()
            If h <> "kUpToDateHealth" AndAlso h <> "kSuppressedHealth" Then
                Dim item As New SickItem()
                item.Obj = c
                item.IsJoint = False
                item.Name = c.Name
                sick.Add(item)
                constraintCount += 1
                lst.Items.Add(Describe(c, h))
            End If
        Next

        Try
            For Each j As AssemblyJoint In asm.ComponentDefinition.Joints
                Dim h As String = j.HealthStatus.ToString()
                If h <> "kUpToDateHealth" AndAlso h <> "kSuppressedHealth" Then
                    Dim item As New SickItem()
                    item.Obj = j
                    item.IsJoint = True
                    item.Name = j.Name
                    sick.Add(item)
                    jointCount += 1
                    lst.Items.Add(Describe(j, h))
                End If
            Next
        Catch
            ' If joints cannot be read, the constraints are still listed.
        End Try
        ' Defer Update only matters when there is more than one problem
        ApplyDefer(sick.Count > 1)
        ' The checkbox can only be used when there is more than one broken constraint or joint
        chkDefer.Enabled = (sick.Count > 1)

        If sick.Count = 0 Then
            status.Text = "No broken constraints or joints. Click Finish."
        Else
            Dim summary As String
            If jointCount = 0 Then
                summary = constraintCount & " broken constraint(s)."
            ElseIf constraintCount = 0 Then
                summary = jointCount & " broken joint(s)."
            Else
                summary = constraintCount & " broken constraint(s) and " & jointCount & " broken joint(s)."
            End If
            status.Text = summary & vbCrLf &
                          "Click one to select it, then click the Edit button (or right-click it in the browser > Edit)."
        End If

        ResizeToRows()
    End Sub

    ' Window height follows the list: at least 5 rows, at most 10, scrollbar beyond that.
    Private Sub ResizeToRows()
        Dim rows As Integer = Math.Max(5, Math.Min(10, sick.Count))
        If rows = lastRows Then Return          ' don't fight a manual resize
        lastRows = rows
        Me.ClientSize = New System.Drawing.Size(Me.ClientSize.Width, STATUS_H + BAR_H + rows * lst.ItemHeight + 6)
    End Sub

    Private Sub ApplyDefer(want As Boolean)
        Try
            opts.DeferUpdate = want
        Catch
        End Try
        chkDefer.Checked = want
        UpdateDeferLabel()
    End Sub

    Private Sub UpdateDeferLabel()
        If chkDefer.Checked Then
            chkDefer.Text = "Defer updates: ON"
            chkDefer.ForeColor = System.Drawing.Color.Black
        Else
            chkDefer.Text = "Defer updates: OFF"
            chkDefer.ForeColor = System.Drawing.Color.Gray
        End If
    End Sub

    Private Function Describe(c As AssemblyConstraint, h As String) As String
        Dim s As String = c.Name
        Try
            s &= "   [" & c.OccurrenceOne.Name & "  <->  " & c.OccurrenceTwo.Name & "]"
        Catch
        End Try
        Return s & "   (" & h.Substring(1).Replace("Health", "") & ")"
    End Function

    Private Function Describe(j As AssemblyJoint, h As String) As String
        Dim s As String = "Joint:  " & j.Name
        Try
            s &= "   [" & j.OccurrenceOne.Name & "  <->  " & j.OccurrenceTwo.Name & "]"
        Catch
        End Try
        Return s & "   (" & h.Substring(1).Replace("Health", "") & ")"
    End Function

    Private Sub SelectCurrent()
        Dim i As Integer = lst.SelectedIndex
        If i < 0 OrElse i >= sick.Count Then Return
        Try
            asm.SelectSet.Clear()
            asm.SelectSet.Select(sick(i).Obj)
        Catch
        End Try
    End Sub

    Private Sub OnPick(sender As Object, e As EventArgs)
        If busy Then Return
        Dim i As Integer = lst.SelectedIndex
        If i < 0 OrElse i >= sick.Count Then Return

        busy = True
        Try
            Dim targetName As String = sick(i).Name
            Dim targetIsJoint As Boolean = sick(i).IsJoint
            Try
                asm.Update()
            Catch
            End Try
            Reload()

            Dim newIdx As Integer = -1
            For j As Integer = 0 To sick.Count - 1
                If sick(j).Name = targetName AndAlso sick(j).IsJoint = targetIsJoint Then
                    newIdx = j
                    Exit For
                End If
            Next

            If newIdx >= 0 Then
                lst.SelectedIndex = newIdx
                SelectCurrent()
            Else
                status.Text = "'" & targetName & "' is no longer broken. " & sick.Count & " remaining."
            End If
        Finally
            busy = False
        End Try
    End Sub

    Private Sub OnEdit(sender As Object, e As EventArgs)
        SelectCurrent()
        Dim tried As New List(Of String)
        Dim isJoint As Boolean = False
        If lst.SelectedIndex >= 0 AndAlso lst.SelectedIndex < sick.Count Then isJoint = sick(lst.SelectedIndex).IsJoint

        Dim names As New List(Of String)
        If isJoint Then
            ' The command behind Edit on a joint (confirmed present in Inventor 2025.4)
            names.Add("AssemblyJointEditComponentCmd")
        Else
            ' Confirmed present in Inventor 2025.4
            names.Add("AssemblyEditConstraintCtxCmd")
            names.Add("AssemblyEditConstraintCmd")
        End If
        ' Context-menu commands need Inventor to be the active window
        Try
            SetForegroundWindow(New IntPtr(app.MainFrameHWND))
        Catch
        End Try
        For Each n As String In names
            Try
                Dim cd As ControlDefinition = app.CommandManager.ControlDefinitions.Item(n)
                If cd.Enabled Then
                    status.Text = "Running " & n & "..."
                    cd.Execute2(True)
                    status.Text = "Ran " & n & ". Click Refresh to re-check the list."
                    Return
                Else
                    tried.Add(n & " (disabled)")
                End If
            Catch ex As Exception
                tried.Add(n & " (" & ex.Message & ")")
            End Try
        Next
        status.Text = "No usable edit command (" & String.Join(", ", tried.ToArray()) &
                      "). Right-click the selected item in the browser > Edit."
    End Sub

    Private Sub OnDeferToggled(sender As Object, e As EventArgs)
        Try
            opts.DeferUpdate = chkDefer.Checked
        Catch
        End Try
        UpdateDeferLabel()
    End Sub

    Private Sub OnRefresh(sender As Object, e As EventArgs)
        Try
            asm.Update()
        Catch
        End Try
        Reload()
    End Sub

    Private Sub OnFinish(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub OnClosed(sender As Object, e As FormClosedEventArgs)
        If restored Then Return
        restored = True
        Try
            opts.DeferUpdate = False      ' Finish / close always turns Defer Update off
            asm.Update()
        Catch
        End Try
    End Sub

End Class
