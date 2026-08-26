Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Threading
Imports System.Text
Imports System.Text.Json

''' <summary>
''' WinForms client for interacting with the OpenAI Chat Completions API.
''' Supports setting an API key, selecting a model, sending text prompts, and attaching files.
''' </summary>
Public Class OpenAIClientForm
    Inherits Form

    ' ====== Controls ======
    Private grpApiKey As GroupBox
    Private txtApiKey As TextBox
    Private chkShowKey As CheckBox
    Private cmbModels As ComboBox
    Private lblModel As Label
    Private txtPrompt As TextBox
    Private btnSend As Button
    Private btnCancel As Button
    Private btnBrowseFile As Button
    Private txtFilePath As TextBox
    Private grpMessages As GroupBox
    Private dgvMessages As DataGridView
    Private lblStatus As Label
    Private progressBar As ProgressBar

    ' ====== State ======
    Private apiClient As HttpClient
    Private ctsCurrentRequest As CancellationTokenSource = Nothing
    Private conversationHistory As New List(Of ChatMessage)()
    Private apiKeyValue As String = String.Empty
    Private selectedModelName As String = "gpt-4o"

    ''' <summary>Possible models available in the dropdown.</summary>
    Private ReadOnly AvailableModels As New List(Of String) From {
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4o-mini-2024-07-18",
        "gpt-4-turbo",
        "gpt-4-turbo-2024-04-09",
        "gpt-3.5-turbo",
        "gpt-3.5-turbo-instruct",
        "o1",
        "o1-preview",
        "o1-mini",
        "o3-mini"
    }

    Public Sub New()
        InitializeComponent()
        SetupHttpClient()
    End Sub

    Private Sub InitializeComponent()
        ' Main form settings
        Me.Text = "OpenAI API Client"
        Me.Size = New Size(920, 780)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(640, 520)

        ' ====== API Key GroupBox ======
        grpApiKey = New GroupBox() With {
            .Text = "OpenAI API Key",
            .Location = New Point(10, 10),
            .Size = New Size(895, 48),
            .Font = SystemFonts.MessageBoxFont
        }

        txtApiKey = New TextBox() With {
            .Name = "txtApiKey",
            .Location = New Point(12, 20),
            .Size = New Size(780, 22),
            .PasswordChar = "*"c,
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 0
        }

        chkShowKey = New CheckBox() With {
            .Name = "chkShowKey",
            .Location = New Point(800, 23),
            .Size = New Size(90, 17),
            .Text = "Show key",
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 1
        }

        ' ====== Status Label ======
        lblStatus = New Label() With {
            .Name = "lblStatus",
            .Location = New Point(10, 62),
            .Size = New Size(895, 20),
            .Text = "Enter your API key above to get started.",
            .Font = New Font(SystemFonts.MessageBoxFont.FontFamily, 8.5F),
            .ForeColor = Color.DimGray
        }

        ' ====== Model Selector ======
        lblModel = New Label() With {
            .Name = "lblModel",
            .Location = New Point(10, 85),
            .Size = New Size(70, 20),
            .Text = "Model:",
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 2
        }

        cmbModels = New ComboBox() With {
            .Name = "cmbModels",
            .Location = New Point(85, 85),
            .Size = New Size(220, 24),
            .DropDownStyle = ComboBoxStyle.DropDown,
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 3
        }
        cmbModels.Items.AddRange(AvailableModels.ToArray())
        cmbModels.SelectedItem = "gpt-4o"

        ' ====== Prompt TextBox ======
        txtPrompt = New TextBox() With {
            .Name = "txtPrompt",
            .Location = New Point(10, 115),
            .Size = New Size(895, 140),
            .Multiline = True,
            .ScrollBars = ScrollBars.Both,
            .Font = New Font("Consolas", 10F),
            .TabIndex = 4,
            .MaxLength = 120_000,
            .AcceptsReturn = True,
            .WordWrap = True
        }

        ' ====== File Attachment Area ======
        btnBrowseFile = New Button() With {
            .Name = "btnBrowseFile",
            .Location = New Point(10, 265),
            .Size = New Size(110, 30),
            .Text = "Attach file…",
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 5
        }

        txtFilePath = New TextBox() With {
            .Name = "txtFilePath",
            .Location = New Point(125, 268),
            .Size = New Size(780, 22),
            .ReadOnly = True,
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 6,
            .Text = "(No file attached)"
        }

        ' ====== Action Buttons ======
        btnSend = New Button() With {
            .Name = "btnSend",
            .Location = New Point(10, 305),
            .Size = New Size(120, 34),
            .Text = "Send",
            .Font = New Font(SystemFonts.MessageBoxFont.FontFamily, 10F, FontStyle.Bold),
            .TabIndex = 7,
            .BackColor = Color.LightGreen,
            .Enabled = False ' Disabled until API key is entered
        }

        btnCancel = New Button() With {
            .Name = "btnCancel",
            .Location = New Point(140, 305),
            .Size = New Size(90, 34),
            .Text = "Cancel",
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 8,
            .Visible = False
        }

        ' ====== Conversation GroupBox ======
        grpMessages = New GroupBox() With {
            .Text = "Conversation",
            .Location = New Point(10, 350),
            .Size = New Size(895, 340),
            .Font = SystemFonts.MessageBoxFont
        }

        ' ====== DataGridView for Messages ======
        dgvMessages = New DataGridView() With {
            .Name = "dgvMessages",
            .Location = New Point(10, 22),
            .Size = New Size(875, 308),
            .Anchor = CType(AnchorStyles.Left Or AnchorStyles.Top Or AnchorStyles.Right Or AnchorStyles.Bottom, AnchorStyles),
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .EnableHeadersVisualStyles = False,
            .ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            .ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.LightSteelBlue,
                .Font = New Font(SystemFonts.MessageBoxFont.FontFamily, 9F, FontStyle.Bold),
                .Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Font = New Font("Consolas", 9.25F),
                .BackColor = Color.White,
                .SelectionBackColor = Color.LightSkyBlue
            },
            .RowTemplate = New DataGridViewRow() With {.Height = 24},
            .Font = SystemFonts.MessageBoxFont,
            .TabIndex = 9,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False
        }

        ' Add three columns: Role | Message | Model
        dgvMessages.Columns.Add("colRole", "Role")
        dgvMessages.Columns("colRole").Width = 70
        dgvMessages.Columns("colRole").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgvMessages.Columns("colRole").DefaultCellStyle.BackColor = Color.LightCoral
        dgvMessages.Columns.Add("colContent", "Message")
        dgvMessages.Columns.Add("colModel", "Model")
        dgvMessages.Columns("colModel").Width = 130
        dgvMessages.Columns("colModel").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' ====== Progress Bar ======
        progressBar = New ProgressBar() With {
            .Name = "progressBar",
            .Location = New Point(10, 700),
            .Size = New Size(895, 20),
            .Style = ProgressBarStyle.Continuous,
            .Visible = False,
            .Minimum = 0,
            .Maximum = 100
        }

        ' ====== Wire Event Handlers ======
        AddHandler chkShowKey.CheckedChanged, AddressOf OnToggleKeyVisibility
        AddHandler txtApiKey.TextChanged, AddressOf OnApiKeyChanged
        AddHandler cmbModels.SelectedIndexChanged, AddressOf OnModelSelected
        AddHandler btnSend.Click, AddressOf OnSendPrompt
        AddHandler btnCancel.Click, AddressOf OnCancelRequest
        AddHandler btnBrowseFile.Click, AddressOf OnBrowseFile
        AddHandler txtPrompt.KeyDown, AddressOf OnPromptKeyDown

        ' ====== Add Controls to Form ======
        grpApiKey.Controls.Add(txtApiKey)
        grpApiKey.Controls.Add(chkShowKey)
        Me.Controls.AddRange({grpApiKey, lblStatus, lblModel, cmbModels,
                              txtPrompt, btnBrowseFile, txtFilePath,
                              btnSend, btnCancel, grpMessages})

        ' Add DataGridView last so it renders on top
        Me.Controls.Add(dgvMessages)
        Me.Controls.Add(progressBar)

        ' Size the form to fit all content
        Dim requiredHeight As Integer = CInt(progressBar.Location.Y + progressBar.Size.Height + 10)
        Me.Size = New Size(920, requiredHeight)
    End Sub

    ' ==========================================================================
    '  HTTP CLIENT SETUP
    ' ==========================================================================

    Private Sub SetupHttpClient()
        apiClient = New HttpClient() With {
            .Timeout = TimeSpan.FromMinutes(2)
        }
        apiClient.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse("application/json"))
    End Sub

    ' ==========================================================================
    '  EVENT HANDLERS
    ' ==========================================================================

    Private Sub OnToggleKeyVisibility(sender As Object, e As EventArgs)
        Dim visible = DirectCast(sender, CheckBox).Checked
        txtApiKey.PasswordChar = If(visible, Chr(0), "*"c)
        chkShowKey.Text = If(visible, "Hide key", "Show key")
    End Sub

    Private Sub OnApiKeyChanged(sender As Object, e As EventArgs)
        apiKeyValue = DirectCast(sender, TextBox).Text.Trim()
        btnSend.Enabled = Not String.IsNullOrEmpty(apiKeyValue)

        If String.IsNullOrEmpty(apiKeyValue) Then
            lblStatus.Text = "Enter your API key above to get started."
            lblStatus.ForeColor = Color.DimGray
        ElseIf apiKeyValue.StartsWith("sk-", StringComparison.OrdinalIgnoreCase) OrElse _
               apiKeyValue.StartsWith("api-", StringComparison.OrdinalIgnoreCase) Then
            lblStatus.Text = $"API key set. Model: {cmbModels.SelectedItem}"
            lblStatus.ForeColor = Color.Green
        Else
            lblStatus.Text = "Warning: OpenAI keys typically start with 'sk-' or 'api-'"
            lblStatus.ForeColor = Color.Orange
        End If
    End Sub

    Private Sub OnModelSelected(sender As Object, e As EventArgs)
        If cmbModels.SelectedItem IsNot Nothing Then
            selectedModelName = DirectCast(cmbModels.SelectedItem, String)
            If Not String.IsNullOrEmpty(apiKeyValue) Then
                lblStatus.Text = $"Using model: {selectedModelName}"
                lblStatus.ForeColor = Color.Green
            End If
        End If
    End Sub

    Private Sub OnPromptKeyDown(sender As Object, e As KeyEventArgs)
        ' Allow Ctrl+Enter to send the prompt
        If e.Control AndAlso e.KeyCode = Keys.Enter Then
            If btnSend.Enabled Then
                OnSendPrompt(btnSend, EventArgs.Empty)
            End If
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub OnBrowseFile(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog() With {
            .Title = "Attach a text file to your prompt",
            .Filter = "Text Files (*.txt;*.md;*.csv;*.json;*.xml;*.html)|*.txt;*.md;*.csv;*.json;*.xml;*.html|" &
                      "Code Files (*.py;*.js;*.ts;*.cs;*.vb;*.java;*.rb)|*.py;*.js;*.ts;*.cs;*.vb;*.java;*.rb|" &
                      "All Files (*.*)|*.*",
            .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        }
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Try
                Dim fileContent As String = System.IO.File.ReadAllText(dlg.FileName)
                txtFilePath.Text = $"Attached: {System.IO.Path.GetFileName(dlg.FileName)}"

                ' Prepend file content as a natural preamble in the prompt
                Dim preamble As String = "[--- Attached file: " & dlg.FileName & " ---]" & vbCrLf & vbCrLf & fileContent & vbCrLf & vbCrLf & "[--- End of attached file ---]" & vbCrLf & vbCrLf
                
                If String.IsNullOrEmpty(txtPrompt.Text) Then
                    txtPrompt.Text = preamble
                Else
                    ' Insert at the top (before existing prompt text)
                    Dim cursorPos As Integer = txtPrompt.SelectionStart
                    txtPrompt.Text = $"{preamble}{txtPrompt.Text}"
                    txtPrompt.SelectionStart = CInt(preamble.Length) + cursorPos
                End If

                lblStatus.ForeColor = Color.Blue
                lblStatus.Text = "File attached and prepended to your prompt."

            Catch ex As Exception
                lblStatus.ForeColor = Color.Red
                lblStatus.Text = $"Could not read file: {ex.Message}"
            End Try
        End Using
    End Sub

    Private Async Sub OnSendPrompt(sender As Object, e As EventArgs)
        ' Disable send, enable cancel
        btnSend.Enabled = False
        btnCancel.Visible = True
        btnCancel.Enabled = True
        progressBar.Visible = True
        progressBar.Value = 0
        lblStatus.Text = "Sending request to OpenAI…"
        lblStatus.ForeColor = Color.Orange

        Try
            Dim reply As String = Await SendPromptAsync()

            ' Append assistant reply to grid and conversation history
            dgvMessages.Rows.Add("Assistant", reply, selectedModelName)
            conversationHistory.Add(New ChatMessage With {.Role = "assistant", .Content = reply})

            lblStatus.Text = "Response received successfully."
            lblStatus.ForeColor = Color.Green
            progressBar.Value = 100

        Catch ex As OperationCanceledException
            lblStatus.Text = "Request was cancelled."
            lblStatus.ForeColor = Color.OrangeRed

        Catch ex As HttpRequestException
            lblStatus.Text = $"Network error: {ex.Message}"
            lblStatus.ForeColor = Color.Red

        Catch ex As TaskCanceledException
            lblStatus.Text = "Request timed out (2 min limit)."
            lblStatus.ForeColor = Color.Red

        Catch ex As JsonException
            lblStatus.Text = $"JSON parse error from API: {ex.Message}"
            lblStatus.ForeColor = Color.Red

        Catch ex As UnauthorizedAccessException
            lblStatus.Text = "Unauthorized – check your API key."
            lblStatus.ForeColor = Color.Red

        Catch ex As Exception
            lblStatus.Text = $"Error: {ex.GetType().Name}: {ex.Message}"
            lblStatus.ForeColor = Color.Red

        Finally
            btnSend.Enabled = Not String.IsNullOrEmpty(apiKeyValue)
            btnCancel.Visible = False
            btnCancel.Enabled = False
            progressBar.Visible = False
        End Try
    End Sub

    Private Sub OnCancelRequest(sender As Object, e As EventArgs)
        If ctsCurrentRequest IsNot Nothing Then
            ctsCurrentRequest.Cancel()
        End If
        btnCancel.Enabled = False
        lblStatus.Text = "Cancelling…"
        lblStatus.ForeColor = Color.OrangeRed
    End Sub

    ' ==========================================================================
    '  OPENAI API REQUEST
    ' ==========================================================================

    Private Async Function SendPromptAsync() As Task(Of String)
        ctsCurrentRequest = New CancellationTokenSource(TimeSpan.FromSeconds(120))

        Try
            Dim messagesPayload = BuildMessagesPayload()
            Dim requestBody As New Dictionary(Of String, Object) From {
                {"model", selectedModelName},
                {"messages", messagesPayload},
                {"temperature", 0.7D},
                {"max_tokens", 4096}
            }

            Dim jsonBody As String = JsonSerializer.Serialize(requestBody)
            Dim content As New StringContent(jsonBody, Encoding.UTF8, "application/json")

            ' Set the Authorization header with Bearer token
            apiClient.DefaultRequestHeaders.Clear()
            apiClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {apiKeyValue}")

            lblStatus.Text = "Waiting for response…"
            lblStatus.ForeColor = Color.Orange

            Using resp As HttpResponseMessage = Await apiClient.PostAsync(
                "https://api.openai.com/v1/chat/completions", content, ctsCurrentRequest.Token)

                If Not resp.IsSuccessStatusCode Then
                    Dim errText As String = Await resp.Content.ReadAsStringAsync(ctsCurrentRequest.Token)
                    Throw New Exception($"API error ({resp.StatusCode}): {errText}")
                End If

                Dim rawResponse As String = Await resp.Content.ReadAsStringAsync(ctsCurrentRequest.Token)

                ' Parse JSON: choices[0].message.content
                Using doc As JsonDocument = JsonDocument.Parse(rawResponse)
                    Dim root As JsonElement = doc.RootElement
                    Dim choices As JsonElement = root.GetProperty("choices")

                    If choices.GetArrayLength() > 0 Then
                        Dim firstChoice As JsonElement = choices(0)
                        Dim message As JsonElement = firstChoice.GetProperty("message")
                        Dim contentProp As JsonElement = message.GetProperty("content")
                        Dim reply As String = Nothing
                        
                        If contentProp.ValueKind = JsonValueKind.String Then
                            reply = contentProp.GetString()
                        End If

                        Return If(reply, "(empty response from API)")
                    Else
                        Throw New Exception("API returned zero choices.")
                    End If
                End Using

            End Using

        Finally
            ctsCurrentRequest.Dispose()
            ctsCurrentRequest = Nothing
        End Try
    End Function

    ''' <summary>Builds the messages array including system prompt, history, and current input.</summary>
    Private Function BuildMessagesPayload() As List(Of ChatMessage)
        Dim list As New List(Of ChatMessage) From {
            New ChatMessage With {.Role = "system", .Content = "You are a helpful, honest, and concise assistant. Answer clearly."}
        }

        ' Conversation history (keep last 30 exchanges to manage token budget)
        For Each msg In conversationHistory.TakeLast(30)
            list.Add(New ChatMessage With {.Role = msg.Role, .Content = msg.Content})
        Next

        ' Current user input (already includes any attached file content in txtPrompt.Text)
        list.Add(New ChatMessage With {.Role = "user", .Content = txtPrompt.Text.Trim()})

        Return list
    End Function

    ' ==========================================================================
    '  HELPERS
    ' ==========================================================================

    ''' <summary>A single message in the conversation history.</summary>
    Friend Class ChatMessage
        Property Role As String
        Property Content As String
    End Class

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If ctsCurrentRequest IsNot Nothing Then
            ctsCurrentRequest.Cancel()
        End If
        If apiClient IsNot Nothing Then
            apiClient.CancelPendingRequests()
            apiClient.Dispose()
        End If
    End Sub

End Class
