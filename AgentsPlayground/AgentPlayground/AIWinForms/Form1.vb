Imports System.Text.Json
Imports System.Threading
Imports System.ComponentModel

Public Class Form1
    Private timeoutTokenSource As CancellationTokenSource = Nothing
    Private parseTimer As System.Windows.Forms.Timer = Nothing
    
    ' Declare controls as class-level variables
    Private txtJson As TextBox
    Private tvJson As TreeView
    Private lblStatus As Label
    Private WithEvents btnParse As Button
    Private WithEvents btnCancel As Button
    Private lastJson As String = ""
    
    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()
        
        ' Add any initialization after the InitializeComponent() call.
        InitializeUI()
    End Sub
    
    Private Sub InitializeUI()
        ' Setup the form
        Me.Text = "JSON Parser"
        Me.Size = New Size(800, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        ' Create JSON input TextBox (multi-line for easy pasting)
        txtJson = New TextBox() With {
            .Name = "txtJson",
            .Location = New Point(10, 10),
            .Size = New Size(780, 200),
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .Font = New Font("Consolas", 10),
            .Text = "{""name"": ""John"", ""age"": 30, ""city"": ""New York""}",
            .TabIndex = 0
        }
        
        ' Create Parse button
        btnParse = New Button() With {
            .Name = "btnParse",
            .Location = New Point(10, 220),
            .Size = New Size(120, 35),
            .Text = "Parse JSON",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .TabIndex = 1
        }
        
        ' Create Clear button
        Dim btnClear As New Button() With {
            .Name = "btnClear",
            .Location = New Point(140, 220),
            .Size = New Size(100, 35),
            .Text = "Clear",
            .Font = New Font("Segoe UI", 10),
            .TabIndex = 2
        }
        AddHandler btnClear.Click, AddressOf ClearJson_Click
        
        ' Create Cancel button (for timeout)
        btnCancel = New Button() With {
            .Name = "btnCancel",
            .Location = New Point(250, 220),
            .Size = New Size(100, 35),
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 10),
            .Enabled = False,
            .TabIndex = 3
        }
        
        ' Create status label
        lblStatus = New Label() With {
            .Name = "lblStatus",
            .Location = New Point(10, 265),
            .Size = New Size(780, 25),
            .Text = "Paste JSON and click Parse or press Ctrl+P",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.Gray,
            .TabIndex = 4
        }
        
        ' Create TreeView for displaying parsed JSON
        tvJson = New TreeView() With {
            .Name = "tvJson",
            .Location = New Point(10, 295),
            .Size = New Size(780, 300),
            .Indent = 20,
            .BorderStyle = BorderStyle.Fixed3D,
            .TabIndex = 5
        }
        
        ' Add all controls to the form
        Me.Controls.Add(tvJson)
        Me.Controls.Add(lblStatus)
        Me.Controls.Add(btnCancel)
        Me.Controls.Add(btnClear)
        Me.Controls.Add(btnParse)
        Me.Controls.Add(txtJson)
        
        ' Wire up text changed event for auto-parse on paste
        AddHandler txtJson.TextChanged, AddressOf TxtJson_TextChanged
        
        ' Create a timer for debouncing paste operations
        parseTimer = New System.Windows.Forms.Timer() With {
            .Interval = 500
        }
        AddHandler parseTimer.Tick, AddressOf ParseTimer_Tick
    End Sub
    
    Private Sub TxtJson_TextChanged(sender As Object, e As EventArgs)
        ' Don't auto-parse on every character change - wait for user to finish pasting
        ' Only trigger if text is significantly different from previous
        If Me.txtJson.Text <> lastJson Then
            lastJson = Me.txtJson.Text
            
            ' Check if it looks like complete JSON (starts with { or [)
            Dim trimmed As String = Me.txtJson.Text.Trim()
            If trimmed.StartsWith("{") OrElse trimmed.StartsWith("[") Then
                ' Reset the timer - only parse after user stops typing for 500ms
                parseTimer.Stop()
                parseTimer.Start()
            End If
        End If
    End Sub
    
    Private Sub ParseTimer_Tick(sender As Object, e As EventArgs)
        parseTimer.Stop()
        
        ' Only auto-parse if there's meaningful JSON content
        Dim trimmed As String = Me.txtJson.Text.Trim()
        If Not String.IsNullOrEmpty(trimmed) AndAlso (trimmed.StartsWith("{") OrElse trimmed.StartsWith("[")) Then
            ParseAndDisplay(trimmed)
        End If
    End Sub
    
    Private Sub ParseJson_Click(sender As Object, e As EventArgs) Handles btnParse.Click
        Dim jsonInput As String = txtJson.Text.Trim()
        
        If String.IsNullOrEmpty(jsonInput) Then
            lblStatus.Text = "Error: JSON input cannot be empty"
            lblStatus.ForeColor = Color.Red
            Return
        End If
        
        ' Validate JSON structure before parsing
        Dim validationError As String = ValidateJsonStructure(jsonInput)
        If Not String.IsNullOrEmpty(validationError) Then
            lblStatus.Text = $"Invalid JSON: {validationError}"
            lblStatus.ForeColor = Color.Red
            Return
        End If
        
        ' Start parsing with timeout protection
        ParseAndDisplay(jsonInput)
    End Sub
    
    Private Async Sub ParseAndDisplay(json As String)
        Dim cts As CancellationTokenSource = Nothing
        Try
            ' Create a cancellation token source for timeout (5 seconds)
            cts = New CancellationTokenSource(TimeSpan.FromSeconds(5))
            timeoutTokenSource = cts

            ' Update UI to show parsing in progress
            lblStatus.Text = "Parsing JSON..."
            lblStatus.ForeColor = Color.Orange
            btnCancel.Enabled = True
            btnParse.Enabled = False
            tvJson.Nodes.Clear()

            Dim rootNode As TreeNode = Await ParseTaskAsync(json, cts.Token)

            If rootNode IsNot Nothing Then
                ' Add root node and expand all children
                tvJson.Nodes.Add(rootNode)
                tvJson.ExpandAll()
                lblStatus.Text = "JSON parsed successfully!"
                lblStatus.ForeColor = Color.Green
            Else
                lblStatus.Text = "Error: Parser returned no result"
                lblStatus.ForeColor = Color.Red
            End If

        Catch ex As OperationCanceledException
            ' User cancelled parsing or timeout occurred
            lblStatus.Text = "Parsing timed out after 5 seconds. JSON may be too large or malformed."
            lblStatus.ForeColor = Color.Red

        Catch ex As TimeoutException
            lblStatus.Text = $"Parsing timed out after 5 seconds. JSON may be too large or malformed."
            lblStatus.ForeColor = Color.Red

        Catch ex As Exception
            ' Parse error (invalid JSON format, etc.)
            lblStatus.Text = $"Error parsing JSON: {ex.Message}"
            lblStatus.ForeColor = Color.Red

        Finally
            ' Cleanup timeout token source
            If cts IsNot Nothing Then
                cts.Dispose()
                cts = Nothing
            End If
            If timeoutTokenSource IsNot Nothing AndAlso timeoutTokenSource Is cts Then
                timeoutTokenSource = Nothing
            End If

            ' Reset button states on UI thread
            btnCancel.Enabled = False
            btnParse.Enabled = True
        End Try
    End Sub

    Private Async Function ParseTaskAsync(json As String, token As CancellationToken) As Task(Of TreeNode)
        Return Await Task.Run(Function() DoParseJson(json), token)
    End Function

    Private Function DoParseJson(json As String) As TreeNode
        Dim rootNode As TreeNode = Nothing

        Using doc As JsonDocument = JsonDocument.Parse(json)
            rootNode = BuildTreeNode(doc.RootElement, "")
        End Using

        Return rootNode
    End Function

    Private Function BuildTreeNode(element As JsonElement, name As String) As TreeNode
        Dim node As New TreeNode()
        
        ' Set node text based on element type
        Select Case element.ValueKind
            Case JsonValueKind.Object
                ' For objects, show the key:name if provided, otherwise just {Object}
                If Not String.IsNullOrEmpty(name) Then
                    node.Text = $"{{ {name} }}"
                Else
                    node.Text = "{ Object }"
                End If
                
                ' Add children properties
                For Each prop In element.EnumerateObject()
                    Dim childNode As New TreeNode()
                    
                    ' Recursively build tree for the property value
                    Select Case prop.Value.ValueKind
                        Case JsonValueKind.Object
                            childNode = BuildTreeNode(prop.Value, prop.Name)
                        Case JsonValueKind.Array
                            For i As Integer = 0 To prop.Value.GetArrayLength() - 1
                                Dim arrayChild As TreeNode = BuildTreeNode(prop.Value(i), $"[{i}]")
                                childNode.Nodes.Add(arrayChild)
                            Next
                            childNode.Text = $"[] {prop.Name} ({prop.Value.GetArrayLength()} items)"
                        Case JsonValueKind.String
                            childNode.Text = $"{prop.Name}: ""{prop.Value.GetString()}{""}"
                        Case JsonValueKind.Number
                            Dim numVal As Decimal
                            If prop.Value.TryGetDecimal(numVal) Then
                                childNode.Text = $"{prop.Name}: {numVal}"
                            Else
                                childNode.Text = $"{prop.Name}: {prop.Value.ToString()}"
                            End If
                        Case JsonValueKind.True, JsonValueKind.False
                            childNode.Text = $"{prop.Name}: {prop.Value.GetBoolean().ToString().ToLower()}"
                        Case JsonValueKind.Null
                            childNode.Text = $"{prop.Name}: null"
                        Case Else
                            childNode.Text = $"{prop.Name}: [{prop.Value.ValueKind.ToString()}]"
                    End Select
                    
                    node.Nodes.Add(childNode)
                Next
                
            Case JsonValueKind.Array
                ' For arrays, show the index
                If Not String.IsNullOrEmpty(name) Then
                    node.Text = $"[ {name} ]"
                Else
                    node.Text = "[ Array ]"
                End If
                
                ' Add array elements as children
                For i As Integer = 0 To element.GetArrayLength() - 1
                    Dim childNode As TreeNode
                    
                    Select Case element(i).ValueKind
                        Case JsonValueKind.Object
                            childNode = BuildTreeNode(element(i), $"[{i}]")
                        Case Else
                            childNode = New TreeNode()
                            childNode.Text = $"{i}: {FormatJsonValue(element(i))}"
                    End Select
                    
                    node.Nodes.Add(childNode)
                Next
                
            Case JsonValueKind.String
                ' For string values, just display the value
                If Not String.IsNullOrEmpty(name) Then
                    node.Text = $"'{element.GetString()}{""}' ({name})"
                Else
                    node.Text = $"'{element.GetString()}{""}'"
                End If
                
            Case JsonValueKind.Number
                ' For number values, display the numeric value
                If Not String.IsNullOrEmpty(name) Then
                    node.Text = $"{element.GetDecimal()} ({name})"
                Else
                    node.Text = $"{element.GetDecimal()}"
                End If
                
            Case JsonValueKind.True
                If Not String.IsNullOrEmpty(name) Then
                    node.Text = $"True ({name})"
                Else
                    node.Text = "True"
                End If
                
            Case JsonValueKind.False
                If Not String.IsNullOrEmpty(name) Then
                    node.Text = $"False ({name})"
                Else
                    node.Text = "False"
                End If
                
            Case JsonValueKind.Null
                If Not String.IsNullOrEmpty(name) Then
                    node.Text = $"null ({name})"
                Else
                    node.Text = "null"
                End If
        End Select
        
        Return node
    End Function
    
    Private Function FormatJsonValue(element As JsonElement) As String
        Return element.ValueKind.ToString().ToLower()
    End Function
    
    Private Function ValidateJsonStructure(jsonInput As String) As String
        Try
            ' Check for balanced braces and brackets
            Dim braceCount As Integer = 0
            Dim bracketCount As Integer = 0
            
            For i As Integer = 0 To jsonInput.Length - 1
                Dim c As Char = jsonInput.Chars(i)
                
                If c = "{" Then
                    braceCount += 1
                ElseIf c = "}" Then
                    braceCount -= 1
                    If braceCount < 0 Then
                        Return $"Unbalanced braces at position {i}"
                    End If
                ElseIf c = "[" Then
                    bracketCount += 1
                ElseIf c = "]" Then
                    bracketCount -= 1
                    If bracketCount < 0 Then
                        Return $"Unbalanced brackets at position {i}"
                    End If
                End If
                
                ' Early exit if counts go too negative (indicates mismatched brackets)
                If braceCount < -10 OrElse bracketCount < -10 Then
                    Return $"Severely unbalanced structure"
                End If
            Next
            
            ' Check final balance
            If braceCount <> 0 Then
                Return $"Unbalanced braces: {braceCount} unclosed"
            End If
            
            If bracketCount <> 0 Then
                Return $"Unbalanced brackets: {bracketCount} unclosed"
            End If
            
            ' Check for basic JSON validity using TryParse
            Dim isValid As Boolean = False
            Try
                JsonDocument.Parse(jsonInput)
                isValid = True
            Catch ex As Exception
                Return $"Invalid JSON format: {ex.Message}"
            End Try
            
        Catch ex As Exception
            Return $"Error validating JSON: {ex.Message}"
        End Try
        
        Return String.Empty ' Valid JSON structure
    End Function
    
    Private Sub ClearJson_Click(sender As Object, e As EventArgs)
        txtJson.Clear()
        tvJson.Nodes.Clear()
        lblStatus.Text = "Cleared. Paste JSON and click Parse or press Ctrl+P"
        lblStatus.ForeColor = Color.Gray
    End Sub
    
    Private Sub CancelParsing_Click(sender As Object, e As EventArgs)
        If timeoutTokenSource IsNot Nothing Then
            timeoutTokenSource.Cancel()
            lblStatus.Text = "Cancelling parsing..."
        End If
    End Sub

End Class
